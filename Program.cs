using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using DotNetEnv;
using OfficeOpenXml;
using System.Net;
using System.Security.Claims;

// EPPlus yêu cầu cấu hình LicenseContext để hoạt động trong môi trường non-commercial
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var builder = WebApplication.CreateBuilder(args);
Env.Load();
builder.Configuration.AddEnvironmentVariables();

// 1. Đăng ký các dịch vụ (Services)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// Đăng ký EmailService
builder.Services.AddScoped<Manage_KPI_or_OKR_System.Services.EmailService>();

// Register OKRProgressService
builder.Services.AddScoped<Manage_KPI_or_OKR_System.Services.IOKRProgressService, Manage_KPI_or_OKR_System.Services.OKRProgressService>();

// Register AI services
builder.Services.AddHttpClient<IGeminiService, GeminiService>();
builder.Services.AddScoped<IAIDataService, AIDataService>();
builder.Services.AddScoped<IAIAlertService, AIAlertService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<Manage_KPI_or_OKR_System.Services.AIHistoryCleanupService>();

var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    dataProtectionKeysPath = Environment.ExpandEnvironmentVariables(dataProtectionKeysPath);
}
var dataProtectionKeysDirectory = string.IsNullOrWhiteSpace(dataProtectionKeysPath)
    ? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys")
    : Path.IsPathRooted(dataProtectionKeysPath)
        ? dataProtectionKeysPath
        : Path.Combine(builder.Environment.ContentRootPath, dataProtectionKeysPath);

Directory.CreateDirectory(dataProtectionKeysDirectory);

// Persist Data Protection keys so auth cookies survive IIS/Plesk app pool recycles.
builder.Services.AddDataProtection()
    .SetApplicationName("Manage-KPI-or-OKR-System")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysDirectory));
builder.Services.AddSingleton<EncryptionHelper>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.ForwardLimit = builder.Configuration.GetValue<int?>("ForwardedHeaders:ForwardLimit") ?? 1;

    var knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>();
    if (knownProxies.Length > 0)
    {
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var proxy in knownProxies)
        {
            if (IPAddress.TryParse(proxy, out var ipAddress))
            {
                options.KnownProxies.Add(ipAddress);
                continue;
            }

            throw new InvalidOperationException($"Invalid ForwardedHeaders:KnownProxies value: '{proxy}'. Use a valid IP address.");
        }
    }

    var trustAllProxies = builder.Configuration.GetValue<bool>("ForwardedHeaders:TrustAllProxies");
    if (trustAllProxies && !builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException("ForwardedHeaders:TrustAllProxies is only allowed in Development. Configure ForwardedHeaders:KnownProxies for production.");
    }

    if (trustAllProxies)
    {
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        options.ForwardLimit = null;
    }
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.Name = ".ManageKpiOkr.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (ShouldReturnAuthStatusCode(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (ShouldReturnAuthStatusCode(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnValidatePrincipal = context =>
            {
                if (context.Principal == null)
                {
                    return Task.CompletedTask;
                }

                var removedPermissionClaims = false;
                foreach (var identity in context.Principal.Identities.OfType<ClaimsIdentity>())
                {
                    foreach (var claim in identity.FindAll(PermissionClaimsTransformation.PermissionClaimType).ToList())
                    {
                        identity.RemoveClaim(claim);
                        removedPermissionClaims = true;
                    }
                }

                if (removedPermissionClaims)
                {
                    context.ShouldRenew = true;
                }

                return Task.CompletedTask;
            }
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GOOGLE_CLIENT_ID"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"] ?? string.Empty;
    });

var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(defaultConnectionString))
{
    throw new InvalidOperationException("Missing database connection string. Set ConnectionStrings__DefaultConnection in the environment, .env, user-secrets, or the hosting provider secret store.");
}

builder.Services.AddDbContext<MiniERPDbContext>(options =>
    options.UseSqlServer(defaultConnectionString));
builder.Services.AddScoped<IClaimsTransformation, PermissionClaimsTransformation>();

var app = builder.Build();

var runMigrationsOnStartup = builder.Configuration.GetValue<bool?>("Database:RunMigrationsOnStartup")
    ?? app.Environment.IsDevelopment();

if (runMigrationsOnStartup)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        try
        {
            var dbContext = services.GetRequiredService<MiniERPDbContext>();
            dbContext.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Database migration failed: " + ex);
            throw;
        }
    }
}

// Configure the HTTP request pipeline.
if (builder.Configuration.GetValue<bool>("ForwardedHeaders:Enabled"))
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler("/Home/Error");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static bool ShouldReturnAuthStatusCode(HttpRequest request)
{
    if (request.Headers.XRequestedWith == "XMLHttpRequest")
    {
        return true;
    }

    var path = request.Path;
    if (path.StartsWithSegments("/AI")
        || path.StartsWithSegments("/Notifications")
        || path.StartsWithSegments("/Search")
        || path.StartsWithSegments("/Auth/KeepAlive"))
    {
        return true;
    }

    if (request.Headers.Accept.Any(value => value != null && value.Contains("application/json", StringComparison.OrdinalIgnoreCase)))
    {
        return true;
    }

    return request.HasJsonContentType();
}
