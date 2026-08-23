namespace KoiFengShuiSystem.Modules.Community.Application.Responses
{
    /// <summary>
    /// Read model backing GET api/Dashboard/new-users-list.
    ///
    /// The legacy endpoint serialized the raw Identity Account entity, so this record
    /// mirrors that JSON surface property-for-property (names, declaration order,
    /// nullability, types) to keep response bodies byte-compatible for existing
    /// admin consumers. Do not reorder or rename members; the shape is pinned by
    /// RecentAccountSummaryShapeTests.
    /// </summary>
    public sealed record RecentAccountSummary(
        int AccountId,
        string FullName,
        string Email,
        string? Password,
        DateTime? Dob,
        string? Phone,
        string? Gender,
        int? ElementId,
        int? RoleId,
        string? ResetTokenHash,
        DateTime? ResetTokenExpiresAt,
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
