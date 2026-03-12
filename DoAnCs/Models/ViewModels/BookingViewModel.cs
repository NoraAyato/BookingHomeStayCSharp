using System.ComponentModel.DataAnnotations;

namespace DoAnCs.Models.ViewModels
{
    public class BookingViewModel
    {
        public string HomestayId { get; set; }
        public string HomestayName { get; set; }
        public string HomestayAddress { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfNights { get; set; }
        public List<SelectedRoom> SelectedRooms { get; set; } = new List<SelectedRoom>();
      
    }
}
