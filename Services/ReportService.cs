using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;
using Sistema_GuiaLocal_Turismo.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


namespace Sistema_GuiaLocal_Turismo.Services
{
    public class ReportService : IReportService
    {
        private readonly TourismContext _context;

        public ReportService(TourismContext context)
        {
            _context = context;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            // Get current month stats
            var totalPlaces = await _context.Places.CountAsync();
            var activeReservations = await _context.Reservations
                .CountAsync(r => r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn);

            var pendingCheckIns = await _context.Reservations
                .CountAsync(r => r.Status == ReservationStatus.Confirmed && r.StartDate.Date == today);

            var monthlyRevenue = await _context.Reservations
                .Where(r => r.CreatedDate >= thisMonth && r.Status != ReservationStatus.Cancelled)
                .SumAsync(r => r.TotalAmount);

            // Get previous month stats for growth calculation
            var lastMonthPlaces = await _context.Places
                .CountAsync(p => p.CreatedDate < thisMonth);

            var lastMonthReservations = await _context.Reservations
                .CountAsync(r => r.CreatedDate >= lastMonth && r.CreatedDate < thisMonth);

            var lastMonthRevenue = await _context.Reservations
                .Where(r => r.CreatedDate >= lastMonth && r.CreatedDate < thisMonth && r.Status != ReservationStatus.Cancelled)
                .SumAsync(r => r.TotalAmount);

            // Calculate growth percentages
            var placesGrowth = lastMonthPlaces > 0 ? (int)(((totalPlaces - lastMonthPlaces) / (double)lastMonthPlaces) * 100) : 0;
            var reservationsGrowth = lastMonthReservations > 0 ? (int)(((activeReservations - lastMonthReservations) / (double)lastMonthReservations) * 100) : 0;
            var revenueGrowth = lastMonthRevenue > 0 ? (decimal)(((double)(monthlyRevenue - lastMonthRevenue) / (double)lastMonthRevenue) * 100) : 0;

            // Get recent alerts - fetch raw data first
            var alertsData = await _context.Alerts
                .Where(a => !a.IsSent)
                .OrderByDescending(a => a.CreatedDate)
                .Take(5)
                .Select(a => new
                {
                    a.Type,
                    a.Title,
                    a.Message,
                    a.CreatedDate
                })
                .ToListAsync();

            // Transform alerts in memory
            var recentAlerts = alertsData.Select(a => new RecentAlert
            {
                Type = a.Type.ToString(),
                Title = a.Title,
                Message = a.Message,
                CreatedDate = a.CreatedDate,
                Icon = GetAlertIcon(a.Type),
                CssClass = GetAlertCssClass(a.Type)
            }).ToList();

            // Get category stats - fetch raw data first
            var categoryData = await _context.Categories
                .Select(c => new
                {
                    c.Name,
                    PlaceCount = c.Places.Count(),
                    ReservationCount = c.Places.SelectMany(p => p.Reservations).Count(),
                    Revenue = c.Places.SelectMany(p => p.Reservations)
                        .Where(r => r.Status != ReservationStatus.Cancelled)
                        .Sum(r => r.TotalAmount)
                })
                .ToListAsync();

            // Transform category stats in memory
            var categoryStats = categoryData.Select(c => new CategoryStats
            {
                CategoryName = c.Name,
                PlaceCount = c.PlaceCount,
                ReservationCount = c.ReservationCount,
                Revenue = c.Revenue,
                Icon = GetCategoryIcon(c.Name),
                Color = GetCategoryColor(c.Name)
            }).ToList();

            // Get popular places
            var popularPlaces = await _context.Places
                .Select(p => new
                {
                    p.Name,
                    ReservationCount = p.Reservations.Count()
                })
                .OrderByDescending(p => p.ReservationCount)
                .Take(5)
                .ToListAsync();

            var totalReservations = popularPlaces.Sum(p => p.ReservationCount);
            var popularPlacesList = popularPlaces.Select((p, index) => new PopularPlace
            {
                Name = p.Name,
                ReservationCount = p.ReservationCount,
                Percentage = totalReservations > 0 ? (decimal)p.ReservationCount / totalReservations * 100 : 0,
                Rank = index + 1
            }).ToList();

            return new DashboardViewModel
            {
                Stats = new DashboardStats
                {
                    TotalPlaces = totalPlaces,
                    ActiveReservations = activeReservations,
                    PendingCheckIns = pendingCheckIns,
                    MonthlyRevenue = monthlyRevenue,
                    PlacesGrowth = placesGrowth,
                    ReservationsGrowth = reservationsGrowth,
                    RevenueGrowth = revenueGrowth
                },
                RecentAlerts = recentAlerts,
                CategoryStats = categoryStats,
                PopularPlaces = popularPlacesList
            };
        }

