using AspNetCoreApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspNetCoreApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IEventService _eventService;

        public BookingsController(IBookingService bookingService, IEventService eventService)
        {
            _bookingService = bookingService;
            _eventService = eventService;
        }

        [HttpPost("events/{id:guid}/book")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateBooking(Guid id)
        {
            var bookingId = await _bookingService.CreateBookingAsync(id);

            var booking = await _bookingService.GetBookingByIdAsync(bookingId);

            var locationUrl = Url.Action(
                action: nameof(GetBookingById),
                controller: "Bookings",
                values: new { id = bookingId },
                protocol: Request.Scheme
            );

            return Accepted(locationUrl, booking);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task <IActionResult> GetBookingById(Guid id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null)
            {
                return NotFound();
            }

            return Ok(booking);
        }
    }
}
