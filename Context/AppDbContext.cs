using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using KNQASelfService.Models;

namespace KNQASelfService.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserTypes> UserTypes { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<OtpVerification> OtpVerifications { get; set; }

    }
}
