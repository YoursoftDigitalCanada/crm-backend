using crm_server.Entity;
using Microsoft.EntityFrameworkCore;

namespace crm_server.Data
{
    public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
    }
}
