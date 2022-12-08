namespace StellaBooking.Models
{
    public class Booking
    {
        public Guid Id { get; set; }
        public Guid WashingMachineId { get; set; }
        public Guid UserId { get; set; }
        public string UserEmail { get; set; }
        public string ApartmentComplexName { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
