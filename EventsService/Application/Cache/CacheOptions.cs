namespace EventsService.Application.Cache;

public class CacheOptions
{
    public static readonly string EventKeyPrefix = "event:";
    public static readonly string TopEventsKey = "events:top10";

    public TimeSpan EventTtl { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan TopEventsTtl { get; set; } = TimeSpan.FromMinutes(1);
}
