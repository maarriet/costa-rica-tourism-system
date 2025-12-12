using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Models;
using System.Collections.Generic;
using System.Reflection.Emit;


namespace Sistema_GuiaLocal_Turismo.Data
{
    public class TourismContext : IdentityDbContext<ApplicationUser>
    {
        public TourismContext(DbContextOptions<TourismContext> options) : base(options)
        {
        }

        public DbSet<Place> Places { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<PlaceImage> PlaceImages { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add performance indexes
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.StartDate)
                .HasDatabaseName("IX_Reservation_StartDate");

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.Status)
                .HasDatabaseName("IX_Reservation_Status");

            modelBuilder.Entity<Place>()
                .HasIndex(p => p.Status)
                .HasDatabaseName("IX_Place_Status");

            // Configure decimal precision
            modelBuilder.Entity<Place>()
                .Property(p => p.Price)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Reservation>()
                .Property(r => r.TotalAmount)
                .HasPrecision(10, 2);
            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Alojamiento", Description = "Hoteles, cabañas, hostales", Icon = "fas fa-bed", Color = "#007bff" },
                new Category { Id = 2, Name = "Experiencias", Description = "Tours, aventuras, actividades", Icon = "fas fa-hiking", Color = "#28a745" },
                new Category { Id = 3, Name = "Restaurantes", Description = "Comida típica, internacional", Icon = "fas fa-utensils", Color = "#ffc107" },
                new Category { Id = 4, Name = "Vida Nocturna", Description = "Bares, discotecas, entretenimiento", Icon = "fas fa-cocktail", Color = "#dc3545" },
                new Category { Id = 5, Name = "Bodas", Description = "Ceremonias, recepciones, luna de miel", Icon = "fas fa-heart", Color = "#17a2b8" }
            );

            // Seed Places
            modelBuilder.Entity<Place>().HasData(
                new Place { Id = 1, Code = "HTL001", Name = "Hotel Vista Mar", Description = "Hotel frente al mar con vista panorámica", CategoryId = 1, Price = 120.00m, Capacity = 12, Location = "Guanacaste, Tamarindo", Status = PlaceStatus.Available },
                new Place { Id = 2, Code = "EXP002", Name = "Aventura Canopy", Description = "Tour de canopy en el bosque nuboso", CategoryId = 2, Price = 75.00m, Capacity = 15, Location = "Monteverde, Puntarenas", Status = PlaceStatus.Occupied },
                new Place { Id = 3, Code = "RST003", Name = "Restaurante Típico", Description = "Comida tradicional costarricense", CategoryId = 3, Price = 25.00m, Capacity = 25, Location = "San José, Centro", Status = PlaceStatus.Available }
            );
        }
    }
}
