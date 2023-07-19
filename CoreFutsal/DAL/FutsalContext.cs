using CoreFutsal.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CoreFutsal.DAL
{
    public class FutsalContext : DbContext
    {
        public FutsalContext(DbContextOptions<FutsalContext> options) : base(options)
        {
            
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<TeamPlayer> TeamPlayers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<TeamPlayer>().HasKey("TeamId", "PlayerId");
        }
    }
}
