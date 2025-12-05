// Services/PlaceService.cs - COMPLETE IMPLEMENTATION
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sistema_GuiaLocal_Turismo.Data;
using Sistema_GuiaLocal_Turismo.Models;
using Sistema_GuiaLocal_Turismo.ViewModels;

namespace Sistema_GuiaLocal_Turismo.Services
{
    public class PlaceService : IPlaceService
    {
        private readonly TourismContext _context;
        private readonly IMapper _mapper;

        public PlaceService(TourismContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Get Methods

        public async Task<PlaceListViewModel> GetPlacesAsync(int page, int pageSize, string searchTerm = "", int? categoryId = null, PlaceStatus? status = null)
        {
            var query = _context.Places.Include(p => p.Category).Include(p => p.Reservations).AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(p => p.Name.Contains(searchTerm) || p.Location.Contains(searchTerm));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var places = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var placeViewModels = _mapper.Map<List<PlaceViewModel>>(places);
            var categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();

            return new PlaceListViewModel
            {
                Places = placeViewModels,
                Categories = categories,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalCount = totalCount,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                CategoryFilter = categoryId,
                SelectedStatus = status
            };
        }

        public async Task<PlaceViewModel> GetPlaceByIdAsync(int id)
        {
            var place = await _context.Places
                .Include(p => p.Category)
                .Include(p => p.Reservations)
                .FirstOrDefaultAsync(p => p.Id == id);

            return place == null ? null : _mapper.Map<PlaceViewModel>(place);
        }

        public async Task<PlaceViewModel> GetPlaceByCodeAsync(string code)
        {
            var place = await _context.Places
                .Include(p => p.Category)
                .Include(p => p.Reservations)
                .FirstOrDefaultAsync(p => p.Code == code);

            return place == null ? null : _mapper.Map<PlaceViewModel>(place);
        }

        public async Task<List<PlaceViewModel>> GetPlacesByCategoryAsync(int categoryId)
        {
            var places = await _context.Places
                .Include(p => p.Category)
                .Include(p => p.Reservations)
                .Where(p => p.CategoryId == categoryId && p.Status == PlaceStatus.Available)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return _mapper.Map<List<PlaceViewModel>>(places);
        }

        public async Task<List<PlaceViewModel>> GetAvailablePlacesAsync()
        {
            var places = await _context.Places
                .Include(p => p.Category)
                .Include(p => p.Reservations)
                .Where(p => p.Status == PlaceStatus.Available)
                .OrderBy(p => p.Name)
                .ToListAsync();

            return _mapper.Map<List<PlaceViewModel>>(places);
        }

        #endregion

        #region CRUD Operations

        public async Task<PlaceViewModel> CreatePlaceAsync(PlaceViewModel model)
        {
            // Validate unique code
            if (await _context.Places.AnyAsync(p => p.Code == model.Code))
                throw new InvalidOperationException("Ya existe un lugar con este código");

            var place = _mapper.Map<Place>(model);
            place.CreatedDate = DateTime.Now;
            place.UpdatedDate = DateTime.Now;

            _context.Places.Add(place);
            await _context.SaveChangesAsync();

            return await GetPlaceByIdAsync(place.Id);
        }

        public async Task<PlaceViewModel> UpdatePlaceAsync(PlaceViewModel model)
        {
            var existingPlace = await _context.Places.FindAsync(model.Id);
            if (existingPlace == null)
                throw new InvalidOperationException("Lugar no encontrado");

            // Check unique code (excluding current place)
            if (await _context.Places.AnyAsync(p => p.Code == model.Code && p.Id != model.Id))
                throw new InvalidOperationException("Ya existe un lugar con este código");

            // Update properties
            existingPlace.Code = model.Code;
            existingPlace.Name = model.Name;
            existingPlace.Description = model.Description;
            existingPlace.CategoryId = model.CategoryId;
            existingPlace.Price = model.Price;
            existingPlace.Capacity = model.Capacity;
            existingPlace.Location = model.Location;
            existingPlace.Status = model.Status;
            existingPlace.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return await GetPlaceByIdAsync(existingPlace.Id);
        }

        public async Task<bool> DeletePlaceAsync(int id)
        {
            var place = await _context.Places
                .Include(p => p.Reservations)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (place == null)
                return false;

            if (!await CanDeletePlaceAsync(id))
                throw new InvalidOperationException("No se puede eliminar un lugar con reservas activas");

            _context.Places.Remove(place);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Status and Availability Operations

        public async Task<bool> UpdatePlaceStatusAsync(int id, PlaceStatus status)
        {
            var place = await _context.Places.FindAsync(id);
            if (place == null)
                return false;

            place.Status = status;
            place.UpdatedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateOccupancyAsync(int placeId)
        {
            // Occupancy is calculated dynamically from reservations
            return await PlaceExistsAsync(placeId);
        }

        public async Task<PlaceAvailabilityViewModel> GetPlaceAvailabilityAsync(int placeId, DateTime date)
        {
            var place = await _context.Places.FindAsync(placeId);
            if (place == null)
                return null;

            var reservedSpots = await _context.Reservations
                .Where(r => r.PlaceId == placeId &&
                           r.StartDate.Date == date.Date &&
                           (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn))
                .SumAsync(r => r.NumberOfPeople);

            return new PlaceAvailabilityViewModel
            {
                PlaceId = place.Id,
                PlaceName = place.Name,
                PlaceCode = place.Code,
                Date = date,
                Capacity = place.Capacity,
                ReservedSpots = reservedSpots,
                AvailableSpots = place.Capacity.HasValue ? place.Capacity.Value - reservedSpots : null,
                IsAvailable = place.Status == PlaceStatus.Available &&
                             (place.Capacity == null || reservedSpots < place.Capacity),
                OccupancyPercentage = place.Capacity.HasValue && place.Capacity > 0
                    ? Math.Round((double)reservedSpots / place.Capacity.Value * 100, 2)
                    : null,
                Status = place.Status,
                StatusText = GetStatusText(place.Status)
            };
        }

        public async Task<bool> IsPlaceAvailableAsync(int placeId, DateTime startDate, DateTime? endDate, int numberOfPeople)
        {
            var place = await _context.Places.FindAsync(placeId);
            if (place == null || place.Status != PlaceStatus.Available)
                return false;

            if (place.Capacity.HasValue)
            {
                var checkDate = startDate.Date;
                var existingReservations = await _context.Reservations
                    .Where(r => r.PlaceId == placeId &&
                               r.StartDate.Date == checkDate &&
                               (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn))
                    .SumAsync(r => r.NumberOfPeople);

                return existingReservations + numberOfPeople <= place.Capacity.Value;
            }

            return true; // No capacity limit
        }

        #endregion

        #region Statistics and Reporting

        public async Task<List<PlaceStatisticsViewModel>> GetPlaceStatisticsAsync()
        {
            var places = await _context.Places
                .Include(p => p.Category)
                .Include(p => p.Reservations)
                .ToListAsync();

            return places.Select(p => new PlaceStatisticsViewModel
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                CategoryName = p.Category.Name,
                TotalReservations = p.Reservations.Count,
                ActiveReservations = p.Reservations.Count(r =>
                    r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn),
                TotalRevenue = p.Reservations
                    .Where(r => r.Status != ReservationStatus.Cancelled)
                    .Sum(r => r.TotalAmount),
                MonthlyRevenue = p.Reservations
                    .Where(r => r.CreatedDate >= DateTime.Today.AddDays(-30) &&
                               r.Status != ReservationStatus.Cancelled)
                    .Sum(r => r.TotalAmount),
                CurrentOccupancy = p.Reservations
                    .Where(r => r.Status == ReservationStatus.CheckedIn)
                    .Sum(r => r.NumberOfPeople),
                Capacity = p.Capacity,
                OccupancyRate = p.Capacity.HasValue && p.Capacity > 0
                    ? Math.Round((double)p.Reservations.Where(r => r.Status == ReservationStatus.CheckedIn)
                        .Sum(r => r.NumberOfPeople) / p.Capacity.Value * 100, 2)
                    : 0
            }).ToList();
        }

        public async Task<List<PlaceViewModel>> GetPopularPlacesAsync(int count = 5)
        {
            var places = await _context.Places
                .Include(p => p.Category)
                .Include(p => p.Reservations)
                .OrderByDescending(p => p.Reservations.Count)
                .Take(count)
                .ToListAsync();

            return _mapper.Map<List<PlaceViewModel>>(places);
        }

        public async Task<List<PlaceViewModel>> GetRecentlyAddedPlacesAsync(int count = 10)
        {
            var places = await _context.Places
                .Include(p => p.Category)
                .Include(p => p.Reservations)
                .OrderByDescending(p => p.CreatedDate)
                .Take(count)
                .ToListAsync();

            return _mapper.Map<List<PlaceViewModel>>(places);
        }

        #endregion

        #region Validation Methods

        public async Task<bool> IsCodeUniqueAsync(string code, int? excludePlaceId = null)
        {
            var query = _context.Places.Where(p => p.Code == code);

            if (excludePlaceId.HasValue)
                query = query.Where(p => p.Id != excludePlaceId.Value);

            return !await query.AnyAsync();
        }

        public async Task<bool> PlaceExistsAsync(int id)
        {
            return await _context.Places.AnyAsync(p => p.Id == id);
        }

        public async Task<bool> CanDeletePlaceAsync(int id)
        {
            var hasActiveReservations = await _context.Reservations
                .AnyAsync(r => r.PlaceId == id &&
                              (r.Status == ReservationStatus.Confirmed ||
                               r.Status == ReservationStatus.CheckedIn));

            return !hasActiveReservations;
        }

        #endregion

        #region Helper Methods

        private static string GetStatusText(PlaceStatus status)
        {
            return status switch
            {
                PlaceStatus.Available => "Disponible",
                PlaceStatus.Occupied => "Ocupado",
                PlaceStatus.Maintenance => "Mantenimiento",
                PlaceStatus.Inactive => "Inactivo",
                _ => "Desconocido"
            };
        }

        #endregion
    }
}