using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        private Guid CurrentUserId => new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("/api/events/{id:guid}/book")]
        [Authorize(Roles = "User,Admin")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)] // Лимит броней
        [ProducesResponseType(StatusCodes.Status400BadRequest)] // Прошедшее событие
        public async Task<IActionResult> CreateBooking(Guid id)
        {
            var bookingId = await _bookingService.CreateBookingAsync(id, CurrentUserId);

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
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
