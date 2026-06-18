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
        public async Task<IActionResult> GetById(Guid id)
        {
            var eventItem = _eventService.GetById(id); 
            return Ok(eventItem);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Event newEvent)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                var errorMessage = "Validation failed: " + string.Join("; ", errors);

                throw new ValidationException(errorMessage);
            }

            var createdEvent = await _eventService.Create(newEvent);
            return CreatedAtAction(nameof(GetById), new { id = createdEvent.Id }, createdEvent);
        }
            
     

        [HttpPut("{id:guid}")]
        public async Task <IActionResult> Update(Guid id, [FromBody] Event updatedEvent)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                var errorMessage = "Validation failed: " + string.Join("; ", errors);

                throw new ValidationException(errorMessage);
            }

          
                var result = _eventService.Update(id, updatedEvent);

                return Ok(result);
            }
        

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = _eventService.Delete(id);

            return NoContent();
        }
    }
}
