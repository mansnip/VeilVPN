using Application.Services.Interfaces;
using DataLayer.Context;
using IoC;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// افزودن Dependency Injection برای سرویس‌ها
#region Register Dependencies

DependencyContainer.RegisterDependencies(builder.Services);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserService, UserService>();
#endregion

// اضافه کردن DbContext
builder.Services.AddDbContext<VeilVpnDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

#region Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}
).AddCookie(options =>
{
    options.LoginPath = "/Authentication/SignIn";
    options.LogoutPath = "/Authentication/SignOut";
    options.AccessDeniedPath = "/Authentication/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
});
#endregion

// اضافه کردن تنظیمات کش برای فایل‌های استاتیک SEO
builder.Services.AddResponseCaching();

// اضافه کردن Logger
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configuration برای خواندن تنظیمات از appsettings
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// اضافه کردن تنظیمات سایت به Configuration
builder.Services.Configure<SiteSettings>(builder.Configuration.GetSection("SiteSettings"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// فعال کردن کش برای فایل‌های استاتیک
app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();

// تنظیم مسیرهای کنترلرهای SEO (باید قبل از سایر مسیرها تعریف شوند)
app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Sitemap", action = "Index" });

app.MapControllerRoute(
    name: "robots",
    pattern: "robots.txt",
    defaults: new { controller = "Robots", action = "RobotsTxt" });

// روت اصلی (پیش‌فرض) که به صفحه لندینگ اشاره می‌کند
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "Landing" });

// روت برای کنترلر Authentication خارج از Area
app.MapControllerRoute(
    name: "authentication",
    pattern: "Authentication/{action=SignIn}/{id?}",
    defaults: new { controller = "Authentication" });

// Area routes - برای استفاده صریح از Area
app.MapAreaControllerRoute(
    name: "LandingArea",
    areaName: "Landing",
    pattern: "Landing/{controller=Home}/{action=Index}/{id?}");

app.MapAreaControllerRoute(
    name: "AdminArea",
    areaName: "Admin",
    pattern: "Admin/{controller=Home}/{action=Index}/{id?}");

app.MapAreaControllerRoute(
    name: "UserPanelArea",
    areaName: "UserPanel",
    pattern: "UserPanel/{controller=Panel}/{action=Index}/{id?}");
app.Run();

// کلاس تنظیمات سایت
public class SiteSettings
{
    public string Domain { get; set; } = "https://www.RahaGozar.com";
}
