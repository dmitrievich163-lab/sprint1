using AspNetCoreApi.Models;

namespace AspNetCoreApi.Services
{
    public interface IEventService
    {
        IEnumerable<Event> GetAll();
        PaginatedResult<Event> GetAll(
       string? title = null,
       DateTime? from = null,
       DateTime? to = null,
       int page = 1,
       int pageSize = 10);

        Event GetById(Guid id);
        Event Create(Event newEvent);
        Event Update(Guid id, Event updatedEvent);
        bool Delete(Guid id);
    }
}
