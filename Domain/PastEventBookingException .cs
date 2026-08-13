using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class PastEventBookingException: DomainException
    {
        public PastEventBookingException()
        : base("Нельзя создать бронь для события, которое уже произошло.")
        {
        }
    }
}
