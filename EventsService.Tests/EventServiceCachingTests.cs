using EventsService.Application.Cache;
using EventsService.Application.Repositories;
using EventsService.Application.Services;
using EventsService.Domain;
using Moq;

namespace EventsService.Tests;

public class EventServiceCachingTests
{
    private readonly Mock<IEventRepository> _repositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly CacheOptions _cacheOptions;
    private readonly EventService _service;

    public EventServiceCachingTests()
    {
        _repositoryMock = new Mock<IEventRepository>();
        _cacheMock = new Mock<ICacheService>();
        _cacheOptions = new CacheOptions
        {
            EventTtl = TimeSpan.FromSeconds(300),
            TopEventsTtl = TimeSpan.FromSeconds(60)
        };
        _service = new EventService(_repositoryMock.Object, _cacheMock.Object, _cacheOptions);
    }

    [Fact]
    public async Task GetById_CacheHit_DoesNotCallRepository()
    {
        var id = Guid.NewGuid();
        var cached = new Event { Id = id, Title = "Событие" };
        _cacheMock
            .Setup(c => c.GetAsync<Event>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await _service.GetById(id);

        Assert.Same(cached, result);
        _repositoryMock.Verify(r => r.GetByIdAsync(id), Times.Never);
    }

    [Fact]
    public async Task GetById_CacheMiss_LoadsFromRepositoryAndPopulatesCache()
    {
        var id = Guid.NewGuid();
        var fromRepo = new Event { Id = id, Title = "Концерт" };
        _cacheMock
            .Setup(c => c.GetAsync<Event>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(fromRepo);

        var result = await _service.GetById(id);

        Assert.Same(fromRepo, result);
        _repositoryMock.Verify(r => r.GetByIdAsync(id), Times.Once);
        _cacheMock.Verify(
            c => c.SetAsync($"event:{id}", fromRepo, _cacheOptions.EventTtl, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetById_CacheMissEventNotFound_DoesNotWriteCacheAndThrows()
    {
        var id = Guid.NewGuid();
        _cacheMock
            .Setup(c => c.GetAsync<Event>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Event?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.GetById(id));
        _cacheMock.Verify(c => c.SetAsync<Event>(It.IsAny<string>(), It.IsAny<Event>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_InvalidatesEventCache()
    {
        var id = Guid.NewGuid();
        var existing = new Event { Id = id, Title = "Старое" };
        var updated = new Event { Id = id, Title = "Новое", StartAt = DateTime.UtcNow, EndAt = DateTime.UtcNow.AddHours(2) };

        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Event>())).ReturnsAsync(existing);

        var result = await _service.Update(id, updated);

        Assert.NotNull(result);
        _cacheMock.Verify(c => c.RemoveAsync($"event:{id}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_InvalidatesEventCache()
    {
        var id = Guid.NewGuid();
        var existing = new Event { Id = id, Title = "Новое" };

        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existing);
        _repositoryMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

        var result = await _service.Delete(id);

        Assert.True(result);
        _cacheMock.Verify(c => c.RemoveAsync($"event:{id}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_EventNotFound_DoesNotRemoveFromCache()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Event?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.Delete(id));
        _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
