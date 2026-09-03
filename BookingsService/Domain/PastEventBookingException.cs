namespace BookingsService.Domain;

public class PastEventBookingException : DomainException
{
    public PastEventBookingException()
        : base("Нельзя забронировать место на прошедшее событие.") { }
}
