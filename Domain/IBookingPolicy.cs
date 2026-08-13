using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public interface IBookingPolicy
    {
        void CheckEventAvailability(DateTime eventDateUtc);

    
        void CheckActiveBookingLimit(IReadOnlyCollection<Booking> activeBookings, int maxLimit = 5);

      
        void CheckAccessRights(Guid currentUserId, bool isCurrentUserAdmin, Guid targetBookingOwnerId);
    }
}
