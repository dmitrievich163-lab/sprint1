using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class BookingPolicy: IBookingPolicy
    {
        private const int DefaultMaxActiveBookings = 5;

        public void CheckEventAvailability(DateTime eventDateUtc)
        {
            if (eventDateUtc <= DateTime.UtcNow)
            {
                throw new PastEventBookingException();
            }
        }

        public void CheckActiveBookingLimit(IReadOnlyCollection<Booking> activeBookings, int maxLimit = 5)
        {
            // Используем дефолтное значение, если аргумент не передан
            var limit = maxLimit > 0 ? maxLimit : DefaultMaxActiveBookings;

            if (activeBookings.Count >= limit)
            {
                throw new ActiveBookingsLimitExceededException(limit);
            }
        }

        public void CheckAccessRights(Guid currentUserId, bool isCurrentUserAdmin, Guid targetBookingOwnerId)
        {
            // Если это не админ и ID не совпадают — доступ запрещен
            if (!isCurrentUserAdmin && currentUserId != targetBookingOwnerId)
            {
                throw new ForbiddenOperationException("изменение или просмотр чужой брони");
            }
        }
    }
}
