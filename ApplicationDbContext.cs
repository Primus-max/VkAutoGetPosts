//using Microsoft.EntityFrameworkCore;

//public class ApplicationDbContext : DbContext
//{
//    public DbSet<Content> Contents { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder options)
//        => options.UseSqlite("Data Source=content.db");

//    protected override void OnModelCreating(ModelBuilder modelBuilder)
//    {
//        modelBuilder.Entity<Content>()
//            .HasKey(c => c.MessageId);
//    }

//}
