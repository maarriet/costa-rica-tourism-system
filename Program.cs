using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Mappings;
using Sistema_GuiaLocal_Turismo.Services;
using Sistema_GuiaLocal_Turismo.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using QuestPDF.Infrastructure;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

// Configurar cultura para USD
var cultureInfo = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Database connection (tu código existente)
var host = Environment.GetEnvironmentVariable("PGHOST");
var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
var database = Environment.GetEnvironmentVariable("PGDATABASE");
var username = Environment.GetEnvironmentVariable("PGUSER");
var password = Environment.GetEnvironmentVariable("PGPASSWORD");

string connectionString;
if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(database) && !string.IsNullOrEmpty(username))
{
    connectionString = $"Host={host};Port={pgPort};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    Console.WriteLine($"✅ Using Railway PostgreSQL: {host}:{pgPort}/{database}");
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine("⚠️ Using local connection string");
}

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Add DbContext
builder.Services.AddDbContext<TourismContext>(options =>
    options.UseNpgsql(connectionString));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<TourismContext>()
.AddDefaultTokenProviders();

// Configure authentication cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
});

// Add AutoMapper
builder.Services.AddAutoMapper(cfg => {
    cfg.AddProfile<MappingProfile>();
});

// Add your services
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPlaceService, PlaceService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddHostedService<ReservationAlertService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure localization
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("en-US"),
    SupportedCultures = new[] { cultureInfo },
    SupportedUICultures = new[] { cultureInfo }
});

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

// Add authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// Initialize database and roles
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<TourismContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        Console.WriteLine("🔄 Starting database initialization...");

        // Delete and recreate database
        Console.WriteLine("🗑️ Deleting existing database...");
        await context.Database.EnsureDeletedAsync();

        Console.WriteLine("🗄️ Creating fresh database...");
        var created = await context.Database.EnsureCreatedAsync();

        if (created)
        {
            Console.WriteLine("✅ Fresh database created successfully!");
        }

        // Create roles
        await SeedRoles(roleManager, userManager);

        // Seed sample data
        Console.WriteLine("🌱 Seeding sample data...");
        await SeedSampleData(context);

        Console.WriteLine("✅ Database initialized successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database error: {ex.Message}");
    }
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://*:{port}");

app.Run();

// Seed roles and admin user
static async Task SeedRoles(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
{
    // Create roles
    string[] roleNames = { "Administrador", "Usuario" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
            Console.WriteLine($"✅ Role '{roleName}' created");
        }
    }

    // Create admin user
    var adminEmail = "admin@turismocr.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Administrador",
            LastName = "Sistema",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Administrador");
            Console.WriteLine($"✅ Admin user created: {adminEmail} / Admin123!");
        }
    }
}

// Tu método SeedSampleData existente (mantén igual)
static async Task SeedSampleData(TourismContext context)
{
    // Tu código existente de seed...
}