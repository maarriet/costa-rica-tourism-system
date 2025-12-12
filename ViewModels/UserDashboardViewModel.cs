using Sistema_GuiaLocal_Turismo.Models;

namespace Sistema_GuiaLocal_Turismo.ViewModels
{
    public class UserDashboardViewModel
    {
        public ApplicationUser User { get; set; }
        public List<Place> AvailablePlaces { get; set; } = new();
        public List<Reservation> UserReservations { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
    }
}
