using Microsoft.EntityFrameworkCore;
using StellaAuth.Models;

namespace StellaAuth.Services
{
    public class StellaAuthContext: DbContext
    {
        public StellaAuthContext(DbContextOptions<StellaAuthContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
    }
}
