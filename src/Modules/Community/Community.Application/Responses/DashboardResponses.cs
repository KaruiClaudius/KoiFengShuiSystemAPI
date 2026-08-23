namespace KoiFengShuiSystem.Modules.Community.Application.Responses
{
    /// <summary>
    /// Read model backing GET api/Dashboard/new-users-list.
    ///
    /// Safe admin-profile projection of an Identity Account. Credential material
    /// (Password hash, reset-token hash/expiry) is deliberately excluded: secrets
    /// must never transit an API response, even an admin-only one. Remaining
    /// members keep the legacy Account property names and declaration order so
    /// non-sensitive consumers see the familiar surface minus the dropped fields;
    /// the shape is pinned by DashboardResponseShapeTests.
    /// </summary>
    public sealed record RecentAccountSummary(
        int AccountId,
        string FullName,
        string Email,
        DateTime? Dob,
        string? Phone,
        string? Gender,
        int? ElementId,
        int? RoleId,
        DateTime CreateAt,
        DateTime UpdateAt);

    /// <summary>Posts-per-category distribution entry for the content summary.</summary>
    /// <param name="CategoryId">PostCategories.Id the posts were filed under.</param>
    /// <param name="CategoryName">Display name of the category (PostCategories.PostType).</param>
    /// <param name="Count">Number of posts in the category; categories without posts are absent.</param>
    public sealed record CategoryPostCount(int CategoryId, string CategoryName, int Count);

    /// <summary>
    /// Content-aware dashboard report: overall post volume, its distribution across
    /// categories, and the size of the member-submission pending queue.
    /// </summary>
    public sealed record ContentSummaryResponse(
        int TotalPosts,
        IReadOnlyList<CategoryPostCount> ByCategory,
        int PendingCount);
}
