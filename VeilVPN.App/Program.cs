using Application.Services.Interfaces;
using DataLayer.Context;
using IoC;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using VeilVPN.App.Services.Implimentation;
using VeilVPN.App.Services.Interfaces;
using VeilVPN.Hubs;

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

builder.Services.AddScoped<IChatService, ChatService>(); // رجیستر کردن سرویس چت
builder.Services.AddSingleton<IUserConnectionManager, UserConnectionManager>();

builder.Services.AddSignalR()
                .AddJsonProtocol(options => {
                    options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase; // <<< این خط مهم است
                });

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

using (var scope = app.Services.CreateScope()) // یک Scope جدید برای دسترسی به سرویس‌های Scoped ایجاد می‌کنیم
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<VeilVpnDbContext>(); // دریافت DbContext
        // اعمال Migration ها (اگر دیتابیس وجود نداشته باشد، آن را می‌سازد)
        dbContext.Database.Migrate();
        app.Logger.LogInformation("Database migrations applied successfully."); // لاگ برای اطمینان
    }
    catch (Exception ex)
    {
        // اگر در اتصال به دیتابیس یا اعمال Migration مشکلی پیش آمد، لاگ می‌کنیم
        var logger = services.GetRequiredService<ILogger<Program>>(); // دریافت Logger
        logger.LogError(ex, "An error occurred while migrating the database.");
        // می‌توانید تصمیم بگیرید که آیا برنامه باید در صورت خطا خارج شود یا خیر
        // throw; // uncomment if you want the application to stop on migration error
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting(); // <<< UseRouting باید قبل از UseAuthentication و UseAuthorization باشه

// فعال کردن کش برای فایل‌های استاتیک (جای مناسبش اینجاست)
app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();

// 1. مسیرهای خاص (اول تعریف می‌شوند)
app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Sitemap", action = "Index" });

app.MapControllerRoute(
    name: "robots",
    pattern: "robots.txt",
    defaults: new { controller = "Robots", action = "RobotsTxt" });

// 2. مسیر قراردادی برای همه Area ها
// این مسیر جایگزین تمام MapAreaControllerRoute های قبلی می‌شود
app.MapControllerRoute(
    name: "areas", // یک نام عمومی برای مسیر Area ها
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
// الگوی {area:exists} تضمین می‌کند که بخش اول URL نام یک Area معتبر باشد.
// مقادیر پیش‌فرض controller=Home و action=Index برای زمانی است که در URL مشخص نشوند.
// مثال: /Admin -> Admin/Home/Index
// مثال: /UserPanel/Subscription/Status -> UserPanel/Subscription/Status

// 3. مسیر برای صفحه اصلی سایت (روت /)
// این مسیر به طور خاص آدرس ریشه را به صفحه اصلی لندینگ هدایت می‌کند.
app.MapControllerRoute(
    name: "LandingRoot",
    pattern: "", // الگوی خالی فقط با آدرس ریشه (/) مطابقت دارد
    defaults: new { area = "Landing", controller = "Home", action = "Index" });

// 4. مسیر پیش‌فرض برای کنترلرهای بدون Area (مثل Authentication)
// این مسیر بعد از Area ها تعریف می‌شود تا ابتدا مسیر Area بررسی شود.
// این مسیر جایگزین روت default و authentication قبلی می‌شود.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
// این مسیر Area پیش‌فرض ندارد و برای کنترلرهای خارج از Area ها استفاده می‌شود.
// مثال: /Authentication/SignIn -> Authentication/SignIn
// مثال: /Home/Privacy -> Home/Privacy (اگر چنین کنترولری خارج از Area داشته باشید)


// اضافه کردن Endpoint برای Hub
app.MapHub<ChatHub>("/chatHub"); // آدرس URL که کلاینت به آن وصل می‌شود

app.Run();


// کلاس تنظیمات سایت
public class SiteSettings
{
    public string Domain { get; set; } = "https://www.RahaGozar.com";
}
