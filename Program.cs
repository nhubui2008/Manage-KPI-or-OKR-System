using Manage_KPI_or_OKR_System.Data;
using Manage_KPI_or_OKR_System.Helpers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các dịch vụ (Services)
builder.Services.AddControllersWithViews();

// Đăng ký EmailService
builder.Services.AddScoped<Manage_KPI_or_OKR_System.Services.EmailService>();

// Đăng ký Data Protection & EncryptionHelper
builder.Services.AddDataProtection();
builder.Services.AddSingleton<EncryptionHelper>();

builder.Services.AddAuthentication("Cookies").AddCookie("Cookies", options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
});

builder.Services.AddDbContext<MiniERPDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// ==================================================================================
// 2. ĐOẠN CODE ĐỂ LẤY MẬT KHẨU MÃ HÓA (CHỈ CHẠY 1 LẦN RỒI XÓA)
// ==================================================================================
using (var scope = app.Services.CreateScope())
{
    var helper = scope.ServiceProvider.GetRequiredService<EncryptionHelper>();
    // Thay chuỗi "xgjcfahcgctllynp" bằng mật khẩu ứng dụng Gmail thực tế của bạn
    string secret = "xgjcfahcgctllynp";
    string encryptedValue = helper.Encrypt(secret);

    // In mã này ra cửa sổ Output (Debug) của Visual Studio
    System.Diagnostics.Debug.WriteLine("==============================================");
    System.Diagnostics.Debug.WriteLine("MÃ MẬT KHẨU ĐÃ MÃ HÓA CỦA BẠN:");
    System.Diagnostics.Debug.WriteLine(encryptedValue);
    System.Diagnostics.Debug.WriteLine("==============================================");
}
// ==================================================================================

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
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();