using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Models.AI;
using Manage_KPI_or_OKR_System.Services;
using Manage_KPI_or_OKR_System.Services.AI;
using Manage_KPI_or_OKR_System.Services.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DotNetEnv;
using OfficeOpenXml;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;

// EPPlus yêu cầu cấu hình LicenseContext để hoạt động trong môi trường non-commercial
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var builder = WebApplication.CreateBuilder(args);
// Defense in depth for the Blob client. Its built-in HttpClient loggers are also
// removed at registration because request URIs carry short-lived SAS credentials.
builder.Logging.AddFilter(
    "System.Net.Http.HttpClient.PrivateKnowledgeBlobStore",
    LogLevel.Warning);
// Local .env values are development fallbacks only. Production must use the
// hosting provider's environment/secret store.
if (builder.Environment.IsDevelopment())
{
    Env.NoClobber().Load();
}
builder.Configuration.AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}
else
{
    var smtpSender = builder.Configuration["SmtpSettings:SenderEmail"];
    var smtpPassword = builder.Configuration["SmtpSettings:Password"];
    var passwordResetBaseUrl = builder.Configuration["PasswordReset:PublicBaseUrl"];
    if (string.IsNullOrWhiteSpace(smtpSender) || string.IsNullOrWhiteSpace(smtpPassword))
    {
        throw new InvalidOperationException(
            "SMTP credentials are required outside Development. Set SmtpSettings__SenderEmail and SmtpSettings__Password in the secret store.");
    }

    if (!Uri.TryCreate(passwordResetBaseUrl, UriKind.Absolute, out var resetUri) ||
        resetUri.Scheme != Uri.UriSchemeHttps)
    {
        throw new InvalidOperationException(
            "PasswordReset__PublicBaseUrl must be a trusted absolute HTTPS URL outside Development.");
    }
}

// 1. Đăng ký các dịch vụ (Services)
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add(new Manage_KPI_or_OKR_System.Filters.ForcePasswordChangeFilter());
});
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Đăng ký EmailService
builder.Services.AddScoped<Manage_KPI_or_OKR_System.Services.IEmailService, Manage_KPI_or_OKR_System.Services.EmailService>();

// Register OKRProgressService
builder.Services.AddScoped<Manage_KPI_or_OKR_System.Services.IOKRProgressService, Manage_KPI_or_OKR_System.Services.OKRProgressService>();
builder.Services.AddScoped<Manage_KPI_or_OKR_System.Services.IOKRWorkflowService, Manage_KPI_or_OKR_System.Services.OKRWorkflowService>();

// Register AI services
builder.Services.AddScoped<IAIDataService, AIDataService>();
builder.Services.AddScoped<IAIAlertService, AIAlertService>();
builder.Services.AddScoped<IAITaskDecompositionService, AITaskDecompositionService>();
builder.Services.Configure<Manage_KPI_or_OKR_System.Options.AzureSearchOptions>(
    builder.Configuration.GetSection(Manage_KPI_or_OKR_System.Options.AzureSearchOptions.SectionName));
builder.Services.Configure<Manage_KPI_or_OKR_System.Options.BgeM3Options>(
    builder.Configuration.GetSection(Manage_KPI_or_OKR_System.Options.BgeM3Options.SectionName));
builder.Services.Configure<Manage_KPI_or_OKR_System.Options.MinerUOptions>(
    builder.Configuration.GetSection(Manage_KPI_or_OKR_System.Options.MinerUOptions.SectionName));
builder.Services.Configure<Manage_KPI_or_OKR_System.Options.KnowledgeStorageOptions>(
    builder.Configuration.GetSection(Manage_KPI_or_OKR_System.Options.KnowledgeStorageOptions.SectionName));
builder.Services.Configure<Manage_KPI_or_OKR_System.Options.MalwareScannerOptions>(
    builder.Configuration.GetSection(Manage_KPI_or_OKR_System.Options.MalwareScannerOptions.SectionName));
