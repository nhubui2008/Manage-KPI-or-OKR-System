using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using OfficeOpenXml;

// EPPlus yêu cầu cấu hình LicenseContext để hoạt động trong môi trường non-commercial
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var builder = WebApplication.CreateBuilder(args);
Env.Load();
builder.Configuration.AddEnvironmentVariables();
// 1. Đăng ký các dịch vụ (Services)
builder.Services.AddControllersWithViews();

// Đăng ký EmailService
builder.Services.AddScoped<Manage_KPI_or_OKR_System.Services.EmailService>();

// Đăng ký Data Protection & EncryptionHelper
builder.Services.AddDataProtection();
builder.Services.AddSingleton<EncryptionHelper>();

builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["GOOGLE_CLIENT_ID"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["GOOGLE_CLIENT_SECRET"] ?? string.Empty;
    });


builder.Services.AddDbContext<MiniERPDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();