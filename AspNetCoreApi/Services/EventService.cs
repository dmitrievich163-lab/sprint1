using AspNetCoreApi.DataAccess;
using AspNetCoreApi.Models;
using AspNetCoreApi.Repositories;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreApi.Services
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
            return await _eventRepository.CreateAsync(newEvent);
        }

        public async Task <Event> Update(Guid id, Event updatedEvent)
        {
            return await _eventRepository.UpdateAsync(id, updatedEvent);
        }

        public async Task <bool> Delete(Guid id)
        {
            return await _eventRepository.DeleteAsync(id);
        }
    }
}