builder.Services.Configure<Manage_KPI_or_OKR_System.Options.DocumentIngestionOptions>(
    builder.Configuration.GetSection(Manage_KPI_or_OKR_System.Options.DocumentIngestionOptions.SectionName));
builder.Services.Configure<Manage_KPI_or_OKR_System.Options.DeepSeekOptions>(
    builder.Configuration.GetSection(Manage_KPI_or_OKR_System.Options.DeepSeekOptions.SectionName));
builder.Services
    .AddOptions<Manage_KPI_or_OKR_System.Options.AiAdvisoryRolloutOptions>()
    .Bind(builder.Configuration.GetSection(
        Manage_KPI_or_OKR_System.Options.AiAdvisoryRolloutOptions.SectionName))
    .Validate(
        Manage_KPI_or_OKR_System.Services.AI.CheckInAiRolloutGate.IsValid,
        "AiAdvisoryRollout must use a valid mode and positive pilot identifiers; Pilot mode requires at least one tenant.")
    .ValidateOnStart();
builder.Services.AddHttpClient<
    Manage_KPI_or_OKR_System.Models.AI.IAIModelClient,
    Manage_KPI_or_OKR_System.Services.AI.DeepSeekModelClient>(client =>
        client.Timeout = Timeout.InfiniteTimeSpan)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    });
builder.Services.AddHttpClient<IBgeM3EmbeddingClient, BgeM3EmbeddingClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    });
builder.Services.AddHttpClient<IAIEvidenceRetriever, AzureSearchEvidenceRetriever>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    });
builder.Services.AddHttpClient<IAzureSearchIndexWriter, AzureSearchIndexWriter>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    });
builder.Services.AddHttpClient<IMinerUClient, MinerUClient>(client =>
        client.Timeout = Timeout.InfiniteTimeSpan)
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    });
builder.Services.AddHttpClient<IPrivateKnowledgeBlobStore, PrivateKnowledgeBlobStore>(
        "PrivateKnowledgeBlobStore")
    .RemoveAllLoggers()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    });
builder.Services.AddSingleton<IAIEvidenceSecurityFilterBuilder, EvidenceSecurityFilterBuilder>();
builder.Services.AddScoped<ICheckInAiRolloutGate, CheckInAiRolloutGate>();
builder.Services.AddScoped<IAiProposalPersistence, AiProposalPersistence>();
builder.Services.AddScoped<IOkrKeyResultAiProposalPersistence,
    OkrKeyResultAiProposalPersistence>();
builder.Services.AddScoped<ICheckInAiEvaluationQueue, CheckInAiEvaluationQueue>();
builder.Services.AddScoped<ICheckInAiEvaluationOutboxAdministrationService,
    CheckInAiEvaluationOutboxAdministrationService>();
builder.Services.AddScoped<IDocumentIngestionQueue, DocumentIngestionQueue>();
builder.Services.AddScoped<IKnowledgeDocumentAdministrationService, KnowledgeDocumentAdministrationService>();
builder.Services.AddSingleton<IMinerUResultParser, MinerUResultParser>();
builder.Services.AddSingleton<IDocumentThreatScanner, ClamAvDocumentThreatScanner>();
builder.Services.AddScoped<IDocumentIngestionProcessor, DocumentIngestionProcessor>();
builder.Services.AddSingleton<IDocumentIngestionLeaseHeartbeat, DocumentIngestionLeaseHeartbeat>();
builder.Services.AddHostedService<CheckInAiEvaluationWorker>();
builder.Services.AddHostedService<DocumentIngestionWorker>();
builder.Services.AddScoped<Manage_KPI_or_OKR_System.Services.AI.ICheckInAiEvaluator,
    Manage_KPI_or_OKR_System.Services.AI.CheckInAiEvaluator>();
builder.Services.AddScoped<IOkrKeyResultAiAdvisor, OkrKeyResultAiAdvisor>();
builder.Services.AddScoped<Manage_KPI_or_OKR_System.Services.AI.IGoalPlanningDraftService,
    Manage_KPI_or_OKR_System.Services.AI.GoalPlanningDraftService>();
