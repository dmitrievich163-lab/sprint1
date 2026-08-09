using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Infrastructure.DataAccess;
using Domain;

namespace Application.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly AppDbContext _context;

        public EventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Event>> GetAllAsync()
        {
            return await _context.Events.ToListAsync();
        }

        public async Task<PaginatedResult<Event>> GetAllAsync(string? title=null, DateTime? from=null, DateTime? to=null, int page=1, int pageSize=10)
        {
            var query = _context.Events.AsQueryable();

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

            int totalCount = await query.CountAsync();
            var itemsOnPage = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginatedResult<Event>
            {
                Items = itemsOnPage,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<Event?> GetByIdAsync(Guid id)
        {
            return await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Event> CreateAsync(Event newEvent)
        {
            await _context.Events.AddAsync(newEvent);
            await _context.SaveChangesAsync();

            return newEvent;
        }

        public async Task<Event> UpdateAsync(Guid id, Event updatedEvent)
        {
            await _context.SaveChangesAsync();

            var existing = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var existing = await _context.Events.FirstOrDefaultAsync(e => e.Id == id);

            _context.Events.Remove(existing);
            int rowsAffected = await _context.SaveChangesAsync();

            return rowsAffected > 0;
        }
    }
}
