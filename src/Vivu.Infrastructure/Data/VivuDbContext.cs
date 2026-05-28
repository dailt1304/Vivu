using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vivu.Domain.Entities;

namespace Vivu.Infrastructure.Data
{
    public class VivuDbContext : DbContext
    {
        public VivuDbContext(DbContextOptions<VivuDbContext> options) : base(options)
        {
        }

        // P1: Auth & Users
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UserSettings> UserSettings { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Otp> Otps { get; set; }

        // P2: Trip Planning & History
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripMember> TripMembers { get; set; }
        public DbSet<TripRating> TripRatings { get; set; }
        public DbSet<TripFavorite> TripFavorites { get; set; }
        public DbSet<TripDay> TripDays { get; set; }
        public DbSet<TripLocation> TripLocations { get; set; }
        public DbSet<TripLocationAlternative> TripLocationAlternatives { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        // P3: Locations
        public DbSet<Country> Countries { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<LocationCategory> LocationCategories { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<LocationDetail> LocationDetails { get; set; }
        public DbSet<LocationReport> LocationReports { get; set; }

        // P4: Collections
        public DbSet<Collection> Collections { get; set; }
        public DbSet<CollectionLocation> CollectionLocations { get; set; }

        // P5 & P6: Groups & Chat
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<FileAttachment> FileAttachments { get; set; }

        // P7: Blogs
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<BlogStoryDay> BlogStoryDays { get; set; }
        public DbSet<BlogImage> BlogImages { get; set; }
        public DbSet<BlogTag> BlogTags { get; set; }
        public DbSet<BlogPostTag> BlogPostTags { get; set; }
        public DbSet<BlogSave> BlogSaves { get; set; }
        public DbSet<BlogView> BlogViews { get; set; }
        public DbSet<BlogComment> BlogComments { get; set; }
        public DbSet<BlogCommentLike> BlogCommentLikes { get; set; }
        public DbSet<BlogReport> BlogReports { get; set; }
        public DbSet<BlogLike> BlogLikes { get; set; }

        // P8 & P14: Revenue & Billing
        public DbSet<SubscriptionPackage> SubscriptionPackages { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<ApiUsageLog> ApiUsageLogs { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        // P11: Notifications
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(VivuDbContext).Assembly);
        }
    }
}