        public async Task<byte[]> GenerateReservationReportAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var reservations = await _context.Reservations
                .Include(r => r.Place)
                .ThenInclude(p => p.Category)
                .Where(r => (!dateFrom.HasValue || r.StartDate >= dateFrom.Value) &&
                           (!dateTo.HasValue || r.StartDate <= dateTo.Value))
                .OrderBy(r => r.StartDate)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    // Header
                    page.Header()
                        .Text("Reporte de Reservas - Sistema de Turismo Costa Rica")
                        .SemiBold().FontSize(18).FontColor(Colors.Blue.Medium);

                    // Content
                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            // Date range
                            x.Item().Text($"Período: {dateFrom?.ToString("dd/MM/yyyy") ?? "Inicio"} - {dateTo?.ToString("dd/MM/yyyy") ?? "Fin"}");
                            x.Item().PaddingTop(20);

                            // Table
                            x.Item().Table(table =>
                            {
                                // Define columns
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2); // Código
                                    columns.RelativeColumn(3); // Cliente
                                    columns.RelativeColumn(3); // Lugar
                                    columns.RelativeColumn(2); // Fecha
                                    columns.RelativeColumn(2); // Estado
                                    columns.RelativeColumn(2); // Total
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Código");
                                    header.Cell().Element(CellStyle).Text("Cliente");
                                    header.Cell().Element(CellStyle).Text("Lugar");
                                    header.Cell().Element(CellStyle).Text("Fecha");
                                    header.Cell().Element(CellStyle).Text("Estado");
                                    header.Cell().Element(CellStyle).Text("Total");

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                    }
                                });

                                // Data rows
                                foreach (var reservation in reservations)
                                {
                                    table.Cell().Element(CellStyle).Text(reservation.ReservationCode);
                                    table.Cell().Element(CellStyle).Text(reservation.ClientName);
                                    table.Cell().Element(CellStyle).Text(reservation.Place.Name);
                                    table.Cell().Element(CellStyle).Text(reservation.StartDate.ToString("dd/MM/yyyy"));
                                    table.Cell().Element(CellStyle).Text(GetStatusText(reservation.Status));
                                    table.Cell().Element(CellStyle).Text($"${reservation.TotalAmount:F2}");

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                                    }
                                }
                            });

                            // Summary
                            x.Item().PaddingTop(20).Column(summary =>
                            {
                                summary.Item().Text($"Total de reservas: {reservations.Count}").SemiBold();
                                summary.Item().Text($"Ingresos totales: ${reservations.Sum(r => r.TotalAmount):F2}").SemiBold();
                            });
                        });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                            x.Span(" de ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }


        public async Task<byte[]> GenerateRevenueReportAsync(DateTime? dateFrom, DateTime? dateTo)
        {
            var reservations = await _context.Reservations
                .Include(r => r.Place)
                .ThenInclude(p => p.Category)
                .Where(r => (!dateFrom.HasValue || r.CreatedDate >= dateFrom.Value) &&
                           (!dateTo.HasValue || r.CreatedDate <= dateTo.Value) &&
                           r.Status != ReservationStatus.Cancelled)
                .OrderBy(r => r.CreatedDate)
                .ToListAsync();

            // Group by category for revenue analysis
            var categoryRevenue = reservations
                .GroupBy(r => r.Place.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Revenue = g.Sum(r => r.TotalAmount),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header()
                        .Text("Reporte de Ingresos - Sistema de Turismo Costa Rica")
                        .SemiBold().FontSize(18).FontColor(Colors.Green.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Item().Text($"Período: {dateFrom?.ToString("dd/MM/yyyy") ?? "Inicio"} - {dateTo?.ToString("dd/MM/yyyy") ?? "Fin"}");
                            x.Item().PaddingTop(20);

                            // Summary cards
                            x.Item().Row(row =>
                            {
                                row.RelativeItem().Background(Colors.Blue.Lighten3).Padding(10).Column(col =>
                                {
                                    col.Item().Text("Total Reservas").SemiBold();
                                    col.Item().Text(reservations.Count.ToString()).FontSize(24).SemiBold();
                                });

                                row.Spacing(10);

                                row.RelativeItem().Background(Colors.Green.Lighten3).Padding(10).Column(col =>
                                {
                                    col.Item().Text("Ingresos Totales").SemiBold();
                                    col.Item().Text($"${reservations.Sum(r => r.TotalAmount):F2}").FontSize(24).SemiBold();
                                });

                                row.Spacing(10);

                                row.RelativeItem().Background(Colors.Orange.Lighten3).Padding(10).Column(col =>
                                {
                                    col.Item().Text("Promedio por Reserva").SemiBold();
                                    col.Item().Text($"${(reservations.Any() ? reservations.Average(r => r.TotalAmount) : 0):F2}").FontSize(24).SemiBold();
                                });
                            });

                            x.Item().PaddingTop(20);

                            // Revenue by category table
                            x.Item().Text("Ingresos por Categoría").SemiBold().FontSize(16);
                            x.Item().PaddingTop(10);

                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Categoría");
                                    header.Cell().Element(CellStyle).Text("Reservas");
                                    header.Cell().Element(CellStyle).Text("Ingresos");
                                    header.Cell().Element(CellStyle).Text("Porcentaje");

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                    }
                                });

                                var totalRevenue = categoryRevenue.Sum(x => x.Revenue);

                                foreach (var item in categoryRevenue)
                                {
                                    var percentage = totalRevenue > 0 ? (item.Revenue / totalRevenue * 100) : 0;

                                    table.Cell().Element(CellStyle).Text(item.Category);
                                    table.Cell().Element(CellStyle).Text(item.Count.ToString());
                                    table.Cell().Element(CellStyle).Text($"${item.Revenue:F2}");
                                    table.Cell().Element(CellStyle).Text($"{percentage:F1}%");

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                                    }
                                }
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                            x.Span(" de ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GeneratePlaceOccupancyReportAsync()
        {
            var places = await _context.Places
                .Include(p => p.Category)
                .Include(p => p.Reservations)
                .Where(p => p.Status == PlaceStatus.Available)
                .ToListAsync();

            var occupancyData = places.Select(p => new
            {
                Code = p.Code,
                Name = p.Name,
                Category = p.Category.Name,
                Capacity = p.Capacity,
                CurrentOccupancy = p.Reservations.Where(r => r.Status == ReservationStatus.CheckedIn).Sum(r => r.NumberOfPeople),
                TotalReservations = p.Reservations.Count,
                MonthlyReservations = p.Reservations.Count(r => r.CreatedDate >= DateTime.Today.AddDays(-30))
            }).ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header()
                        .Text("Reporte de Ocupación de Lugares - Sistema de Turismo Costa Rica")
                        .SemiBold().FontSize(16).FontColor(Colors.Teal.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Item().Text($"Fecha del reporte: {DateTime.Now:dd/MM/yyyy HH:mm}");
                            x.Item().PaddingTop(20);

                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2); // Código
                                    columns.RelativeColumn(4); // Nombre
                                    columns.RelativeColumn(2); // Categoría
                                    columns.RelativeColumn(2); // Capacidad
                                    columns.RelativeColumn(2); // Ocupación Actual
                                    columns.RelativeColumn(2); // % Ocupación
                                    columns.RelativeColumn(2); // Total Reservas
                                    columns.RelativeColumn(2); // Reservas Mes
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(CellStyle).Text("Código");
                                    header.Cell().Element(CellStyle).Text("Nombre");
                                    header.Cell().Element(CellStyle).Text("Categoría");
                                    header.Cell().Element(CellStyle).Text("Capacidad");
                                    header.Cell().Element(CellStyle).Text("Ocupación");
                                    header.Cell().Element(CellStyle).Text("% Ocupación");
                                    header.Cell().Element(CellStyle).Text("Total Reservas");
                                    header.Cell().Element(CellStyle).Text("Reservas Mes");

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                    }
                                });

                                foreach (var item in occupancyData)
                                {
                                    var occupancyPercentage = item.Capacity.HasValue && item.Capacity > 0
                                        ? (double)item.CurrentOccupancy / item.Capacity.Value * 100
                                        : 0;

                                    table.Cell().Element(CellStyle).Text(item.Code);
                                    table.Cell().Element(CellStyle).Text(item.Name);
                                    table.Cell().Element(CellStyle).Text(item.Category);
                                    table.Cell().Element(CellStyle).Text(item.Capacity?.ToString() ?? "N/A");
                                    table.Cell().Element(CellStyle).Text(item.CurrentOccupancy.ToString());
                                    table.Cell().Element(CellStyle).Text($"{occupancyPercentage:F1}%");
                                    table.Cell().Element(CellStyle).Text(item.TotalReservations.ToString());
                                    table.Cell().Element(CellStyle).Text(item.MonthlyReservations.ToString());

                                    static IContainer CellStyle(IContainer container)
                                    {
                                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3);
                                    }
                                }
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                            x.Span(" de ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }
        private static string GetStatusText(ReservationStatus status)
        {
            return status switch
            {
                ReservationStatus.Pending => "Pendiente",
                ReservationStatus.Confirmed => "Confirmada",
                ReservationStatus.CheckedIn => "Check-In",
                ReservationStatus.CheckedOut => "Check-Out",
                ReservationStatus.Completed => "Completada",
                ReservationStatus.Cancelled => "Cancelada",
                _ => "Desconocido"
            };
        }

        private string GetAlertIcon(AlertType alertType)
        {
            return alertType switch
            {
                AlertType.ReservationReminder => "fas fa-calendar-check",
                AlertType.CheckInReminder => "fas fa-sign-in-alt",
                AlertType.CheckOutReminder => "fas fa-sign-out-alt",
                AlertType.PaymentReminder => "fas fa-credit-card",
                AlertType.CancellationNotice => "fas fa-times-circle",
                _ => "fas fa-info-circle"
            };
        }

        private string GetAlertCssClass(AlertType alertType)
        {
            return alertType switch
            {
                AlertType.ReservationReminder => "alert-info",
                AlertType.CheckInReminder => "alert-success",
                AlertType.CheckOutReminder => "alert-warning",
                AlertType.PaymentReminder => "alert-warning",
                AlertType.CancellationNotice => "alert-danger",
                _ => "alert-secondary"
            };
        }

        private string GetCategoryIcon(string categoryName)
        {
            return categoryName.ToLower() switch
            {
                "hotel" => "fas fa-bed",
                "restaurante" => "fas fa-utensils",
                "aventura" => "fas fa-mountain",
                "cultura" => "fas fa-landmark",
                "playa" => "fas fa-umbrella-beach",
                "naturaleza" => "fas fa-tree",
                _ => "fas fa-map-marker-alt"
            };
        }

        private string GetCategoryColor(string categoryName)
        {
            return categoryName.ToLower() switch
            {
                "hotel" => "#007bff",
                "restaurante" => "#28a745",
                "aventura" => "#dc3545",
                "cultura" => "#6f42c1",
                "playa" => "#17a2b8",
                "naturaleza" => "#20c997",
                _ => "#6c757d"
            };
        }
    }
}