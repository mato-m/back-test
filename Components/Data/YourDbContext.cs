using Microsoft.EntityFrameworkCore;
public class YourDbContext : DbContext
{
    public YourDbContext(DbContextOptions<YourDbContext> options)
        : base(options)
    {
    }

    public DbSet<IpAddressRecord> IpAddressRecords { get; set; } 
    public DbSet<Citat> Citati { get; set; } 
        public DbSet<WhitelistedIP> WhitelistedIPs { get; set; } 

}