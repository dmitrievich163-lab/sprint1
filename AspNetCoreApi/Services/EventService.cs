using AspNetCoreApi.Models;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreApi.Services
{
    public class EventService: IEventService
    {
        private static readonly List<Event> _events = new();

        public IEnumerable<Event> GetAll()
        {
            return _events;
        }

        public Event GetById(Guid id)
        {
            return _events.FirstOrDefault(e => e.Id == id);
        }

        public Event Create(Event newEvent)
        {
            if (newEvent.EndAt <= newEvent.StartAt)
            {
                throw new ValidationException("Дата окончания (EndAt) должна быть позже даты начала (StartAt).");
            }
            _events.Add(newEvent);
            return newEvent;
        }

        public Event Update(Guid id, Event updatedEvent)
        {
            var existing = _events.FirstOrDefault(e => e.Id == id);
            if (existing == null) return null;

            if (updatedEvent.EndAt <= updatedEvent.StartAt)
            {
                throw new ValidationException("Дата окончания (EndAt) должна быть позже даты начала (StartAt).");
            }
            existing.Title = updatedEvent.Title;
            existing.Description = updatedEvent.Description;
            existing.StartAt = updatedEvent.StartAt;
            existing.EndAt = updatedEvent.EndAt;

            return existing;
        }

        public bool Delete(Guid id)
        {
            var existing = _events.FirstOrDefault(e => e.Id == id);
            if (existing == null) return false;

            return _events.Remove(existing);
        }
    }
}

