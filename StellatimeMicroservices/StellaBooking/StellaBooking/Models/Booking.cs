namespace StellaBooking.Models
{
    public class Booking
    {

        public Booking(int durationInMinutes) 
        {
            Start = DateTime.UtcNow;
            End = Start + TimeSpan.FromMinutes(durationInMinutes);
        }
        public Guid Id { get; set; }
        public string UserEmail { get; set; }
        public string ApartmentComplexName { get; set; }
        private DateTime Start { get; set; }
        private DateTime End { get; set; }
    }
}
