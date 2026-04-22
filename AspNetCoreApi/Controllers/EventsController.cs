using AspNetCoreApi.Models;
using AspNetCoreApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        public IActionResult GetAll(
        [FromQuery] string? title,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            var events = _eventService.GetAll(title, from, to, page, pageSize);
            return Ok(events);
        }

        [HttpGet("{id:guid}")]
        public IActionResult GetById(Guid id)
        {
            var eventItem = _eventService.GetById(id);

            if (eventItem == null)
            {
                return NotFound();
            }

            return Ok(eventItem);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Event newEvent)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var createdEvent = _eventService.Create(newEvent);

                return CreatedAtAction(nameof(GetById), new { id = createdEvent.Id }, createdEvent);
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError("EndAt", ex.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPut("{id:guid}")]
        public IActionResult Update(Guid id, [FromBody] Event updatedEvent)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = _eventService.Update(id, updatedEvent);

                if (result == null)
                {
                    return NotFound();
                }

                return Ok(result);
            }
            catch (ValidationException ex)
            {
                ModelState.AddModelError("EndAt", ex.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpDelete("{id:guid}")]
        public IActionResult Delete(Guid id)
        {
            var deleted = _eventService.Delete(id);

            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
