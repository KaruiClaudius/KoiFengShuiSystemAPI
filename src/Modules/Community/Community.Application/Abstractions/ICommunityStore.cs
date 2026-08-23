using KoiFengShuiSystem.DataAccess.Models;

namespace KoiFengShuiSystem.Modules.Community.Application.Abstractions
{
    /// <summary>Persisted inputs for <see cref="ICommunityStore.UpdateAdminPostAsync"/>.</summary>
    /// <param name="ImageUrls">
    /// Desired final image-url set uploaded through the cloud service; images whose stored
    /// url is absent from this list are deleted together with their Image row, new urls
    /// become PostImage/Image pairs. Mirrors the legacy AdminPostRequest contract.
    /// </param>
    public sealed record AdminPostUpdate(
        int PostId,
        string Name,
        string Description,
        string Status,
        IReadOnlyList<string> ImageUrls);

    public interface ICommunityStore
    {
        // ---- FAQs ----

        Task<IReadOnlyList<FAQ>> GetAllAsync();

        Task<FAQ?> GetByIdAsync(int id);

        Task<FAQ> AddAsync(FAQ faq);

        Task UpdateAsync(FAQ faq);

        Task<bool> DeleteAsync(int id);

        // ---- Posts: public reads ----

        // Full post list feeding the public endpoints. Deliberately no ordering or
        // includes: replicates the legacy GetAllWithElementAsync query shape.
        Task<IReadOnlyList<Post>> GetAllPostsAsync();

        // Category-filtered page using Skip/Take without ordering, matching the
        // legacy repository so row placement stays identical.
        Task<IReadOnlyList<Post>> GetPostsByPostTypeAsync(int postTypeId, int page, int pageSize);

        // Raw entity lookup backing Details/{id}; the controller serializes this
        // entity directly, so the shape (unloaded navigations excluded as null)
        // must not gain includes.
        Task<Post?> GetPostByIdAsync(int id);

        // Element-name join table used by the public feed mapping.
        Task<IReadOnlyDictionary<int, string>> GetElementNamesAsync();

        // ---- Posts: mutations and validation ----

        Task<bool> PostCategoryExistsAsync(int categoryId);

        Task<IReadOnlyList<Image>> GetImagesByIdsAsync(IReadOnlyCollection<int> imageIds);

        // Persists the post together with whatever PostImages the caller attached,
        // in a single save. Adding the post root explicitly keeps zero-image posts
        // persisted (restored-behavior fix).
        Task AddPostAsync(Post post);

        Task<bool> DeletePostAsync(int id);

        // ---- Admin posts ----

        Task<IReadOnlyList<Post>> GetAllAdminPostsWithImagesAsync();

        Task<Post?> GetAdminPostByIdWithImagesAsync(int id);

        Task AddAdminPostWithImagesAsync(Post post);

        // Removes the post together with its PostImage links and Image rows in a
        // single transaction. Returns false when the post does not exist.
        Task<bool> DeletePostWithAllRelatedAsync(int postId);

        // Applies field rewrites plus the url-diffed image replacement in one
        // batched save, then re-reads the stored post with images. Returns null
        // when the post does not exist.
        Task<Post?> UpdateAdminPostAsync(AdminPostUpdate update);

        // Backing for the rarely-used explicit save endpoint contract; reports
        // whether the pending save touched any rows.
        Task<bool> SavePostChangesAsync();
    }
}
