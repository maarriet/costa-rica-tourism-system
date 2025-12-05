// Models/TourismDbContext.cs
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.Data
{
    public class TourismDbContext : DbContext
    {
        public TourismDbContext(DbContextOptions<TourismDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Place> Places { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<PlaceImage> PlaceImages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Category configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Icon).HasMaxLength(50).HasDefaultValue("fas fa-tag");
                entity.Property(e => e.Color).HasMaxLength(20).HasDefaultValue("#007bff");
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Place configuration
            modelBuilder.Entity<Place>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Price).HasColumnType("decimal(10,2)");
                entity.HasIndex(e => e.Code).IsUnique();

                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Places)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Reservation configuration
            modelBuilder.Entity<Reservation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ReservationCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ClientName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.ClientEmail).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)");
                entity.Property(e => e.PlacePrice).HasColumnType("decimal(10,2)");
                entity.HasIndex(e => e.ReservationCode).IsUnique();

                entity.HasOne(e => e.Place)
                      .WithMany(p => p.Reservations)
                      .HasForeignKey(e => e.PlaceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Alert configuration (using your existing model)
            modelBuilder.Entity<Alert>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);

                entity.HasOne(e => e.Reservation)
                      .WithMany(r => r.Alerts)
                      .HasForeignKey(e => e.ReservationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // PlaceImage configuration
            modelBuilder.Entity<PlaceImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.AltText).HasMaxLength(200);

                entity.HasOne(e => e.Place)
                      .WithMany(p => p.Images)
                      .HasForeignKey(e => e.PlaceId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Hoteles y Hospedajes", Description = "Hoteles, hostales, cabañas y todo tipo de alojamiento turístico", Icon = "fas fa-bed", Color = "#007bff", IsActive = true, CreatedDate = DateTime.Now },
                new Category { Id = 2, Name = "Restaurantes y Gastronomía", Description = "Restaurantes, sodas, cafeterías y experiencias gastronómicas", Icon = "fas fa-utensils", Color = "#fd7e14", IsActive = true, CreatedDate = DateTime.Now },
                new Category { Id = 3, Name = "Aventura y Deportes", Description = "Actividades de aventura, deportes extremos y recreación activa", Icon = "fas fa-mountain", Color = "#dc3545", IsActive = true, CreatedDate = DateTime.Now },
                new Category { Id = 4, Name = "Playas y Actividades Acuáticas", Description = "Playas, deportes acuáticos y actividades relacionadas con el mar", Icon = "fas fa-umbrella-beach", Color = "#20c997", IsActive = true, CreatedDate = DateTime.Now },
                new Category { Id = 5, Name = "Cultura y Patrimonio", Description = "Museos, sitios históricos, centros culturales y patrimonio", Icon = "fas fa-landmark", Color = "#6f42c1", IsActive = true, CreatedDate = DateTime.Now },
                new Category { Id = 6, Name = "Transporte y Servicios", Description = "Servicios de transporte, tours y servicios complementarios", Icon = "fas fa-car", Color = "#6c757d", IsActive = true, CreatedDate = DateTime.Now }
            );

            // Seed Places
            modelBuilder.Entity<Place>().HasData(
                new Place { Id = 1, Name = "Hotel Vista Mar", Code = "HTL001", CategoryId = 1, Price = 150.00m, Location = "Guanacaste, Tamarindo", Description = "Hotel frente al mar con vista espectacular", Capacity = 50, Status = PlaceStatus.Available, CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now },
                new Place { Id = 2, Name = "Restaurante Típico Tico", Code = "RST001", CategoryId = 2, Price = 25.00m, Location = "San José, Centro", Description = "Auténtica comida costarricense", Capacity = 80, Status = PlaceStatus.Available, CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now },
                new Place { Id = 3, Name = "Canopy Tour Monteverde", Code = "ADV001", CategoryId = 3, Price = 75.00m, Location = "Monteverde", Description = "Emocionante tour de canopy en el bosque nuboso", Capacity = 20, Status = PlaceStatus.Available, CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now },
                new Place { Id = 4, Name = "Playa Manuel Antonio", Code = "BCH001", CategoryId = 4, Price = 10.00m, Location = "Puntarenas, Manuel Antonio", Description = "Una de las playas más hermosas de Costa Rica", Capacity = 200, Status = PlaceStatus.Available, CreatedDate = DateTime.Now, UpdatedDate = DateTime.Now }
            );

            // Seed Sample Reservations
            modelBuilder.Entity<Reservation>().HasData(
                new Reservation
                {
                    Id = 1,
                    ReservationCode = "RES001",
                    PlaceId = 1,
                    ClientName = "María González",
                    ClientEmail = "maria@email.com",
                    ClientPhone = "+506 8888-1111",
                    StartDate = DateTime.Today.AddDays(7),
                    EndDate = DateTime.Today.AddDays(10),
                    NumberOfPeople = 2,
                    TotalAmount = 450.00m,
                    PlacePrice = 150.00m,
                    Status = ReservationStatus.Confirmed,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                },
                new Reservation
                {
                    Id = 2,
                    ReservationCode = "RES002",
                    PlaceId = 3,
                    ClientName = "Carlos Rodríguez",
                    ClientEmail = "carlos@email.com",
                    ClientPhone = "+506 8888-2222",
                    StartDate = DateTime.Today.AddDays(3),
                    NumberOfPeople = 4,
                    TotalAmount = 300.00m,
                    PlacePrice = 75.00m,
                    Status = ReservationStatus.Pending,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                }
            );
        }
    }
}