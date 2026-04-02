using Microsoft.EntityFrameworkCore;
using ItemProcessingApp.Models;

namespace ItemProcessingApp.Data
{
    /// <summary>
    /// The main Entity Framework Core database context.
    /// Configures the Items table and its self-referencing relationship.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ── DbSets ───────────────────────────────────────────
        public DbSet<Item> Items { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Item>(entity =>
            {
                // Self-referencing one-to-many: a parent has many children.
                // DeleteBehavior.Restrict prevents cascading deletes
                // (you must remove children before deleting a parent).
                entity.HasOne(i => i.Parent)
                      .WithMany(i => i.Children)
                      .HasForeignKey(i => i.ParentId)
                      .OnDelete(DeleteBehavior.Restrict)
                      .IsRequired(false);

                // Index on ParentId for fast hierarchy queries
                entity.HasIndex(i => i.ParentId);

                // Table name
                entity.ToTable("Items");
            });
        }
    }
}
