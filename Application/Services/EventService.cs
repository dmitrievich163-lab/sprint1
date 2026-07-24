using Application.Repositories;
using Domain;
using System.ComponentModel.DataAnnotations;

namespace Application.Services
{
    public class EventService: IEventService
    {

        private readonly IEventRepository _eventRepository; 

        public EventService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }
        public async Task<IEnumerable<Event>> GetAll()
        {
            return await _eventRepository.GetAllAsync();
        }

        public async Task<PaginatedResult<Event>> GetAll(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10)
        {
            return await _eventRepository.GetAllAsync(title, from, to, page, pageSize);
            
        }

        public async Task <Event> GetById(Guid id)
        {

            var eventItem = await _eventRepository.GetByIdAsync(id);
            if (eventItem == null)
            {
                throw new KeyNotFoundException($"Событие с ID {id} не найдено.");
            }

            return eventItem;
        }

        public async Task <Event> Create(Event newEvent)
        {
            if (newEvent.TotalSeats <= 0)
                throw new ValidationException("TotalSeats is required.");

            if (newEvent.EndAt <= newEvent.StartAt)
            {
                throw new ValidationException("Дата окончания (EndAt) должна быть позже даты начала (StartAt).");
            }
            newEvent.AvailableSeats = newEvent.TotalSeats;

            return await _eventRepository.CreateAsync(newEvent);
        }

        public async Task <Event> Update(Guid id, Event updatedEvent)
        {
            var existing = await _eventRepository.GetByIdAsync(id)??
                           throw new KeyNotFoundException($"Событие с ID {id} не найдено.");

            if (updatedEvent.EndAt <= updatedEvent.StartAt)
            {
                throw new ValidationException("Дата окончания (EndAt) должна быть позже даты начала (StartAt).");
            }

            // Обновляем свойства сущности. EF Core отследит изменения.
            existing.Title = updatedEvent.Title;
            existing.Description = updatedEvent.Description;
            existing.StartAt = updatedEvent.StartAt;
            existing.EndAt = updatedEvent.EndAt;
            existing.AvailableSeats = updatedEvent.AvailableSeats;
            return await _eventRepository.UpdateAsync(id, updatedEvent);
        }

        public async Task <bool> Delete(Guid id)
        {
            var existing = await _eventRepository.GetByIdAsync(id) ??
                           throw new KeyNotFoundException($"Событие с ID {id} не найдено.");
            return await _eventRepository.DeleteAsync(id);
        }
    }
}

