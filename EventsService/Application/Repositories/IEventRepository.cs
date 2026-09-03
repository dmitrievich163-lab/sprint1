using EventsService.Domain;

namespace EventsService.Application.Repositories;

public interface IEventRepository
{
    Task<IEnumerable<Event>> GetAllAsync();
    Task<PaginatedResult<Event>> GetAllAsync(string? title, DateTime? from, DateTime? to, int page, int pageSize);
    Task<Event?> GetByIdAsync(Guid id);
    Task<Event> CreateAsync(Event newEvent);
    Task<Event> UpdateAsync(Guid id, Event updatedEvent);
    Task<bool> DeleteAsync(Guid id);
    Task SaveChangesAsync(CancellationToken ct = default);
}
