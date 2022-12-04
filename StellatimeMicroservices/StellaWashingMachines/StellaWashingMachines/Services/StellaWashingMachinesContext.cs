using Microsoft.EntityFrameworkCore;
using StellaWashingMachines.Models;

namespace StellaWashingMachines.Services
{
    public class StellaWashingMachinesContext: DbContext
    {
        public StellaWashingMachinesContext(DbContextOptions<StellaWashingMachinesContext> options) : base(options) { }

        public DbSet<WashingMachine> WashingMachines => Set<WashingMachine>();
    }
}
