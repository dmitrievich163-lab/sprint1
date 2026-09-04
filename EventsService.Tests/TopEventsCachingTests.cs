using EventsService.Application.Cache;
using EventsService.Application.Repositories;
using EventsService.Application.Services;
using EventsService.Domain;
using Moq;

namespace EventsService.Tests;

public class TopEventsCachingTests
{
    [Fact]
    public async Task GetTop10_CacheHit_DoesNotCallRepository()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var options = new CacheOptions();
        var service = new EventService(repositoryMock.Object, cacheMock.Object, options);

        var cached = new[] { new Event { Title = "A" } };
        cacheMock
            .Setup(c => c.GetAsync<IEnumerable<Event>>(CacheOptions.TopEventsKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var result = await service.GetTop10();

        Assert.Same(cached, result);
        repositoryMock.Verify(r => r.GetTop10Async(), Times.Never);
    }

    [Fact]
    public async Task GetTop10_CacheMiss_LoadsFromRepositoryAndPopulatesCache()
    {
        var repositoryMock = new Mock<IEventRepository>();
        var cacheMock = new Mock<ICacheService>();
        var options = new CacheOptions { TopEventsTtl = TimeSpan.FromSeconds(60) };
        var service = new EventService(repositoryMock.Object, cacheMock.Object, options);

        var fromRepo = new[] { new Event { Title = "Топ1" } };
        cacheMock
            .Setup(c => c.GetAsync<IEnumerable<Event>>(CacheOptions.TopEventsKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Event>?)null);
        repositoryMock.Setup(r => r.GetTop10Async()).ReturnsAsync(fromRepo);

        var result = await service.GetTop10();

        Assert.Same(fromRepo, result);
        repositoryMock.Verify(r => r.GetTop10Async(), Times.Once);
        cacheMock.Verify(
            c => c.SetAsync(CacheOptions.TopEventsKey, It.IsAny<IEnumerable<Event>>(), options.TopEventsTtl, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
