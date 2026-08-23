using System.Text.Json;
using System.Text.Json.Serialization;
using KoiFengShuiSystem.Modules.Community.Application.Responses;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace UnitTests.Community
{
    /// <summary>
    /// Pins the wire contract of the ported new-users-list endpoint: the legacy
    /// controller serialized the raw Identity Account entity, so the module read
    /// model must produce an identical JSON property sequence (names, order,
    /// values, null-omission) under the host's serializer options.
    /// </summary>
    public class DashboardResponseShapeTests
    {
        // Mirrors the JsonOptions registered in both Program.cs hosts.
        private static readonly JsonSerializerOptions HostOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            MaxDepth = 32,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly DateTime CreatedAt = new(2026, 8, 1, 9, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime UpdatedAt = new(2026, 8, 2, 18, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void RecentAccountSummary_SerializesWithIdenticalPropertySequenceAsLegacyAccountEntity()
        {
            var legacy = new Account
            {
                AccountId = 42,
                FullName = "Ada Lovelace",
                Email = "ada@test.local",
                Password = "hash",
                Dob = new DateTime(1990, 12, 10),
                Phone = "0123456789",
                Gender = "female",
                ElementId = 3,
                RoleId = 2,
                ResetTokenHash = "token-hash",
                ResetTokenExpiresAt = UpdatedAt.AddDays(1),
                CreateAt = CreatedAt,
                UpdateAt = UpdatedAt
            };
            var module = new RecentAccountSummary(
                42,
                "Ada Lovelace",
                "ada@test.local",
                "hash",
                new DateTime(1990, 12, 10),
                "0123456789",
                "female",
                3,
                2,
                "token-hash",
                UpdatedAt.AddDays(1),
                CreatedAt,
                UpdatedAt);

            using var legacyDocument = JsonDocument.Parse(JsonSerializer.Serialize(legacy, HostOptions));
            using var moduleDocument = JsonDocument.Parse(JsonSerializer.Serialize(module, HostOptions));

            Assert.Equal(
                legacyDocument.RootElement.EnumerateObject().Select(property => property.Name),
                moduleDocument.RootElement.EnumerateObject().Select(property => property.Name));

            foreach (var (legacyProperty, moduleProperty) in legacyDocument.RootElement.EnumerateObject()
                         .Zip(moduleDocument.RootElement.EnumerateObject()))
            {
                Assert.Equal(legacyProperty.Value.GetRawText(), moduleProperty.Value.GetRawText());
            }
        }

        [Fact]
        public void RecentAccountSummary_NullableFields_AreOmittedExactlyLikeTheLegacyEntity()
        {
            var legacy = new Account
            {
                AccountId = 7,
                FullName = "Null Fields",
                Email = "nulls@test.local",
                CreateAt = CreatedAt,
                UpdateAt = UpdatedAt
            };
            var module = new RecentAccountSummary(
                7,
                "Null Fields",
                "nulls@test.local",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                CreatedAt,
                UpdatedAt);

            using var legacyDocument = JsonDocument.Parse(JsonSerializer.Serialize(legacy, HostOptions));
            using var moduleDocument = JsonDocument.Parse(JsonSerializer.Serialize(module, HostOptions));

            Assert.Equal(
                legacyDocument.RootElement.EnumerateObject().Select(property => property.Name).ToList(),
                moduleDocument.RootElement.EnumerateObject().Select(property => property.Name).ToList());
            Assert.DoesNotContain("Role", moduleDocument.RootElement.EnumerateObject().Select(property => property.Name));
        }
    }
}
