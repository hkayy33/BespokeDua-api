using BespokeDuaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BespokeDuaApi.Data
{
    public class BespokeDuaDbContext : DbContext
    {
        public BespokeDuaDbContext(DbContextOptions<BespokeDuaDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<SavedDua> SavedDuas { get; set; } = null!;
        public DbSet<SavedSunnahDua> SavedSunnahDuas { get; set; } = null!;
        public DbSet<UserUsage> UserUsages { get; set; } = null!;
        public DbSet<SubscriptionOwnership> SubscriptionOwnerships { get; set; } = null!;
        public DbSet<FeelingLabel> FeelingLabels { get; set; } = null!;
        public DbSet<AllahName> AllahNames { get; set; } = null!;
        public DbSet<DuaCollection> DuaCollections { get; set; } = null!;
        public DbSet<DuaCollectionItem> DuaCollectionItems { get; set; } = null!;
        public DbSet<DuaFeedPost> DuaFeedPosts { get; set; } = null!;
        public DbSet<DuaFeedLike> DuaFeedLikes { get; set; } = null!;
        public DbSet<DevicePushToken> DevicePushTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.Property(e => e.Username)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Email)
                      .IsRequired()
                      .HasMaxLength(255);

                entity.Property(e => e.AuthUserId);

                entity.Property(e => e.HashedPassword);

                entity.Property(e => e.Plan)
                      .IsRequired()
                      .HasConversion<string>();

                entity.Property(e => e.CreatedAt)
                      .IsRequired();

                entity.HasIndex(e => e.Username)
                      .IsUnique();

                entity.HasIndex(e => e.Email)
                      .IsUnique();

                entity.HasIndex(e => e.AuthUserId)
                      .IsUnique();

                entity.HasMany(e => e.SavedDuas)
                      .WithOne(d => d.User)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.SavedSunnahDuas)
                      .WithOne(d => d.User)
                      .HasForeignKey(d => d.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.DuaCollections)
                      .WithOne(c => c.User)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SavedDua>(entity =>
            {
                entity.HasKey(e => e.DuaId);

                entity.Property(e => e.DuaId)
                      .ValueGeneratedNever();

                entity.Property(e => e.Dua)
                      .IsRequired();

                entity.Property(e => e.CreatedAt)
                      .IsRequired();
            });

            modelBuilder.Entity<SavedSunnahDua>(entity =>
            {
                entity.HasKey(e => e.SunnahDuaId);

                entity.Property(e => e.SunnahDuaId)
                      .ValueGeneratedNever();

                entity.Property(e => e.SunnahDua)
                      .IsRequired();

                entity.Property(e => e.CreatedAt)
                      .IsRequired();

                entity.HasIndex(e => e.UserId);
            });

            modelBuilder.Entity<UserUsage>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Date)
                      .IsRequired()
                      .HasColumnType("date");

                entity.Property(e => e.RequestsCount)
                      .IsRequired();

                entity.HasIndex(e => new { e.UserId, e.Date })
                      .IsUnique();
            });

            modelBuilder.Entity<SubscriptionOwnership>(entity =>
            {
                entity.ToTable("SubscriptionOwnerships");
                entity.HasKey(e => e.SubscriptionOwnershipId);

                entity.Property(e => e.SubscriptionOwnershipId).ValueGeneratedOnAdd();

                entity.Property(e => e.OriginalTransactionId).IsRequired().HasMaxLength(128);
                entity.Property(e => e.ProductId).IsRequired().HasMaxLength(256);
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasIndex(e => e.OriginalTransactionId).IsUnique();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FeelingLabel>(entity =>
            {
                entity.HasKey(e => e.FeelingLabelId);

                entity.Property(e => e.FeelingLabelId)
                      .ValueGeneratedNever();

                entity.Property(e => e.Label)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.DisplayOrder)
                      .IsRequired();

                entity.HasIndex(e => e.Label)
                      .IsUnique();

                entity.HasMany(e => e.Names)
                      .WithOne(n => n.FeelingLabel)
                      .HasForeignKey(n => n.FeelingLabelId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AllahName>(entity =>
            {
                entity.HasKey(e => e.Number);

                entity.Property(e => e.Number)
                      .ValueGeneratedNever();

                entity.Property(e => e.Arabic)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Transliteration)
                      .IsRequired()
                      .HasMaxLength(100);

                entity.Property(e => e.Translation)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Meaning)
                      .IsRequired();

                entity.Property(e => e.SortOrder)
                      .IsRequired();

                entity.HasIndex(e => new { e.FeelingLabelId, e.SortOrder });
            });

            modelBuilder.Entity<DuaCollection>(entity =>
            {
                entity.HasKey(e => e.CollectionId);

                entity.Property(e => e.CollectionId)
                      .ValueGeneratedNever();

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(200);

                entity.Property(e => e.Description)
                      .HasMaxLength(2000);

                entity.Property(e => e.CreatedAt)
                      .IsRequired();

                entity.Property(e => e.UpdatedAt)
                      .IsRequired();

                entity.HasIndex(e => e.UserId);
            });

            modelBuilder.Entity<DuaCollectionItem>(entity =>
            {
                entity.HasKey(e => e.ItemId);

                entity.Property(e => e.ItemId)
                      .ValueGeneratedNever();

                entity.Property(e => e.SortOrder)
                      .IsRequired();

                entity.HasOne(e => e.Collection)
                      .WithMany(c => c.Items)
                      .HasForeignKey(e => e.CollectionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SavedDua)
                      .WithMany(d => d.CollectionItems)
                      .HasForeignKey(e => e.DuaId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SavedSunnahDua)
                      .WithMany(d => d.CollectionItems)
                      .HasForeignKey(e => e.SunnahDuaId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.DuaId);
                entity.HasIndex(e => e.SunnahDuaId);
                entity.HasIndex(e => new { e.CollectionId, e.DuaId })
                      .IsUnique()
                      .HasFilter("\"DuaId\" IS NOT NULL");
                entity.HasIndex(e => new { e.CollectionId, e.SunnahDuaId })
                      .IsUnique()
                      .HasFilter("\"SunnahDuaId\" IS NOT NULL");
            });

            modelBuilder.Entity<DuaFeedPost>(entity =>
            {
                entity.HasKey(e => e.PostId);

                entity.Property(e => e.PostId)
                      .ValueGeneratedNever();

                entity.Property(e => e.Content)
                      .IsRequired();

                entity.Property(e => e.CreatedAt)
                      .IsRequired();

                entity.Property(e => e.ExpiresAt)
                      .IsRequired();

                entity.HasOne(e => e.User)
                      .WithMany(u => u.DuaFeedPosts)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.SavedDua)
                      .WithMany()
                      .HasForeignKey(e => e.SavedDuaId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.SavedSunnahDua)
                      .WithMany()
                      .HasForeignKey(e => e.SavedSunnahDuaId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => new { e.UserId, e.ExpiresAt });
            });

            modelBuilder.Entity<DuaFeedLike>(entity =>
            {
                entity.HasKey(e => e.LikeId);

                entity.Property(e => e.LikeId)
                      .ValueGeneratedNever();

                entity.Property(e => e.CreatedAt)
                      .IsRequired();

                entity.HasOne(e => e.Post)
                      .WithMany(p => p.Likes)
                      .HasForeignKey(e => e.PostId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.DuaFeedLikes)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.PostId);
                entity.HasIndex(e => new { e.PostId, e.UserId })
                      .IsUnique();
            });

            modelBuilder.Entity<DevicePushToken>(entity =>
            {
                entity.HasKey(e => e.DevicePushTokenId);

                entity.Property(e => e.DevicePushTokenId)
                      .ValueGeneratedNever();

                entity.Property(e => e.Token)
                      .IsRequired()
                      .HasMaxLength(256);

                entity.Property(e => e.UpdatedAt)
                      .IsRequired();

                entity.HasOne(e => e.User)
                      .WithMany(u => u.DevicePushTokens)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Token)
                      .IsUnique();

                entity.HasIndex(e => e.UserId);
            });
        }
    }
}
