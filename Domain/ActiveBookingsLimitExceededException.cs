using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class ActiveBookingsLimitExceededException:DomainException
    {
        public int Limit { get; }

        public ActiveBookingsLimitExceededException(int limit)
            : base($"Превышен лимит активных броней ({limit}).")
        {
            Limit = limit;
        }
    }
}
