using KoiFengShuiSystem.Shared.Kernel;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Modules.Community.Application.Responses;
using KoiFengShuiSystem.Shared.Kernel.Results;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Modules.Community.Application.Services
{
    public class PostService : IPostService
    {
        /// <summary>
        /// Server-assigned status for member-created posts; admins promote them
        /// through AdminPost management. Never accepted from the client.
        /// </summary>
        public const string MemberPostDefaultStatus = "Pending";

        private readonly ICommunityStore _store;
        private readonly ILogger<PostService> _logger;

        public PostService(ICommunityStore store, ILogger<PostService> logger)
        {
            _store = store;
            _logger = logger;
        }

        public async Task<IBusinessResult> GetAll()
        {
            try
            {
                var posts = await _store.GetAllPostsAsync();
                var elementDict = await _store.GetElementNamesAsync();
                var postResponses = posts.Select(po => MapToResponse(po, elementDict)).ToList();
                return new BusinessResult(ResponseCodes.SuccessReadCode, ResponseCodes.SuccessReadMessage, postResponses);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostService.GetAll failed");
                return new BusinessResult(ResponseCodes.ErrorException, "Failed to retrieve posts.");
            }
        }

        public async Task<IBusinessResult> GetPostByPostTypeId(int postTypeId, int pageNumber, int pageSize)
        {
            try
            {
                var posts = await _store.GetPostsByPostTypeAsync(postTypeId, pageNumber, pageSize);
                var elementDict = await _store.GetElementNamesAsync();
                var postResponses = posts.Select(po => MapToResponse(po, elementDict)).ToList();
                return new BusinessResult(ResponseCodes.SuccessReadCode, ResponseCodes.SuccessReadMessage, postResponses);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostService.GetPostByPostTypeId failed for postTypeId={PostTypeId}", postTypeId);
                return new BusinessResult(ResponseCodes.ErrorException, "Failed to retrieve posts.");
            }
        }

        public async Task<IBusinessResult> GetPostById(int id)
        {
            try
            {
                // Council D11: public detail maps through PostResponse (same shape as
                // the feed endpoints) so blog detail renders in one call - including
                // ImageUrls. Non-approved posts read as null upstream (D2) and surface
                // here as the standard no-data warning -> 404 at the controller.
                var post = await _store.GetPostByIdAsync(id);
                if (post == null)
                {
                    return new BusinessResult(ResponseCodes.WarningNoDataCode, ResponseCodes.FailReadMessage);
                }
                var elementDict = await _store.GetElementNamesAsync();
                return new BusinessResult(ResponseCodes.SuccessReadCode, ResponseCodes.SuccessReadMessage, MapToResponse(post, elementDict));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostService.GetPostById failed for id={PostId}", id);
                return new BusinessResult(ResponseCodes.ErrorException, "Failed to retrieve post.");
            }
        }

        public async Task<IBusinessResult> GetCategories()
        {
            try
            {
                var categories = await _store.GetPostCategoriesAsync();
                var responses = categories
                    .Select(c => new PostCategoryResponse(c.Id, c.PostType))
                    .ToList();
                return new BusinessResult(ResponseCodes.SuccessReadCode, ResponseCodes.SuccessReadMessage, responses);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostService.GetCategories failed");
                return new BusinessResult(ResponseCodes.ErrorException, "Failed to retrieve post categories.");
            }
        }

        public async Task<IBusinessResult> GetPostByIdForAdmin(int id)
        {
            try
            {
                // Admin bypass (council D2): full queue visibility with images,
                // regardless of status. Non-existent ids read as null -> no-data.
                var post = await _store.GetAdminPostByIdWithImagesAsync(id);
                if (post == null)
                {
                    return new BusinessResult(ResponseCodes.WarningNoDataCode, ResponseCodes.FailReadMessage);
                }
                var elementDict = await _store.GetElementNamesAsync();
                return new BusinessResult(ResponseCodes.SuccessReadCode, ResponseCodes.SuccessReadMessage, MapToResponse(post, elementDict));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostService.GetPostByIdForAdmin failed for id={PostId}", id);
                return new BusinessResult(ResponseCodes.ErrorException, "Failed to retrieve post.");
            }
        }

        public async Task<IBusinessResult> CreatePost(CreatePostRequest request, int authorAccountId)
        {
            try
            {
                var categoryExists = await _store.PostCategoryExistsAsync(request.CategoryId);
                if (!categoryExists)
                {
                    return new BusinessResult(ResponseCodes.WarningNoDataCode, "The provided CategoryId does not exist.");
                }

                var postImages = new List<PostImage>();
                if (request.ImageIds is { Count: > 0 })
                {
                    var distinctImageIds = request.ImageIds.Distinct().ToList();
                    var images = await _store.GetImagesByIdsAsync(distinctImageIds);
                    if (images.Count != distinctImageIds.Count)
                    {
                        return new BusinessResult(ResponseCodes.WarningNoDataCode, "One or more of the provided ImageIds do not exist.");
                    }

                    postImages.AddRange(images.Select(image => new PostImage
                    {
                        ImageId = image.ImageId,
                        ImageDescription = "Member upload"
                    }));
                }

                // Explicit mapping: everything not present on CreatePostRequest is
                // server-owned (Status, timestamps, identity, author) and can never
                // be mass-assigned by a client.
                var post = new Post
                {
                    Name = request.Title,
                    Description = request.Content,
                    PostCategoryId = request.CategoryId,
                    AccountId = authorAccountId,
                    Status = MemberPostDefaultStatus,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow,
                };
                foreach (var postImage in postImages)
                {
                    post.PostImages.Add(postImage);
                }

                await _store.AddPostAsync(post);
                return new BusinessResult(ResponseCodes.SuccessCreateCode, ResponseCodes.SuccessCreateMessage);
            }
            catch (Exception)
            {
                return new BusinessResult(ResponseCodes.ErrorException, "Failed to create post.");
            }
        }

        public async Task<IBusinessResult> DeletePost(int id)
        {
            try
            {
                var deleted = await _store.DeletePostAsync(id);
                if (!deleted)
                {
                    return new BusinessResult(ResponseCodes.WarningNoDataCode, ResponseCodes.WarningNoDataMessage);
                }
                return new BusinessResult(ResponseCodes.SuccessDeleteCode, ResponseCodes.SuccessDeleteMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostService.DeletePost failed for id={PostId}", id);
                return new BusinessResult(ResponseCodes.ErrorException, "Failed to delete post.");
            }
        }

        public async Task<IBusinessResult> Save()
        {
            try
            {
                var saved = await _store.SavePostChangesAsync();
                if (saved)
                {
                    return new BusinessResult(ResponseCodes.SuccessCreateCode, ResponseCodes.SuccessCreateMessage);
                }
                else
                {
                    return new BusinessResult(ResponseCodes.FailCreateCode, ResponseCodes.FailCreateMessage);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostService.Save failed");
                return new BusinessResult(ResponseCodes.ErrorException, "Failed to save changes.");
            }
        }

        private static PostResponse MapToResponse(Post po, IReadOnlyDictionary<int, string> elementDict) => new()
        {
            PostId = po.PostId,
            Description = po.Description,
            CreateAt = po.CreateAt,
            AccountId = po.AccountId,
            UpdateAt = po.UpdateAt,
            // Posts without an element (e.g. member submissions) read as "uncategorized"
            // instead of throwing on the nullable cast.
            ElementId = po.ElementId ?? 0,
            Follows = po.Follows,
            Id = po.PostCategoryId,
            Name = po.Name,
            ElementName = po.ElementId.HasValue && elementDict.TryGetValue(po.ElementId.Value, out var en) ? en : null,
            AccountName = "N/A", // Account nav removed - use AccountId for lookup
            Status = po.Status,
            ImageUrls = po.PostImages?
                .Where(pi => pi?.Image != null)
                .Select(pi => pi.Image.ImageUrl)
                .ToList() ?? [],
        };
    }
}
