using crm_server.Entity;
using Microsoft.EntityFrameworkCore;

namespace crm_server.Data
{
    public class UserDbContext(DbContextOptions<UserDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }

        // override global query filter for usertable
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Global query filter to exclude soft-deleted users
            modelBuilder.Entity<User>().HasQueryFilter(user => !user.IsDeleted);
        }
    }
}
