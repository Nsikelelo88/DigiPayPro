using DigitalPayPro.Data;
using DigitalPayPro.Services;
using DigitalPayPro.Data;
using DigitalPayPro.Services;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services
builder.Services.AddScoped<IPayrollService, PayrollService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<ILeaveService, LeaveService>();

// Configure PDF settings
builder.Services.Configure<PdfSettings>(builder.Configuration.GetSection("PdfSettings"));

// Add DinkToPdf converter
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Harden cookie settings
        options.Cookie.HttpOnly = true; // Mitigate XSS
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Send cookie only on HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax; // Prevent CSRF in cross-site contexts while allowing top-level GET navigations
        options.Cookie.Name = ".DigiPayPro.Auth"; // Explicit cookie name
        options.Cookie.IsEssential = true; // Required if using cookie consent
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireHR", policy => policy.RequireRole("Admin", "HR"));
    options.AddPolicy("RequireEmployee", policy => policy.RequireRole("Admin", "HR", "Employee", "Manager"));
});

// Add logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed initial data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();