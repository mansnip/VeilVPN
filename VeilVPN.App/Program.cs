using Domain.Interfaces;
using IoC;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// افزودن Dependency Injection برای سرویس‌ها
#region Register Dependencies

DependencyContainer.RegisterDependencies(builder.Services);

DependencyContainer.RegisterDependencies(builder.Services);

#endregion
// اضافه کردن Logger
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configuration برای خواندن تنظیمات از appsettings
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

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
app.UseAuthorization();

// Area routes
app.MapAreaControllerRoute(
    name: "LandingArea",
    areaName: "Landing",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Default route (redirect to Landing Area)
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Landing}/{controller=Home}/{action=Index}/{id?}");

app.Run();