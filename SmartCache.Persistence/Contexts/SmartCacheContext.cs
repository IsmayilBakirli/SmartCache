using Microsoft.EntityFrameworkCore;
using SmartCache.Domain.Entities;
using SmartCache.Domain.Entities.Common;

namespace SmartCache.Persistence.Contexts
{
    public class SmartCacheContext : DbContext
    {
        public SmartCacheContext(DbContextOptions<SmartCacheContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Story> Stories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Category konfiqurasiyası
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.IsActive)
                      .IsRequired();

                entity.Property(e => e.CreatedDate)
                      .IsRequired();

                entity.Property(e => e.UpdatedDate);
            });

            // Service konfiqurasiyası
            // Service konfiqurasiyası
            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Description)
                      .HasMaxLength(1000);

                entity.Property(e => e.IsActive)
                      .IsRequired();

                entity.Property(e => e.Price)
                      .IsRequired()
                      .HasPrecision(18, 2); // <-- Warning-in qarşısı alındı

                entity.Property(e => e.CreatedDate)
                      .IsRequired();

                entity.Property(e => e.UpdatedDate);
            });


            modelBuilder.Entity<Story>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Title)
                      .IsRequired()
                      .HasMaxLength(300);

                entity.Property(e => e.Content)
                      .IsRequired();

                entity.Property(e => e.ImageUrl)
                      .HasMaxLength(500);

                entity.Property(e => e.IsPublished)
                      .IsRequired();

                entity.Property(e => e.CreatedDate)
                      .IsRequired();

                entity.Property(e => e.UpdatedDate);
            });
        }
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                if (entityEntry.State == EntityState.Added && entityEntry.Entity is IHasCreatedDate createdEntity)
                {
                    createdEntity.CreatedDate = DateTime.UtcNow;
                }

                if (entityEntry.State == EntityState.Modified && entityEntry.Entity is IHasUpdatedDate updatedEntity)
                {
                    updatedEntity.UpdatedDate = DateTime.UtcNow;
                }
            }
        }


    }
}