builder.Services.AddSingleton<IGoalPlanningCritic, GoalPlanningCritic>();
builder.Services.AddScoped<IGoalPlanningAssignmentAdvisor, GoalPlanningAssignmentAdvisor>();
builder.Services.AddScoped<IEvaluationReviewDraftAdvisor, EvaluationReviewDraftAdvisor>();
builder.Services.AddScoped<ICustomerSegmentAdvisor, CustomerSegmentAdvisor>();
builder.Services.AddScoped<IPerformanceAnalysisAdvisor, PerformanceAnalysisAdvisor>();
builder.Services.AddScoped<IAIChatAdvisor, AIChatAdvisor>();
builder.Services.AddScoped<IKpiSuggestionAdvisor, KpiSuggestionAdvisor>();
builder.Services.AddScoped<IOkrKeyResultSuggestionAdvisor, OkrKeyResultSuggestionAdvisor>();
builder.Services.AddScoped<EvaluationCalculator>();
builder.Services.AddScoped<IWorkItemCommandValidator, WorkItemCommandValidator>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddHostedService<Manage_KPI_or_OKR_System.Services.AIHistoryCleanupService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddSingleton<IPasswordResetRateLimiter, PasswordResetRateLimiter>();

// Shared-database tenant boundary. The middleware mutates this scoped object
// before controller actions execute; DbContext query filters read the same
// instance for every query in the request.
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<ITenantContext>(services =>
    services.GetRequiredService<TenantContext>());
builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();

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
            OnValidatePrincipal = async context =>
            {
                if (context.Principal == null)
                {
                    return;
                }

                var systemUserIdValue = context.Principal.FindFirstValue("SystemUserId");
                if (string.IsNullOrWhiteSpace(systemUserIdValue) &&
                    context.Request.Path.StartsWithSegments("/Auth/GoogleResponse"))
                {
                    // Google middleware tạm thời dùng cookie scheme này để chuyển
                    // principal ngoài sang tài khoản nội bộ ngay trong callback.
                    return;
                }

                var userIdValue = systemUserIdValue ?? context.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdValue, out var userId))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<MiniERPDbContext>();
                var systemUser = await dbContext.SystemUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(user => user.Id == userId);
                if (systemUser == null ||
                    systemUser.IsActive != true ||
                    (systemUser.TrialEndTime.HasValue && systemUser.TrialEndTime.Value <= DateTime.Now))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var role = systemUser.RoleId.HasValue
                    ? await dbContext.Roles.AsNoTracking().FirstOrDefaultAsync(item => item.Id == systemUser.RoleId.Value)
                    : null;
                if ((systemUser.RoleId.HasValue && role == null) || (role != null && role.IsActive != true))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var expectedRoleName = AuthRoleHelper.GetRoleNameOrDefault(role);
                var currentRoleName = context.Principal.FindFirstValue(ClaimTypes.Role);
                var expectedPasswordStamp = (systemUser.LastPasswordChange?.Ticks ?? 0L)
                    .ToString(System.Globalization.CultureInfo.InvariantCulture);
                var currentPasswordStamp = context.Principal.FindFirstValue(AuthRoleHelper.PasswordChangedClaimType);
                if (!string.Equals(currentRoleName, expectedRoleName, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(currentPasswordStamp, expectedPasswordStamp, StringComparison.Ordinal))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
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
            }
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GOOGLE_CLIENT_ID"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"] ?? string.Empty;
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthRoleHelper.PlatformAdminPolicyName,
        policy => policy.RequireClaim(AuthRoleHelper.PlatformAdminClaimType, bool.TrueString));
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("LoginAttempts", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                SegmentsPerWindow = 5,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
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
app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var extension = Path.GetExtension(context.File.Name);
        if (extension.Equals(".css", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".js", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".woff", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".woff2", StringComparison.OrdinalIgnoreCase))
        {
            context.Context.Response.Headers.CacheControl = "public,max-age=604800";
        }
    }
});
app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
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
