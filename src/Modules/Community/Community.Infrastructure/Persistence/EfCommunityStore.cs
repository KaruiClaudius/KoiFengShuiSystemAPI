using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Modules.Community.Application.Responses;
using KoiFengShuiSystem.Modules.Community.Application.Services;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.Modules.Community.Infrastructure.Persistence
{
    public class EfCommunityStore : ICommunityStore
    {
        private readonly KoiFengShuiContext _context;

        public EfCommunityStore(KoiFengShuiContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _context = context;
        }

        // ---- FAQs ----

        public async Task<IReadOnlyList<FAQ>> GetAllAsync() =>
            await _context.FAQs
                .AsNoTracking()
                .ToListAsync();

        public async Task<FAQ?> GetByIdAsync(int id) =>
            await _context.FAQs.AsNoTracking().FirstOrDefaultAsync(f => f.FAQId == id);

        public async Task<FAQ> AddAsync(FAQ faq)
        {
            await _context.FAQs.AddAsync(faq);
            await _context.SaveChangesAsync();
            return faq;
        }

        // Full-replace semantics for detached instances: GetByIdAsync reads with AsNoTracking, so the
        // service mutates an entity that is not tracked by this context. Update() re-attaches it and
        // marks every property modified, which preserves legacy FAQ behavior because the service only
        // rewrites Question/Answer/CreateAt on the instance it just fetched (AccountId keeps its
        // database value).
        public async Task UpdateAsync(FAQ faq)
        {
            _context.FAQs.Update(faq);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var affected = await _context.FAQs
                .Where(f => f.FAQId == id)
                .ExecuteDeleteAsync();
            return affected > 0;
        }

        // ---- Posts: public reads ----

        // No ordering and no includes: the legacy GetAllWithElementAsync was a plain
        // ToListAsync, so element names were resolved separately by the service.
        public async Task<IReadOnlyList<Post>> GetAllPostsAsync() =>
            await _context.Posts
                .AsNoTracking()
                .ToListAsync();

        // Skip/Take without OrderBy replicates the legacy pagination query shape,
        // keeping row placement identical for existing callers.
        public async Task<IReadOnlyList<Post>> GetPostsByPostTypeAsync(int postTypeId, int page, int pageSize) =>
            await _context.Posts
                .AsNoTracking()
                .Where(p => p.PostCategoryId == postTypeId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        public async Task<IReadOnlyDictionary<int, string>> GetElementNamesAsync() =>
            await _context.Elements
                .AsNoTracking()
                .ToDictionaryAsync(e => e.ElementId, e => e.ElementName);

        public async Task<Post?> GetPostByIdAsync(int id) =>
            await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.PostId == id);

        // ---- Posts: mutations and validation ----

        public async Task<bool> PostCategoryExistsAsync(int categoryId) =>
            await _context.PostCategories.AnyAsync(c => c.Id == categoryId);

        public async Task<IReadOnlyList<Image>> GetImagesByIdsAsync(IReadOnlyCollection<int> imageIds) =>
            await _context.Images
                .AsNoTracking()
                .Where(i => imageIds.Contains(i.ImageId))
                .ToListAsync();

        public async Task AddPostAsync(Post post)
        {
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
        }

        // Tracked remove mirrors the legacy repository pair (GetById + RemoveAsync):
        // false means no such post, true means the delete saved successfully.
        public async Task<bool> DeletePostAsync(int id)
        {
            var post = await _context.Posts
                .FirstOrDefaultAsync(p => p.PostId == id);
            if (post == null)
            {
                return false;
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            return true;
        }

        // ---- Admin posts ----

        public async Task<IReadOnlyList<Post>> GetAllAdminPostsWithImagesAsync() =>
            await _context.Posts
                .AsNoTracking()
                .Include(p => p.PostImages)
                    .ThenInclude(pi => pi.Image)
                .ToListAsync();

        public async Task<Post?> GetAdminPostByIdWithImagesAsync(int id) =>
            await _context.Posts
                .AsNoTracking()
                .Include(p => p.PostImages)
                    .ThenInclude(pi => pi.Image)
                .FirstOrDefaultAsync(p => p.PostId == id);

        public async Task AddAdminPostWithImagesAsync(Post post)
        {
            // Track the post explicitly so creation persists even with zero images;
            // PostImage/Image navigations still propagate generated keys on this save.
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeletePostWithAllRelatedAsync(int postId)
        {
            var post = await _context.Posts
                .Include(p => p.PostImages)
                    .ThenInclude(pi => pi.Image)
                .FirstOrDefaultAsync(p => p.PostId == postId);

            if (post == null)
            {
                return false;
            }

            var imageIds = post.PostImages.Select(pi => pi.ImageId).ToList();
            _context.PostImages.RemoveRange(post.PostImages);
            _context.Posts.Remove(post);
            var images = await _context.Images.Where(i => imageIds.Contains(i.ImageId)).ToListAsync();
            _context.Images.RemoveRange(images);
            await _context.SaveChangesAsync();
            return true;
        }

        // Field rewrites plus url-diffed image replacement in ONE batched save:
        // removed urls drop both PostImage link and Image rows, new urls become new
        // PostImage/Image pairs, unchanged urls are left untouched.
        public async Task<Post?> UpdateAdminPostAsync(AdminPostUpdate update)
        {
            var post = await _context.Posts
                .Include(p => p.PostImages)
                    .ThenInclude(pi => pi.Image)
                .FirstOrDefaultAsync(p => p.PostId == update.PostId);

            if (post == null)
            {
                return null;
            }

            post.Name = update.Name;
            post.Description = update.Description;
            post.Status = update.Status;
            post.UpdateAt = DateTime.Now;

            var uploadedUrls = update.ImageUrls ?? new List<string>();
            var existingImageUrls = post.PostImages.Select(pi => pi.Image.ImageUrl).ToList();
            var imagesToRemove = post.PostImages.Where(pi => !uploadedUrls.Contains(pi.Image.ImageUrl)).ToList();

            foreach (var postImage in imagesToRemove)
            {
                _context.PostImages.Remove(postImage);
                _context.Images.Remove(postImage.Image);
            }

            var newImageUrls = uploadedUrls.Except(existingImageUrls).ToList();
            foreach (var imageUrl in newImageUrls)
            {
                var postImage = new PostImage
                {
                    PostId = post.PostId,
                    Image = new Image { ImageUrl = imageUrl },
                    ImageDescription = "Default description"
                };
                _context.PostImages.Add(postImage);
            }

            await _context.SaveChangesAsync();

            return await GetAdminPostByIdWithImagesAsync(post.PostId);
        }

        public async Task<bool> SavePostChangesAsync()
        {
            var affected = await _context.SaveChangesAsync();
            return affected > 0;
        }

        // ---- Dashboard metrics ----

        // Projects straight into the module read model (same JSON surface as the
        // legacy raw Account serialization) instead of leaking Identity entities
        // or IQueryable past the module boundary.
        public async Task<IReadOnlyList<RecentAccountSummary>> GetAccountsCreatedSinceAsync(DateTime createdAfterUtc) =>
            await _context.Accounts
                .AsNoTracking()
                .Where(a => a.CreateAt >= createdAfterUtc)
                .OrderByDescending(a => a.CreateAt)
                .Select(a => new RecentAccountSummary(
                    a.AccountId,
                    a.FullName,
                    a.Email,
                    a.Password,
                    a.Dob,
                    a.Phone,
                    a.Gender,
                    a.ElementId,
                    a.RoleId,
                    a.ResetTokenHash,
                    a.ResetTokenExpiresAt,
                    a.CreateAt,
                    a.UpdateAt))
                .ToListAsync();

        public Task<int> CountDistinctRegisteredTrafficSinceAsync(DateTime timestampAfterUtc) =>
            _context.TrafficLogs
                .AsNoTracking()
                .Where(log => log.IsRegistered && log.Timestamp >= timestampAfterUtc)
                .Select(log => log.AccountId)
                .Distinct()
                .CountAsync();

        public Task<int> CountDistinctGuestTrafficSinceAsync(DateTime timestampAfterUtc) =>
            _context.TrafficLogs
                .AsNoTracking()
                .Where(log => !log.IsRegistered && log.Timestamp >= timestampAfterUtc)
                .Select(log => log.IpAddress)
                .Distinct()
                .CountAsync();

        public Task<int> CountPostsAsync() =>
            _context.Posts
                .AsNoTracking()
                .CountAsync();

        // Single grouped query joined through the required PostCategory navigation.
        // Categories without posts never appear (posts-side grouping). The stable
        // category-id ordering happens after materialization: the InMemory test
        // provider cannot translate ordering over grouped results, and sorting the
        // tiny aggregate list client-side keeps one portable query shape.
        public async Task<IReadOnlyList<CategoryPostCount>> CountPostsByCategoryAsync()
        {
            var counts = await _context.Posts
                .AsNoTracking()
                .GroupBy(p => new { p.PostCategoryId, CategoryName = p.PostCategory.PostType })
                .Select(g => new CategoryPostCount(g.Key.PostCategoryId, g.Key.CategoryName, g.Count()))
                .ToListAsync();

            return counts.OrderBy(c => c.CategoryId).ToList();
        }

        public Task<int> CountPendingPostsAsync() =>
            _context.Posts
                .AsNoTracking()
                .CountAsync(p => p.Status == PostService.MemberPostDefaultStatus);
    }
}
