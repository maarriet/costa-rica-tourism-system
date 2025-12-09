// Program.cs
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Mappings;
using Sistema_GuiaLocal_Turismo.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Sistema_GuiaLocal_Turismo.Models;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

// Use Railway's individual PostgreSQL variables (these are working!)
var host = Environment.GetEnvironmentVariable("PGHOST");
var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
var database = Environment.GetEnvironmentVariable("PGDATABASE");
var username = Environment.GetEnvironmentVariable("PGUSER");
var password = Environment.GetEnvironmentVariable("PGPASSWORD");

string connectionString;

if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(database) && !string.IsNullOrEmpty(username))
{
    // Railway PostgreSQL connection using individual variables
    connectionString = $"Host={host};Port={pgPort};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    Console.WriteLine($"✅ Using Railway PostgreSQL: {host}:{pgPort}/{database}");
}
else
{
    // Fallback to local connection
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine("⚠️ Using local connection string");
}
// Add services - PostgreSQL for Railway
builder.Services.AddDbContext<TourismContext>(options =>
    options.UseNpgsql(connectionString));



// Add AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});

// Add your services
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPlaceService, PlaceService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Railway handles HTTPS, so only redirect in development
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// Auto-migrate and seed on Railway - Force complete database recreation
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<TourismContext>();

        Console.WriteLine("🔄 Starting database initialization...");

        // Force delete and recreate database completely
        Console.WriteLine("🗑️ Deleting existing database...");
        await context.Database.EnsureDeletedAsync();

        Console.WriteLine("🗄️ Creating fresh database...");
        var created = await context.Database.EnsureCreatedAsync();

        if (created)
        {
            Console.WriteLine("✅ Fresh database created successfully!");
        }

        // Seed sample data
        Console.WriteLine("🌱 Seeding sample data...");
        await SeedSampleData(context);
        Console.WriteLine("✅ Sample data seeded successfully!");

        Console.WriteLine("✅ Database initialized successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        // Don't throw - let app start anyway
    }
}


// Use Railway's PORT environment variable
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://*:{port}");

app.Run();

// Seed method
static async Task SeedSampleData(TourismContext context)
{
    // Categories //prueba
    var categories = new[]
    {
        new Category { Name = "Hoteles y Hospedajes", Description = "Hoteles, hostales, cabañas y alojamientos turísticos", Icon = "fas fa-bed", Color = "#007bff", IsActive = true },
        new Category { Name = "Restaurantes y Gastronomía", Description = "Restaurantes, sodas, cafeterías y experiencias gastronómicas", Icon = "fas fa-utensils", Color = "#fd7e14", IsActive = true },
        new Category { Name = "Aventura y Deportes", Description = "Actividades de aventura, deportes extremos y recreación activa", Icon = "fas fa-mountain", Color = "#dc3545", IsActive = true },
        new Category { Name = "Playas y Actividades Acuáticas", Description = "Playas, deportes acuáticos y actividades relacionadas con el mar", Icon = "fas fa-umbrella-beach", Color = "#20c997", IsActive = true },
        new Category { Name = "Cultura y Patrimonio", Description = "Museos, sitios históricos, centros culturales y patrimonio", Icon = "fas fa-landmark", Color = "#6f42c1", IsActive = true },
        new Category { Name = "Transporte y Servicios", Description = "Servicios de transporte, tours y servicios complementarios", Icon = "fas fa-car", Color = "#6c757d", IsActive = true }
    };

    await context.Categories.AddRangeAsync(categories);
    await context.SaveChangesAsync();

    // Places
    var places = new[]
    {
        new Place { Name = "Hotel Vista Mar Tamarindo", Code = "HTL001", CategoryId = 1, Price = 150m, Location = "Guanacaste, Tamarindo", Description = "Hermoso hotel frente al mar con vista espectacular al Pacífico", Capacity = 50, Status = PlaceStatus.Available },
        new Place { Name = "Restaurante Típico Tico", Code = "RST001", CategoryId = 2, Price = 25m, Location = "San José, Centro", Description = "Auténtica comida costarricense en el corazón de la capital", Capacity = 80, Status = PlaceStatus.Available },
        new Place { Name = "Canopy Tour Monteverde", Code = "ADV001", CategoryId = 3, Price = 75m, Location = "Puntarenas, Monteverde", Description = "Emocionante tour de canopy en el bosque nuboso más famoso de Costa Rica", Capacity = 20, Status = PlaceStatus.Available },
        new Place { Name = "Playa Manuel Antonio", Code = "BCH001", CategoryId = 4, Price = 10m, Location = "Puntarenas, Manuel Antonio", Description = "Una de las playas más hermosas de Costa Rica, ideal para familias", Capacity = 200, Status = PlaceStatus.Available },
        new Place { Name = "Museo Nacional", Code = "CUL001", CategoryId = 5, Price = 15m, Location = "San José, Centro", Description = "Museo con la historia y cultura de Costa Rica", Capacity = 100, Status = PlaceStatus.Available },
        new Place { Name = "Tours Guanacaste", Code = "TRS001", CategoryId = 6, Price = 50m, Location = "Guanacaste, Liberia", Description = "Servicios de transporte y tours por toda la provincia", Capacity = 30, Status = PlaceStatus.Available }
    };

    await context.Places.AddRangeAsync(places);
    await context.SaveChangesAsync();

    Console.WriteLine("✅ Sample data seeded successfully!");
}