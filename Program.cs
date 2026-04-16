using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Manage_KPI_or_OKR_System.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using DotNetEnv;
using OfficeOpenXml;

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

// Đăng ký Data Protection & EncryptionHelper
builder.Services.AddDataProtection();
builder.Services.AddSingleton<EncryptionHelper>();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GOOGLE_CLIENT_ID"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"] ?? string.Empty;
    });

builder.Services.AddDbContext<MiniERPDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Tự chạy migration khi app khởi động
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

// Configure the HTTP request pipeline.
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
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
