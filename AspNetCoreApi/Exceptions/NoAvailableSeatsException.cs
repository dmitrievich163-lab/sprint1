namespace AspNetCoreApi.Exceptions
{
    public class NoAvailableSeatsException:Exception
    {
        public NoAvailableSeatsException()
        {
        }

        public NoAvailableSeatsException(string message) : base(message)
        {
        }

        public NoAvailableSeatsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    
}
}
