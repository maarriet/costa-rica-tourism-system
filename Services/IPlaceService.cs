// Services/IPlaceService.cs - COMPLETE INTERFACE
using Sistema_GuiaLocal_Turismo.Models;
using Sistema_GuiaLocal_Turismo.ViewModels;

namespace Sistema_GuiaLocal_Turismo.Services
{
    public interface IPlaceService
    {
        // Get methods
        Task<PlaceListViewModel> GetPlacesAsync(int page, int pageSize, string searchTerm = "", int? categoryId = null, PlaceStatus? status = null);
        Task<PlaceViewModel> GetPlaceByIdAsync(int id);
        Task<PlaceViewModel> GetPlaceByCodeAsync(string code);
        Task<List<PlaceViewModel>> GetPlacesByCategoryAsync(int categoryId);
        Task<List<PlaceViewModel>> GetAvailablePlacesAsync();

        // CRUD operations
        Task<PlaceViewModel> CreatePlaceAsync(PlaceViewModel model);
        Task<PlaceViewModel> UpdatePlaceAsync(PlaceViewModel model);
        Task<bool> DeletePlaceAsync(int id);

        // Status and availability operations
        Task<bool> UpdatePlaceStatusAsync(int id, PlaceStatus status);
        Task<bool> UpdateOccupancyAsync(int placeId);
        Task<PlaceAvailabilityViewModel> GetPlaceAvailabilityAsync(int placeId, DateTime date);
        Task<bool> IsPlaceAvailableAsync(int placeId, DateTime startDate, DateTime? endDate, int numberOfPeople);

        // Statistics and reporting
        Task<List<PlaceStatisticsViewModel>> GetPlaceStatisticsAsync();
        Task<List<PlaceViewModel>> GetPopularPlacesAsync(int count = 5);
        Task<List<PlaceViewModel>> GetRecentlyAddedPlacesAsync(int count = 10);

        // Validation methods
        Task<bool> IsCodeUniqueAsync(string code, int? excludePlaceId = null);
        Task<bool> PlaceExistsAsync(int id);
        Task<bool> CanDeletePlaceAsync(int id);
    }
}