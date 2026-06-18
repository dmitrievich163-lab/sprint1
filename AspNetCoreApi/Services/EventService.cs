using AspNetCoreApi.DataAccess;
using AspNetCoreApi.Models;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreApi.Services
{
    public class EventService: IEventService
    {

        private readonly AppDbContext _context;

        // AppDbContext внедряется через конструктор
        public EventService(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Event>> GetAll()
        {
            return _context.Events;
        }

        public async Task<PaginatedResult<Event>> GetAll(string? title = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 10)
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

        public async Task <Event> GetById(Guid id)
        {
            var eventItem = _context.Events.FirstOrDefault(e => e.Id == id);
            if (eventItem == null)
            {
                throw new KeyNotFoundException($"Событие с ID {id} не найдено.");
            }

            return eventItem;
        }

        public async Task <Event> Create(Event newEvent)
        {
            if (newEvent.TotalSeats <=0)
                throw new ValidationException("TotalSeats is required.");

            if (newEvent.EndAt <= newEvent.StartAt)
            {
                throw new ValidationException("Дата окончания (EndAt) должна быть позже даты начала (StartAt).");
            }
            newEvent.AvailableSeats = newEvent.TotalSeats;
            await _context.Events.AddAsync(newEvent);

            // Сохраняем изменения в базе данных.
            await _context.SaveChangesAsync();

            return newEvent;
        }

        public async Task <Event> Update(Guid id, Event updatedEvent)
        {
            var existing = _context.Events.FirstOrDefault(e => e.Id == id);
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

            await _context.SaveChangesAsync();

            return existing;
        }

        public async Task <bool> Delete(Guid id)
        {

            var existing = _context.Events.FirstOrDefault(e => e.Id == id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Событие с ID {id} не найдено.");
            }
            _context.Events.Remove(existing);

            int rowsAffected = await _context.SaveChangesAsync();

            // Возвращаем true, если хотя бы одна строка была удалена.
            return rowsAffected > 0;
        }
    }
}

