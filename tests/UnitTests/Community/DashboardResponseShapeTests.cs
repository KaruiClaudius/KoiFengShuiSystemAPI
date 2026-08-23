using System.Text.Json;
using System.Text.Json.Serialization;
using KoiFengShuiSystem.Modules.Community.Application.Responses;

namespace UnitTests.Community
{
    /// <summary>
    /// Pins the wire contract of the ported new-users-list endpoint as a SAFE
    /// admin-profile projection: only non-sensitive Account fields cross the API,
    /// keeping the legacy property names and declaration order minus every
    /// credential-bearing member (Password hash, reset-token hash and expiry).
    /// The regression below proves absence of credential material in the actual
    /// serialized response body, not merely in the type declaration.
    /// </summary>
    public class DashboardResponseShapeTests
    {
        // Mirrors the JsonOptions registered in both Program.cs hosts, including
        // MVC's default camelCase naming policy (the hosts override only reference
        // handling, depth and null omission), so these assertions run against the
        // exact JSON shape the endpoint puts on the wire.
        private static readonly JsonSerializerOptions HostOptions = new()
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            MaxDepth = 32,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private static readonly DateTime CreatedAt = new(2026, 8, 1, 9, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime UpdatedAt = new(2026, 8, 2, 18, 0, 0, DateTimeKind.Utc);

        // Safe profile surface: legacy Account names/order with the credential
        // members (Password, ResetTokenHash, ResetTokenExpiresAt) removed.
        private static readonly string[] SafePropertySequence =
        {
            "accountId",
            "fullName",
            "email",
            "dob",
            "phone",
            "gender",
            "elementId",
            "roleId",
            "createAt",
            "updateAt"
        };

        [Fact]
        public void RecentAccountSummary_SerializesSafeProfileFieldsInLegacyOrder()
        {
            var module = new RecentAccountSummary(
                42,
                "Ada Lovelace",
                "ada@test.local",
                new DateTime(1990, 12, 10),
                "0123456789",
                "female",
                3,
                2,
                CreatedAt,
                UpdatedAt);

            using var document = JsonDocument.Parse(JsonSerializer.Serialize(module, HostOptions));

            Assert.Equal(
                SafePropertySequence,
                document.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
        }

        [Fact]
        public void RecentAccountSummary_NullableFields_AreOmittedWhenNullLikeTheLegacyEntity()
        {
            var module = new RecentAccountSummary(
                7,
                "Null Fields",
                "nulls@test.local",
                null,
                null,
                null,
                null,
                2,
                CreatedAt,
                UpdatedAt);

            using var document = JsonDocument.Parse(JsonSerializer.Serialize(module, HostOptions));

            Assert.Equal(
                new[] { "accountId", "fullName", "email", "roleId", "createAt", "updateAt" },
                document.RootElement.EnumerateObject().Select(property => property.Name).ToArray());
        }

        // Regression guard for the credential-leak fix: even a fully populated
        // summary must never carry password or reset-token material on the wire.
        [Fact]
        public void RecentAccountSummary_SerializedJson_NeverContainsCredentialMaterial()
        {
            var module = new RecentAccountSummary(
                42,
                "Ada Lovelace",
                "ada@test.local",
                new DateTime(1990, 12, 10),
                "0123456789",
                "female",
                3,
                2,
                CreatedAt,
                UpdatedAt);
            var nulledModule = module with { Dob = null, Phone = null, Gender = null, ElementId = null, RoleId = null };

            var populatedJson = JsonSerializer.Serialize(module, HostOptions);
            var nulledJson = JsonSerializer.Serialize(nulledModule, HostOptions);

            Assert.DoesNotContain("password", populatedJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("resettoken", populatedJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("password", nulledJson, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("resettoken", nulledJson, StringComparison.OrdinalIgnoreCase);
        }
    }
}
