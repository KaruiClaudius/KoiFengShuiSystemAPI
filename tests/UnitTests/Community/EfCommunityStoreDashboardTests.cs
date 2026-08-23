using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Community.Infrastructure.Persistence;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace UnitTests.Community
{
    /// <summary>
    /// Metric correctness for the dashboard store reads: window boundaries,
    /// traffic de-duplication, category distribution and the pending queue count.
    /// All fixtures use fixed UTC timestamps so cutoffs stay deterministic.
    /// </summary>
    public class EfCommunityStoreDashboardTests : IDisposable
    {
        // Fixed "now": every seeded timestamp is expressed relative to it.
        private static readonly DateTime Now = new(2026, 8, 23, 12, 0, 0, DateTimeKind.Utc);

        private readonly KoiFengShuiContext _context;
        private readonly EfCommunityStore _store;

        public EfCommunityStoreDashboardTests()
        {
            var options = new DbContextOptionsBuilder<KoiFengShuiContext>()
                .UseInMemoryDatabase(databaseName: $"CommunityStoreDashboard_{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new KoiFengShuiContext(options);
            _store = new EfCommunityStore(_context);
        }

        public void Dispose() => _context.Dispose();

        // ---- Users growth ----

        [Fact]
        public async Task GetAccountsCreatedSinceAsync_IncludesAccountsOnOrAfterCutoff_ExcludesOlder()
        {
            var cutoff = Now.AddDays(-30);
            SeedAccounts(
                (1, Now.AddDays(-1)),   // well inside
                (2, cutoff),            // boundary: >= is inclusive like the legacy query
                (3, cutoff.AddTicks(-1)) // one tick before: excluded
            );

            var users = await _store.GetAccountsCreatedSinceAsync(cutoff);

            Assert.Equal(new[] { 1, 2 }, users.Select(u => u.AccountId).ToArray());
        }

        [Fact]
        public async Task GetAccountsCreatedSinceAsync_OrdersNewestFirst()
        {
            var cutoff = Now.AddDays(-10);
            SeedAccounts(
                (1, Now.AddDays(-5)),
                (2, Now.AddDays(-2)),
                (3, Now.AddDays(-8))
            );

            var users = await _store.GetAccountsCreatedSinceAsync(cutoff);

            Assert.Equal(new[] { 2, 1, 3 }, users.Select(u => u.AccountId).ToArray());
        }

        [Fact]
        public async Task GetAccountsCreatedSinceAsync_ProjectionsCarryAccountFields()
        {
            _context.Accounts.Add(new Account
            {
                AccountId = 9,
                FullName = "Ada Lovelace",
                Email = "ada@test.local",
                Password = "hash",
                Dob = new DateTime(1990, 12, 10),
                Phone = "0123456789",
                Gender = "female",
                ElementId = 3,
                RoleId = 2,
                ResetTokenHash = "token-hash",
                ResetTokenExpiresAt = Now.AddDays(1),
                CreateAt = Now.AddDays(-1),
                UpdateAt = Now.AddHours(-1)
            });
            _context.SaveChanges();

            var user = Assert.Single(await _store.GetAccountsCreatedSinceAsync(Now.AddDays(-2)));

            Assert.Equal("Ada Lovelace", user.FullName);
            Assert.Equal("ada@test.local", user.Email);
            Assert.Equal("hash", user.Password);
            Assert.Equal(new DateTime(1990, 12, 10), user.Dob);
            Assert.Equal("0123456789", user.Phone);
            Assert.Equal("female", user.Gender);
            Assert.Equal(3, user.ElementId);
            Assert.Equal(2, user.RoleId);
            Assert.Equal("token-hash", user.ResetTokenHash);
            Assert.Equal(Now.AddDays(1), user.ResetTokenExpiresAt);
            Assert.Equal(Now.AddDays(-1), user.CreateAt);
            Assert.Equal(Now.AddHours(-1), user.UpdateAt);
        }

        // ---- Traffic distribution ----

        [Fact]
        public async Task CountDistinctRegisteredTrafficSinceAsync_DeduplicatesAccountsAndIgnoresOldLogs()
        {
            var cutoff = Now.AddDays(-30);
            _context.TrafficLogs.AddRange(
                new TrafficLog { Timestamp = Now.AddDays(-1), IsRegistered = true, AccountId = 10 },
                new TrafficLog { Timestamp = Now.AddDays(-2), IsRegistered = true, AccountId = 10 }, // duplicate account
                new TrafficLog { Timestamp = Now.AddDays(-3), IsRegistered = true, AccountId = 11 },
                new TrafficLog { Timestamp = cutoff.AddDays(-5), IsRegistered = true, AccountId = 12 } // outside window
            );
            _context.SaveChanges();

            Assert.Equal(2, await _store.CountDistinctRegisteredTrafficSinceAsync(cutoff));
        }

        [Fact]
        public async Task CountDistinctGuestTrafficSinceAsync_DeduplicatesIpAddresses()
        {
            var cutoff = Now.AddDays(-30);
            _context.TrafficLogs.AddRange(
                new TrafficLog { Timestamp = Now.AddDays(-1), IsRegistered = false, IpAddress = "203.0.113.7" },
                new TrafficLog { Timestamp = Now.AddDays(-2), IsRegistered = false, IpAddress = "203.0.113.7" }, // duplicate ip
                new TrafficLog { Timestamp = Now.AddDays(-3), IsRegistered = false, IpAddress = "198.51.100.4" },
                new TrafficLog { Timestamp = Now.AddDays(-40), IsRegistered = false, IpAddress = "192.0.2.9" } // outside window
            );
            _context.SaveChanges();

            Assert.Equal(2, await _store.CountDistinctGuestTrafficSinceAsync(cutoff));
        }

        [Fact]
        public async Task TrafficCounters_EmptyTable_ReturnZero()
        {
            var cutoff = Now.AddDays(-30);

            Assert.Equal(0, await _store.CountDistinctRegisteredTrafficSinceAsync(cutoff));
            Assert.Equal(0, await _store.CountDistinctGuestTrafficSinceAsync(cutoff));
        }

        // ---- Content metrics ----

        [Fact]
        public async Task ContentMetrics_CountAllPostsByCategoryAndPendingQueue()
        {
            SeedCategories(
                (1, "Blog"),
                (2, "Koi Care"),
                (3, "Empty Category") // no posts: must not appear in byCategory
            );
            SeedPost(1, categoryId: 1, status: "Published");
            SeedPost(2, categoryId: 1, status: "Pending");
            SeedPost(3, categoryId: 2, status: "Pending");

            Assert.Equal(3, await _store.CountPostsAsync());

            var byCategory = await _store.CountPostsByCategoryAsync();
            Assert.Equal(2, byCategory.Count); // empty category excluded
            Assert.Equal(1, byCategory[0].CategoryId);
            Assert.Equal("Blog", byCategory[0].CategoryName);
            Assert.Equal(2, byCategory[0].Count);
            Assert.Equal(2, byCategory[1].CategoryId);
            Assert.Equal("Koi Care", byCategory[1].CategoryName);
            Assert.Equal(1, byCategory[1].Count);

            Assert.Equal(2, await _store.CountPendingPostsAsync());
        }

        [Fact]
        public async Task CountPendingPostsAsync_OnlyCountsExactMemberDefaultStatus()
        {
            SeedCategories((1, "Blog"));
            SeedPost(1, categoryId: 1, status: "pending"); // wrong casing: member default is "Pending"
            SeedPost(2, categoryId: 1, status: "Approved");

            Assert.Equal(0, await _store.CountPendingPostsAsync());
        }

        [Fact]
        public async Task ContentMetrics_NoPostsAtAll_ReturnZerosAndEmptyDistribution()
        {
            Assert.Equal(0, await _store.CountPostsAsync());
            Assert.Empty(await _store.CountPostsByCategoryAsync());
            Assert.Equal(0, await _store.CountPendingPostsAsync());
        }

        // ---- Helpers ----

        private void SeedAccounts(params (int id, DateTime createAt)[] accounts)
        {
            foreach (var (id, createAt) in accounts)
            {
                _context.Accounts.Add(new Account
                {
                    AccountId = id,
                    FullName = $"User {id}",
                    Email = $"user{id}@test.local",
                    RoleId = 2,
                    CreateAt = createAt,
                    UpdateAt = createAt
                });
            }
            _context.SaveChanges();
        }

        private void SeedCategories(params (int id, string name)[] categories)
        {
            foreach (var (id, name) in categories)
            {
                _context.PostCategories.Add(new PostCategory { Id = id, PostType = name });
            }
            _context.SaveChanges();
        }

        private void SeedPost(int id, int categoryId, string status)
        {
            _context.Posts.Add(new Post
            {
                PostId = id,
                PostCategoryId = categoryId,
                Name = $"Post {id}",
                Description = "Body",
                Status = status,
                AccountId = 1,
                CreateAt = Now,
                UpdateAt = Now
            });
            _context.SaveChanges();
        }
    }
}
