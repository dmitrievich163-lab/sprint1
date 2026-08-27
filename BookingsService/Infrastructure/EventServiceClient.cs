using System.Net.Http.Json;
using BookingsService.Application.Services;

namespace BookingsService.Infrastructure;

public class EventServiceClient : IEventServiceClient
{
    private readonly HttpClient _httpClient;

    public EventServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EventDto?> GetEventAsync(Guid eventId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/events/{eventId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EventDto>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
