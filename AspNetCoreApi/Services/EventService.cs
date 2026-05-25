using AspNetCoreApi.Models;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreApi.Services
{
    public class EventService: IEventService
    {
        private readonly List<Event> _events = new();

        public IEnumerable<Event> GetAll()
        {
            return _events;
        }

        public PaginatedResult<Event> GetAll(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10)
        {
            var query = _events.AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
            {
                string lowerTitle = title.ToLower();
                query = query.Where(e => e.Title.ToLower().Contains(lowerTitle));
            }

            if (from.HasValue)
            {
                query = query.Where(e => e.StartAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(e => e.EndAt <= to.Value);
            }

            int totalCount = query.Count();

            var itemsOnPage = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PaginatedResult<Event>
            {
                Items = itemsOnPage,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public Event GetById(Guid id)
        {
            var eventItem = _events.FirstOrDefault(e => e.Id == id);
            if (eventItem == null)
            {
                throw new KeyNotFoundException($"Событие с ID {id} не найдено.");
            }

            return eventItem;
        }

        public Event Create(Event newEvent)
        {
            if (newEvent.TotalSeats <=0)
                throw new ValidationException("TotalSeats is required.");

            if (newEvent.EndAt <= newEvent.StartAt)
            {
                throw new ValidationException("Дата окончания (EndAt) должна быть позже даты начала (StartAt).");
            }

            var eventToAdd = new Event
            {
                Id = Guid.NewGuid(),
                Title = newEvent.Title,
                Description = newEvent.Description,
                StartAt = newEvent.StartAt,
                EndAt = newEvent.EndAt,
                TotalSeats = newEvent.TotalSeats,
                AvailableSeats = newEvent.TotalSeats

            };

            _events.Add(eventToAdd);
            return eventToAdd;
        }

        public Event Update(Guid id, Event updatedEvent)
        {
            var existing = _events.FirstOrDefault(e => e.Id == id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Событие с ID {id} не найдено.");
            }

            if (updatedEvent.EndAt <= updatedEvent.StartAt)
            {
                throw new ValidationException("Дата окончания (EndAt) должна быть позже даты начала (StartAt).");
            }
            existing.Title = updatedEvent.Title;
            existing.Description = updatedEvent.Description;
            existing.StartAt = updatedEvent.StartAt;
            existing.EndAt = updatedEvent.EndAt;
            existing.AvailableSeats = updatedEvent.AvailableSeats;

            return existing;
        }

        public bool Delete(Guid id)
        {

            var existing = _events.FirstOrDefault(e => e.Id == id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Событие с ID {id} не найдено.");
            }

            return _events.Remove(existing);
        }
    }
}

