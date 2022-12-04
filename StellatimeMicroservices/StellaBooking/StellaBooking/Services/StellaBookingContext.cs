using Microsoft.EntityFrameworkCore;
using StellaBooking.Models;

namespace StellaBooking.Services
{
    public class StellaBookingContext: DbContext
    {
        public StellaBookingContext(DbContextOptions<StellaBookingContext> options) :base (options){ }

        public DbSet<Booking> Bookings => Set<Booking>();
    }
}
