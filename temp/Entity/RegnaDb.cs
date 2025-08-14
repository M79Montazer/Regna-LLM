using Microsoft.EntityFrameworkCore;

namespace temp.Entity
{
    public class RegnaDb : DbContext
    {
        public DbSet<DataCard> DataCards { get; set; }
        public DbSet<Embedding> Embeddings { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=game.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DataCard>()
                .HasOne(c => c.Embedding)
                .WithOne(e => e.Card)
                .HasForeignKey<Embedding>(e => e.CardId);

            modelBuilder.Entity<Embedding>().HasKey(e => e.CardId);
            // Optional: configure other fields like string lengths or indexing here
        }
    }
}
