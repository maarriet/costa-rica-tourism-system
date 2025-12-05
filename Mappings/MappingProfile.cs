// Mappings/MappingProfile.cs
using AutoMapper;
using Sistema_GuiaLocal_Turismo.Models;
using Sistema_GuiaLocal_Turismo.ViewModels;

namespace Sistema_GuiaLocal_Turismo.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Place mappings
            CreateMap<Place, PlaceViewModel>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
                .ForMember(dest => dest.CurrentOccupancy, opt => opt.MapFrom(src =>
                    src.Reservations.Where(r => r.Status == ReservationStatus.CheckedIn).Sum(r => r.NumberOfPeople)))
                .ForMember(dest => dest.ReservationCount, opt => opt.MapFrom(src => src.Reservations.Count))
                .ForMember(dest => dest.ActiveReservations, opt => opt.MapFrom(src =>
                    src.Reservations.Count(r => r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.CheckedIn)));

            CreateMap<PlaceViewModel, Place>()
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.Reservations, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now));

            // Reservation mappings
            CreateMap<Reservation, ReservationViewModel>()
                .ForMember(dest => dest.PlaceName, opt => opt.MapFrom(src => src.Place.Name))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Place.Category.Name))
                .ForMember(dest => dest.PlacePrice, opt => opt.MapFrom(src => src.Place.Price))
                .ForMember(dest => dest.DaysUntilReservation, opt => opt.MapFrom(src => (src.StartDate - DateTime.Now).Days));

            CreateMap<ReservationViewModel, Reservation>()
                .ForMember(dest => dest.Place, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now));

            // Category mappings
            CreateMap<Category, CategoryViewModel>()
                .ForMember(dest => dest.PlaceCount, opt => opt.MapFrom(src => src.Places.Count))
                .ForMember(dest => dest.ReservationCount, opt => opt.MapFrom(src =>
                    src.Places.SelectMany(p => p.Reservations).Count()))
                .ForMember(dest => dest.Revenue, opt => opt.MapFrom(src =>
                    src.Places.SelectMany(p => p.Reservations)
                        .Where(r => r.Status != ReservationStatus.Cancelled)
                        .Sum(r => r.TotalAmount)));

            CreateMap<CategoryViewModel, Category>()
                .ForMember(dest => dest.Places, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedDate, opt => opt.Ignore());
        }
    }
}