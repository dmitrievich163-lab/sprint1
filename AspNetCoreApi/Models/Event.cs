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
    }
}
