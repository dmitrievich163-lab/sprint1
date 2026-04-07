using AspNetCoreApi.Models;

namespace AspNetCoreApi.Services
{
    public interface IEventService
    {
        IEnumerable<Event> GetAll();
        Event GetById(Guid id);
        Event Create(Event newEvent);
        Event Update(Guid id, Event updatedEvent);
        bool Delete(Guid id);
    }
}
