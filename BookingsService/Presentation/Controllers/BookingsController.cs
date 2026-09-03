using BookingsService.Application.Services;
using BookingsService.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookingsService.Presentation.Controllers;

[ApiController]
[Route("api")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    private Guid CurrentUserId => new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole(UserRole.Admin.ToString());

    [HttpPost("events/{id:guid}/book")]
    [Authorize(Roles = "User,Admin")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    [HttpGet("bookings/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingById(Guid id)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id);
        if (booking == null)
            return NotFound();

        if (booking.UserId != CurrentUserId && !IsAdmin)
            return Forbid();

        return Ok(booking);
    }

    [HttpDelete("bookings/{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelBooking(Guid id)
    {
        await _bookingService.CancelBookingAsync(id);
        return NoContent();
    }
}
