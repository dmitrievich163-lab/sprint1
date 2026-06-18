using AspNetCoreApi.DataAccess;
using AspNetCoreApi.Models;
using AspNetCoreApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace EventServices.Tests
{
    public class EventServiceTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITestOutputHelper _output;

        public EventServiceTests(ITestOutputHelper output)
        {
            _output = output;
            var dbName = Guid.NewGuid().ToString(); // Уникальное имя для каждой сессии тестов

            var services = new ServiceCollection();
            services.AddDbContext<AspNetCoreApi.DataAccess.AppDbContext>(options =>
                options.UseInMemoryDatabase(dbName)); // Используем InMemory-провайдер
            services.AddScoped<IEventService, EventService>(); // Регистрируем сервис

            _serviceProvider = services.BuildServiceProvider();
        }

        // --- УСПЕШНЫЕ СЦЕНАРИИ ---

        [Fact]
        public async Task Create_AddsEventToCollection()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 };

            var createdEvent = await eventService.Create(newEvent);

            using var checkScope = _serviceProvider.CreateScope();
            var dbContext = checkScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var eventInDb = await dbContext.Events.FindAsync(createdEvent.Id);

            Assert.NotNull(eventInDb);
            Assert.Equal("Test Event", eventInDb.Title);
        }
    


