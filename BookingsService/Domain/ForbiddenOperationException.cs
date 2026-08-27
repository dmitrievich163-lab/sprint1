namespace BookingsService.Domain;

public class ForbiddenOperationException : DomainException
{
    public ForbiddenOperationException(string operation)
        : base($"Доступ запрещён: {operation}.") { }
}
