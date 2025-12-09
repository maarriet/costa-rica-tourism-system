using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Sistema_GuiaLocal_Turismo.Data;

#nullable disable

namespace Sistema_GuiaLocal_Turismo.Migrations
{
    [DbContext(typeof(TourismContext))]
    partial class TourismContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Sistema_GuiaLocal_Turismo.Models.Category", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                b.Property<string>("Color")
                    .HasMaxLength(20)
                    .HasColumnType("character varying(20)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("Description")
                    .HasMaxLength(500)
                    .HasColumnType("character varying(500)");

                b.Property<string>("Icon")
                    .HasMaxLength(50)
                    .HasColumnType("character varying(50)");

                b.Property<bool>("IsActive")
                    .HasColumnType("boolean");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("character varying(100)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("timestamp with time zone");

                b.HasKey("Id");

                b.ToTable("Categories");
            });

            modelBuilder.Entity("Sistema_GuiaLocal_Turismo.Models.Place", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                b.Property<int>("Capacity")
                    .HasColumnType("integer");

                b.Property<int>("CategoryId")
                    .HasColumnType("integer");

                b.Property<string>("Code")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnType("character varying(20)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("Description")
                    .HasColumnType("text");

                b.Property<string>("Location")
                    .IsRequired()
                    .HasMaxLength(300)
                    .HasColumnType("character varying(300)");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.Property<decimal>("Price")
                    .HasColumnType("numeric(10,2)");

                b.Property<int>("Status")
                    .HasColumnType("integer");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("timestamp with time zone");

                b.HasKey("Id");

                b.HasIndex("CategoryId");

                b.HasIndex("Code")
                    .IsUnique();

                b.ToTable("Places");
            });

            modelBuilder.Entity("Sistema_GuiaLocal_Turismo.Models.Reservation", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("integer");

                NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                b.Property<string>("Code")
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnType("character varying(20)");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("CustomerEmail")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.Property<string>("CustomerName")
                    .IsRequired()
                    .HasMaxLength(200)
                    .HasColumnType("character varying(200)");

                b.Property<string>("CustomerPhone")
                    .HasMaxLength(20)
                    .HasColumnType("character varying(20)");

                b.Property<string>("Notes")
                    .HasColumnType("text");

                b.Property<int>("NumberOfPeople")
                    .HasColumnType("integer");

                b.Property<int>("PlaceId")
                    .HasColumnType("integer");

                b.Property<DateTime>("ReservationDate")
                    .HasColumnType("timestamp with time zone");

                b.Property<int>("Status")
                    .HasColumnType("integer");

                b.Property<decimal>("TotalAmount")
                    .HasColumnType("numeric(10,2)");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("timestamp with time zone");

                b.HasKey("Id");

                b.HasIndex("Code")
                    .IsUnique();

                b.HasIndex("PlaceId");

                b.ToTable("Reservations");
            });

            modelBuilder.Entity("Sistema_GuiaLocal_Turismo.Models.Place", b =>
            {
                b.HasOne("Sistema_GuiaLocal_Turismo.Models.Category", "Category")
                    .WithMany("Places")
                    .HasForeignKey("CategoryId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Category");
            });

            modelBuilder.Entity("Sistema_GuiaLocal_Turismo.Models.Reservation", b =>
            {
                b.HasOne("Sistema_GuiaLocal_Turismo.Models.Place", "Place")
                    .WithMany("Reservations")
                    .HasForeignKey("PlaceId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Place");
            });

            modelBuilder.Entity("Sistema_GuiaLocal_Turismo.Models.Category", b =>
            {
                b.Navigation("Places");
            });

            modelBuilder.Entity("Sistema_GuiaLocal_Turismo.Models.Place", b =>
            {
                b.Navigation("Reservations");
            });
#pragma warning restore 612, 618
        }
    }
}