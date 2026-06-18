using AspNetCoreApi.Models;

namespace AspNetCoreApi.Services
{
    public interface IEventService
    {
        Task <IEnumerable<Event>> GetAll();
        Task <PaginatedResult<Event>> GetAll(
       string? title = null,
       DateTime? from = null,
       DateTime? to = null,
       int page = 1,
       int pageSize = 10);

        Task <Event> GetById(Guid id);
        Task<Event> Create(Event newEvent);
        Task<Event> Update(Guid id, Event updatedEvent);
        Task<bool> Delete(Guid id);
    }
}
