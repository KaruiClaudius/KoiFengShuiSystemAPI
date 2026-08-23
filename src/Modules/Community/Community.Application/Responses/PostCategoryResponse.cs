namespace KoiFengShuiSystem.Modules.Community.Application.Responses
{
    /// <summary>
    /// Category row for the public GET /api/Post/categories surface (council D10).
    /// Serialized as { categoryId, categoryName } via camelCase policy.
    /// </summary>
    public sealed record PostCategoryResponse(int CategoryId, string CategoryName);
}
