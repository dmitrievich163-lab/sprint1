using System.ComponentModel.DataAnnotations;

namespace AspNetCoreApi.Models
{
    public class Event
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public DateTime StartAt { get; set; }

        [Required]
        public DateTime EndAt { get; set; }

        [Required]
        public int TotalSeats { get;  set; }

        public int AvailableSeats { get;  set; }

        public bool TryReserveSeats(int count = 1)
        {
            if (count <= 0) return false;
            if (AvailableSeats >= count)
            {
                AvailableSeats -= count;
                return true;
            }
            return false;
        }

        public void ReleaseSeats(int count = 1)
        {
            if (count <= 0) return;
            AvailableSeats = Math.Min(TotalSeats, AvailableSeats + count);
        }
    }
}