[Fact]
public async Task GetAll_ReturnsAllEvents()
{
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            await eventService.Create(new Event { Title = "Event 1", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 });
            await eventService.Create(new Event { Title = "Event 2", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 });

    var events = await eventService.GetAll();

    Assert.Equal(2, events.Count());
}

        [Fact]
        public async Task GetById_ReturnsCorrectEvent()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            // Arrange
            var created = await eventService.Create(new Event { Title = "Find Me", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 });

            // Act
            var found = await eventService.GetById(created.Id);

            // Assert
            Assert.NotNull(found);
            Assert.Equal(created.Id, found.Id);
        }

        [Fact]
        public async Task Update()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
            // Arrange
            var created = await eventService.Create(new Event { Title = "Find Me", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 });

            // Act
            var update = await eventService.Update(created.Id, new Event { Title = "Find Me update", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 });
            var found = await eventService.GetById(created.Id);
            // Assert
            Assert.NotNull(found);
            Assert.Equal(update.Title, found.Title);
        }

        [Fact]
        public async Task Delete_ExistingEvent_ReturnsTrueAndEventIsRemoved()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            var created = await eventService.Create(new Event { Title = "Find Me", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 });

            var result = await eventService.Delete(created.Id);
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => eventService.GetById(created.Id));
            Assert.True(result);
            Assert.IsType<KeyNotFoundException>(exception);
        }

        [Fact]
        public async Task Delete_NonExistingEvent_ThrowsKeyNotFoundException()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            var nonExistentId = Guid.NewGuid();

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => eventService.Delete(nonExistentId));
            Assert.Contains(nonExistentId.ToString(), exception.Message);
        }

        [Fact]
        public async Task FilterByTitle()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            await eventService.Create(new Event { Title = "Конференция", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 });
            await eventService.Create(new Event { Title = "Семинар", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 });

            var result = await eventService.GetAll(title: "конф");

            Assert.Single(result.Items);
            Assert.Equal("Конференция", result.Items.First().Title);
        }

        [Fact]
        public async Task FilterByStartDate()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            var created1 = await eventService.Create(new Event { Title = "Конференция", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 });
            var created2 = await eventService.Create(new Event { Title = "Семинар", StartAt = DateTime.Now - TimeSpan.FromDays(1), EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 });

            var result = await eventService.GetAll(from: created1.StartAt);

            Assert.Single(result.Items);
            Assert.Equal(created1.StartAt, result.Items.First().StartAt);
        }

        [Fact]
        public async Task FilterByEndDate()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            var created1 = await eventService.Create(new Event { Title = "Конференция", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 });
            var created2 = await eventService.Create(new Event { Title = "Семинар", StartAt = DateTime.Now - TimeSpan.FromDays(1), EndAt = DateTime.Now.AddHours(48), TotalSeats = 1 });

            var result = await eventService.GetAll(to: created1.EndAt);

            Assert.Single(result.Items);
            Assert.Equal(created1.EndAt, result.Items.First().EndAt);
        }

        [Fact]
        public async Task Pagination_ReturnsCorrectPage()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            for (int i = 0; i < 25; i++)
            {
                await eventService.Create(new Event { Title = $"Event {i}", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1), TotalSeats = 1 });
            }

            var page1Result = await eventService.GetAll(page: 1, pageSize: 10);
            var page2Result = await eventService.GetAll(page: 2, pageSize: 10);

            Assert.Equal(10, page1Result.Items.Count);
            Assert.Equal(10, page2Result.Items.Count);

            Assert.Equal("Event 0", page1Result.Items.First().Title);
            Assert.Equal("Event 10", page2Result.Items.First().Title);
        }

        [Fact]
        public async Task CombinedFiltering_ReturnsOnlyMatchingEvent()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            await eventService.Create(new Event
            {
                Title = "Семинар по дизайну",
                StartAt = DateTime.Now.AddDays(-2),
                EndAt = DateTime.Now.AddDays(5),
                TotalSeats = 1
            });

            await eventService.Create(new Event
            {
                Title = "Конференция по .NET",
                StartAt = DateTime.Now.AddDays(-10),
                EndAt = DateTime.Now.AddDays(-5),
                TotalSeats = 1
            });

            await eventService.Create(new Event
            {
                Title = "Конференция по .NET",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(10),
                TotalSeats = 1
            });

            var expectedEvent = await eventService.Create(new Event
            {
                Title = "Конференция по .NET",
                StartAt = DateTime.Now.AddDays(-1),
                EndAt = DateTime.Now.AddDays(2),
                TotalSeats = 1
            });


            string searchTitle = "конф";
            DateTime filterFrom = DateTime.Now.AddDays(-3);
            DateTime filterTo = DateTime.Now.AddDays(3);

            var result = await eventService.GetAll(
                title: searchTitle,
                from: filterFrom,
                to: filterTo);


            Assert.Single(result.Items);

            var actualEvent = result.Items.First();

            Assert.Equal(expectedEvent.Id, actualEvent.Id);
            Assert.Equal(expectedEvent.Title, actualEvent.Title);
        }


        [Fact]
        public async Task GetById_NonExistentId_ReturnsExcept()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => eventService.GetById(Guid.NewGuid()));
           
        }

        [Fact]
        public async Task Update_NonExistentId_ReturnsExcept()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => eventService.Update(Guid.NewGuid(), new Event { Title = "Invalid", StartAt = DateTime.Now.AddDays(1), EndAt = DateTime.Now, TotalSeats = 1 }));

        }

        [Fact]
        public async Task Create_WithEndDateBeforeStartDate_ThrowsValidationException()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();


            var invalidEvent = new Event
            {
                Title = "Некорректное событие",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now,
                TotalSeats = 1
            };
            var exception = await Assert.ThrowsAsync<ValidationException>(() => eventService.Create(invalidEvent));

            _output.WriteLine(exception.Message);
            Assert.Contains("Дата окончания", exception.Message);
            Assert.Contains("должна быть позже", exception.Message);
        }

        [Fact]
        public async Task Update_WithEndDateBeforeStartDate_ThrowsValidationException()
        {
            using var scope = _serviceProvider.CreateScope();
            var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();

            var existingEvent = await eventService.Create(new Event
            {
                Title = "Событие для обновления",
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(2),
                TotalSeats = 1
            });

            var updatedData = new Event
            {
                Title = "Новое название",
                StartAt = DateTime.Now.AddDays(5),
                EndAt = DateTime.Now.AddDays(3),
                TotalSeats = 1
            };

            var exception = await Assert.ThrowsAsync<ValidationException>(() => eventService.Update(existingEvent.Id, updatedData));

            Assert.Contains("Дата окончания", exception.Message);
            Assert.Contains("должна быть позже", exception.Message);
        }
    }
}

