using AspNetCoreApi.DataAccess;
using AspNetCoreApi.Models;
using AspNetCoreApi.Repositories;
using k8s.KubeConfigModels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventServices.Tests
{
    public class EventRepositoryTests : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

        private string? _connectionString;
        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            _connectionString = _postgres.GetConnectionString();
            await using var setupContext = CreateContextForMigration();

            await setupContext.Database.MigrateAsync();
        }

        private AppDbContext CreateContextForMigration()
        {
            if (_connectionString == null)
                throw new InvalidOperationException("Connection string is not initialized.");

            var options = new DbContextOptionsBuilder<AppDbContext>()
                 .UseNpgsql(_connectionString)
                 .Options;

            return new AppDbContext(options);
        }


        public async Task DisposeAsync()
        {
            await _postgres.DisposeAsync();
        }

        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            var context = new AppDbContext(options);
            return context;
        }

        private async Task ResetDatabaseAsync()
        {
            await using var context = CreateContext();
            await context.Database.ExecuteSqlRawAsync(
                "TRUNCATE TABLE \"Bookings\", \"Events\" RESTART IDENTITY CASCADE");
        }

        [Fact]
        public async Task GetAllAsync()
        {
            await ResetDatabaseAsync();
            await using var context = CreateContext();
            var nowUtc = DateTime.UtcNow;
            var event1 = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Концерт",
                StartAt = nowUtc,
                EndAt = nowUtc.AddHours(2)
            };

            var event2 = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Выставка",
                StartAt = nowUtc.AddDays(1),
                EndAt = nowUtc.AddDays(1).AddHours(3)
            };
            await context.Events.AddRangeAsync(event1, event2);
            await context.SaveChangesAsync();

            var repository = new EventRepository(context);
            var result = await repository.GetAllAsync();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAllAsyncFilter()
        {
            await ResetDatabaseAsync();
            await using var context = CreateContext();
            var nowUtc = DateTime.UtcNow;
            var event1 = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Концерт",
                StartAt = nowUtc,
                EndAt = nowUtc.AddHours(2)
            };

            var event2 = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Выставка",
                StartAt = nowUtc.AddDays(1),
                EndAt = nowUtc.AddDays(1).AddHours(3)
            };
            await context.Events.AddRangeAsync(event1, event2);
            await context.SaveChangesAsync();

            var repository = new EventRepository(context);
            var result = await repository.GetAllAsync(title: "Концерт", from: nowUtc.AddDays(-1), to: nowUtc.AddDays(2), page: 1, pageSize: 10);
            Assert.Equal("Концерт", result.Items.First().Title);
        }

        [Fact]
        public async Task FilterByStartDate()
        {
            await ResetDatabaseAsync();
            await using var context = CreateContext();
            var nowUtc = DateTime.UtcNow;

            var created1 = new Event { Title = "Конференция", StartAt = nowUtc, EndAt = nowUtc.AddHours(1), TotalSeats = 1 };
            var created2 = new Event { Title = "Семинар", StartAt = nowUtc - TimeSpan.FromDays(1), EndAt = nowUtc.AddHours(1), TotalSeats = 1 };

            await context.Events.AddRangeAsync(created1, created2);
            await context.SaveChangesAsync();

            var repository = new EventRepository(context);
            var result = await repository.GetAllAsync(from: created1.StartAt);

            Assert.Single(result.Items);
            Assert.Equal(created1.StartAt, result.Items.First().StartAt);
        }

        [Fact]
        public async Task FilterByEndDate()
        {
            await ResetDatabaseAsync();
            await using var context = CreateContext();
            var nowUtc = DateTime.UtcNow;

            var created1 = new Event { Title = "Конференция", StartAt = nowUtc, EndAt = nowUtc.AddHours(1), TotalSeats = 1 };
            var created2 = new Event { Title = "Семинар", StartAt = nowUtc - TimeSpan.FromDays(1), EndAt = nowUtc.AddHours(48), TotalSeats = 1 };

            await context.Events.AddRangeAsync(created1, created2);
            await context.SaveChangesAsync();

            var repository = new EventRepository(context);
            var result = await repository.GetAllAsync(to: created1.EndAt);

            Assert.Single(result.Items);
            Assert.Equal(created1.EndAt, result.Items.First().EndAt);
        }

        [Fact]
        public async Task Pagination_ReturnsCorrectPage()
        {
            await ResetDatabaseAsync();
            await using var context = CreateContext();
            var nowUtc = DateTime.UtcNow;
            var eventsToAdd = new List<Event>();

            for (int i = 0; i < 25; i++)
            {
                var newEvent = new Event { Title = $"Event {i}", StartAt = nowUtc, EndAt = nowUtc.AddHours(1), TotalSeats = 1 };
                eventsToAdd.Add(newEvent);
            }
            await context.Events.AddRangeAsync(eventsToAdd);
            await context.SaveChangesAsync();

            var repository = new EventRepository(context);

            var page1Result = await repository.GetAllAsync(page: 1, pageSize: 10); 
            var page2Result = await repository.GetAllAsync(page: 2, pageSize: 10);

            Assert.Equal(10, page1Result.Items.Count);
            Assert.Equal(10, page2Result.Items.Count);

          
        }

        [Fact]
        public async Task GetById()
        {
            await ResetDatabaseAsync();
            await using var context = CreateContext();
            var nowUtc = DateTime.UtcNow;
            var event1 = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Концерт",
                StartAt = nowUtc,
                EndAt = nowUtc.AddHours(2)
            };

            await context.Events.AddAsync(event1);
            await context.SaveChangesAsync();

            var repository = new EventRepository(context);
            var result = await repository.GetByIdAsync(event1.Id);
            Assert.Equal(event1.Id, result.Id);
        }

        [Fact]
        public async Task CreateAndDeleteAsync_AddsAndRemovesEntity_UsingNewContextForVerification()
        {
            await ResetDatabaseAsync();

            await using var contextForAdding = CreateContext();
            var nowUtc = DateTime.UtcNow;
            var event1 = new Event { Id = Guid.NewGuid(), Title = "Концерт", StartAt = nowUtc, EndAt = nowUtc.AddHours(2) };

            await contextForAdding.Events.AddAsync(event1);
            await contextForAdding.SaveChangesAsync();


            await using var newContextAfterAdd = CreateContext();
            var eventFromDb = await newContextAfterAdd.Events.FindAsync(event1.Id);

            Assert.NotNull(eventFromDb); 


            var repository = new EventRepository(contextForAdding); 
            var isDeleted = await repository.DeleteAsync(event1.Id);

            Assert.True(isDeleted);


            await using var newContextAfterDelete = CreateContext();

            eventFromDb = await newContextAfterDelete.Events.FindAsync(event1.Id);

            Assert.Null(eventFromDb); 
        }

        [Fact]
        public async Task UpdateAsync_UpdatesEventSuccessfully()
        {
            await ResetDatabaseAsync();
            await using var context = CreateContext();
            var repository = new EventRepository(context);

            var originalEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Оригинальный Концерт",
                Description = "Описание до обновления",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(3),
                AvailableSeats = 100,
            };

            await context.Events.AddAsync(originalEvent);
            await context.SaveChangesAsync();

            var updatedData = new Event 
            {
                Id = originalEvent.Id,
                Title = "Обновленный Концерт",
                Description = "Новое описание",
                StartAt = DateTime.UtcNow.AddDays(1),
                EndAt = DateTime.UtcNow.AddDays(1).AddHours(4),
                AvailableSeats = 50, 
            };

            var updatedEventFromRepo = await repository.UpdateAsync(originalEvent.Id, updatedData);

            Assert.NotNull(updatedEventFromRepo);
            Assert.Equal(updatedData.Title, updatedEventFromRepo.Title);
            Assert.Equal(updatedData.Description, updatedEventFromRepo.Description);
            Assert.Same(originalEvent, updatedEventFromRepo);


            await using var newContextForVerification = CreateContext();

            var eventFromDb = await newContextForVerification.Events.FindAsync(originalEvent.Id);

            Assert.NotNull(eventFromDb);
            Assert.Equal(updatedData.Title, eventFromDb.Title);
            Assert.Equal(updatedData.Description, eventFromDb.Description);
            Assert.Equal(updatedData.AvailableSeats, eventFromDb.AvailableSeats);
            Assert.Equal(updatedData.TotalSeats, eventFromDb.TotalSeats);
        }


        [Fact]
        public async Task UpdateAsync_WithNonExistentId_ThrowsKeyNotFoundException()
        {
            // Arrange
            await ResetDatabaseAsync();
            await using var context = CreateContext();
            var repository = new EventRepository(context);

            var nonExistentId = Guid.NewGuid();
            var someEventData = new Event { Id = nonExistentId }; // Данные не важны, т.к. сущность не будет найдена

            // Act & Assert: Проверяем, что выбрасывается ожидаемое исключение
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                repository.UpdateAsync(nonExistentId, someEventData)
            );

            // Можно также проверить текст сообщения, если это важно
            Assert.Contains(nonExistentId.ToString(), exception.Message);
        }


        [Fact]
        public async Task UpdateAsync_WithInvalidDates_ThrowsValidationException()
        {
            // Arrange: Создаем и сохраняем исходное событие
            await ResetDatabaseAsync();
            await using var context = CreateContext();
            var repository = new EventRepository(context);

            var originalEvent = new Event {
                Id = Guid.NewGuid(),
                Title = "Оригинальный Концерт",
                Description = "Описание до обновления",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(3),
                AvailableSeats = 100,
            };
            await context.Events.AddAsync(originalEvent);
            await context.SaveChangesAsync();

            var invalidData = new Event {
                Id = Guid.NewGuid(),
                Title = "Оригинальный Концерт",
                Description = "Описание до обновления",
                StartAt = DateTime.UtcNow.AddDays(4),
                EndAt = DateTime.UtcNow.AddHours(3),
                AvailableSeats = 100,
            };

            var exception = await Assert.ThrowsAsync<ValidationException>(() =>
                repository.UpdateAsync(originalEvent.Id, invalidData)
            );

            Assert.Equal("Дата окончания (EndAt) должна быть позже даты начала (StartAt).", exception.Message);
        }
    }
}
