using AspNetCoreApi.Models;
using AspNetCoreApi.Services;
using System.ComponentModel.DataAnnotations;
using Xunit.Abstractions;

namespace EventServices.Tests
{
    public class EventServiceTests
    {
        private readonly EventService _service;
        private readonly ITestOutputHelper _output;

        public EventServiceTests(ITestOutputHelper output)
        {
            _output = output;
            _service = new EventService();
        }

        // --- УСПЕШНЫЕ СЦЕНАРИИ ---

        [Fact]
        public void Create_AddsEventToCollection()
        {
            var newEvent = new Event { Title = "Test Event", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) };
            int initialCount = _service.GetAll().Count();

            var createdEvent = _service.Create(newEvent);

            Assert.NotNull(createdEvent.Id); 
            Assert.Equal(initialCount + 1, _service.GetAll().Count());
        }

        [Fact]
        public void GetAll_ReturnsAllEvents()
        {
            _service.Create(new Event { Title = "Event 1", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) });
            _service.Create(new Event { Title = "Event 2", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) });

            var events = _service.GetAll();

            Assert.Equal(2, events.Count());
        }

        [Fact]
        public void GetById_ReturnsCorrectEvent()
        {
            // Arrange
            var created = _service.Create(new Event { Title = "Find Me", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) });

            // Act
            var found = _service.GetById(created.Id);

            // Assert
            Assert.NotNull(found);
            Assert.Equal(created.Id, found.Id);
        }

        [Fact]
        public void Update()
        {
            // Arrange
            var created = _service.Create(new Event { Title = "Find Me", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) });

            // Act
            var update = _service.Update(created.Id, new Event { Title = "Find Me update", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) });
            var found = _service.GetById(created.Id);
            // Assert
            Assert.NotNull(found);
            Assert.Equal(update.Title, found.Title);
        }

        [Fact]
        public void Delete()
        {
            // Arrange
            var created = _service.Create(new Event { Title = "Find Me", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) });

            // Act
            var delete = _service.Delete(created.Id);
            var deleted = _service.GetById(created.Id);
            // Assert
            Assert.Null(deleted);
        }

        [Fact]
        public void FilterByTitle()
        {
            _service.Create(new Event { Title = "Конференция", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) });
            _service.Create(new Event { Title = "Семинар", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) });

            var result = _service.GetAll(title: "конф");

            Assert.Single(result.Items);
            Assert.Equal("Конференция", result.Items.First().Title);
        }

        [Fact]
        public void FilterByStartDate()
        {
            var created1 = _service.Create(new Event { Title = "Конференция", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) });
            var created2 = _service.Create(new Event { Title = "Семинар", StartAt = DateTime.Now-TimeSpan.FromDays(1), EndAt = DateTime.Now.AddHours(1) });

            var result = _service.GetAll(from: created1.StartAt);

            Assert.Single(result.Items);
            Assert.Equal(created1.StartAt, result.Items.First().StartAt);
        }

        [Fact]
        public void FilterByEndDate()
        {
            var created1 = _service.Create(new Event { Title = "Конференция", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) });
            var created2 = _service.Create(new Event { Title = "Семинар", StartAt = DateTime.Now - TimeSpan.FromDays(1), EndAt = DateTime.Now.AddHours(48) });

            var result = _service.GetAll(to: created1.EndAt);

            Assert.Single(result.Items);
            Assert.Equal(created1.EndAt, result.Items.First().EndAt);
        }

        [Fact]
        public void Pagination_ReturnsCorrectPage()
        {
            for (int i = 0; i < 25; i++)
            {
                _service.Create(new Event { Title = $"Event {i}", StartAt = DateTime.Now, EndAt = DateTime.Now.AddHours(1) });
            }

            var page1Result = _service.GetAll(page: 1, pageSize: 10);
            var page2Result = _service.GetAll(page: 2, pageSize: 10);

            Assert.Equal(10, page1Result.Items.Count);
            Assert.Equal(10, page2Result.Items.Count);

            Assert.Equal("Event 0", page1Result.Items.First().Title);
            Assert.Equal("Event 10", page2Result.Items.First().Title);
        }

        [Fact]
        public void CombinedFiltering_ReturnsOnlyMatchingEvent()
        {
            _service.Create(new Event
            {
                Title = "Семинар по дизайну",
                StartAt = DateTime.Now.AddDays(-2),
                EndAt = DateTime.Now.AddDays(5) 
            });

            _service.Create(new Event
            {
                Title = "Конференция по .NET", 
                StartAt = DateTime.Now.AddDays(-10), 
                EndAt = DateTime.Now.AddDays(-5)
            });

            _service.Create(new Event
            {
                Title = "Конференция по .NET", 
                StartAt = DateTime.Now.AddDays(1),
                EndAt = DateTime.Now.AddDays(10)
            });

            var expectedEvent = _service.Create(new Event
            {
                Title = "Конференция по .NET",
                StartAt = DateTime.Now.AddDays(-1), 
                EndAt = DateTime.Now.AddDays(2)
            });


            string searchTitle = "конф"; 
            DateTime filterFrom = DateTime.Now.AddDays(-3); 
            DateTime filterTo = DateTime.Now.AddDays(3);   

            var result = _service.GetAll(
                title: searchTitle,
                from: filterFrom,
                to: filterTo);


            Assert.Single(result.Items);

            var actualEvent = result.Items.First();

            Assert.Equal(expectedEvent.Id, actualEvent.Id);
            Assert.Equal(expectedEvent.Title, actualEvent.Title);
        }


        [Fact]
        public void GetById_NonExistentId_ReturnsNull()
        {
            var result = _service.GetById(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public void Update_NonExistentId_ReturnsNull()
        {
            var result = _service.Update(Guid.NewGuid(), new Event { Title = "Invalid", StartAt = DateTime.Now.AddDays(1), EndAt = DateTime.Now });

            Assert.Null(result);
        }

        [Fact]
        public void Create_WithEndDateBeforeStartDate_ThrowsValidationException()
        {
            var invalidEvent = new Event
            {
                Title = "Некорректное событие",
                StartAt = DateTime.Now.AddDays(1), 
                EndAt = DateTime.Now               
            };

            var exception = Assert.Throws<ValidationException>(() => _service.Create(invalidEvent));

            _output.WriteLine(exception.Message);
            Assert.Contains("Дата окончания", exception.Message);
            Assert.Contains("должна быть позже", exception.Message);
        }

        [Fact]
        public void Update_WithEndDateBeforeStartDate_ThrowsValidationException()
        {
            var existingEvent = _service.Create(new Event
            {
                Title = "Событие для обновления",
                StartAt = DateTime.Now.AddDays(1), 
                EndAt = DateTime.Now.AddDays(2)    
            });

            var updatedData = new Event
            {
                Title = "Новое название",
                StartAt = DateTime.Now.AddDays(5), 
                EndAt = DateTime.Now.AddDays(3)    
            };



            var exception = Assert.Throws<ValidationException>(() =>
                _service.Update(existingEvent.Id, updatedData)
            );

            Assert.Contains("Дата окончания", exception.Message);
            Assert.Contains("должна быть позже", exception.Message);
        }
    }
}

