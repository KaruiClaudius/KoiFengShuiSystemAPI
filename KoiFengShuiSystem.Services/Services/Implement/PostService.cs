using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Kernel.Results;
using KoiFengShuiSystem.Common;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class PostService : IPostService
    {
        /// <summary>
        /// Server-assigned status for member-created posts; admins promote them
        /// through AdminPost management. Never accepted from the client.
        /// </summary>
        public const string MemberPostDefaultStatus = "Pending";

        private readonly UnitOfWorkRepository _unitOfWork;
        private readonly KoiFengShuiContext _context;
        private readonly ILogger<PostService> _logger;

        public PostService(UnitOfWorkRepository unitOfWork, KoiFengShuiContext context, ILogger<PostService> logger)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _logger = logger;
        }

        public async Task<IBusinessResult> GetAll()
        {
            try
            {
                var posts = await _unitOfWork.PostRepository.GetAllWithElementAsync();
                var elements = await _unitOfWork.ElementRepository.GetAllAsync();
                var elementDict = elements.ToDictionary(e => e.ElementId, e => e.ElementName);
                if (posts != null)
                {
                    var postResponses = posts.Select(po => new PostResponse
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
                        Id = po.Id,
                        Name = po.Name,
                        ElementName = po.ElementId.HasValue && elementDict.TryGetValue(po.ElementId.Value, out var en) ? en : null,
                        AccountName = "N/A", // Account nav removed - use AccountId for lookup
                        Status = po.Status,
                    }).ToList();
                    if (postResponses == null)
                    {
                        return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);
                    }
                    else
                    {
                        return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, postResponses);
                    }
                }
                return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostService.GetAll failed");
                return new BusinessResult(Const.ERROR_EXCEPTION, "Failed to retrieve posts.");
            }
        }

        public async Task<IBusinessResult> GetPostByPostTypeId(int postTypeId,  int pageNumber, int pageSize)
        {
            try
            {
                var posts = await _unitOfWork.PostRepository.GetAllByPostTypeIdAsync(postTypeId, pageNumber, pageSize);
                var elements = await _unitOfWork.ElementRepository.GetAllAsync();
                var elementDict = elements.ToDictionary(e => e.ElementId, e => e.ElementName);
                if (posts != null)
                {
                    var postResponses = posts.Select(po => new PostResponse
                    {
                        PostId = po.PostId,
                        Description = po.Description,
                        CreateAt = po.CreateAt,
                        AccountId = po.AccountId,
                        UpdateAt = po.UpdateAt,
                        // See GetAll: null element reads as "uncategorized".
                        ElementId = po.ElementId ?? 0,
                        Follows = po.Follows,
                        Id = po.Id,
                        Name = po.Name,
                        ElementName = po.ElementId.HasValue && elementDict.TryGetValue(po.ElementId.Value, out var en) ? en : null,
                        AccountName = "N/A", // Account nav removed - use AccountId for lookup
                        Status = po.Status,
                    }).ToList();
                    if (postResponses == null)
                    {
                        return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);
                    }
                    else
                    {
                        return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, postResponses);
                    }
                }
                return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostService.GetPostByPostTypeId failed for postTypeId={PostTypeId}", postTypeId);
                return new BusinessResult(Const.ERROR_EXCEPTION, "Failed to retrieve posts.");
            }
        }

        public async Task<IBusinessResult> GetPostById(int id)
        {
            try
            {
                var Post = await _unitOfWork.PostRepository.GetByIdAsync(id);
                if (Post == null)
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);
                }
                else
                {
                    return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, Post);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostService.GetPostById failed for id={PostId}", id);
                return new BusinessResult(Const.ERROR_EXCEPTION, "Failed to retrieve post.");
            }
        }



        public async Task<IBusinessResult> CreatePost(CreatePostRequest request, int authorAccountId)
        {
            try
            {
                var categoryExists = await _context.PostCategories.AnyAsync(c => c.Id == request.CategoryId);
                if (!categoryExists)
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, "The provided CategoryId does not exist.");
                }

                var postImages = new List<PostImage>();
                if (request.ImageIds is { Count: > 0 })
                {
                    var distinctImageIds = request.ImageIds.Distinct().ToList();
                    var images = await _unitOfWork.ImageRepository.GetAllAsync(i => distinctImageIds.Contains(i.ImageId));
                    if (images.Count != distinctImageIds.Count)
                    {
                        return new BusinessResult(Const.WARNING_NO_DATA_CODE, "One or more of the provided ImageIds do not exist.");
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
                    Id = request.CategoryId,
                    AccountId = authorAccountId,
                    Status = MemberPostDefaultStatus,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow,
                };
                foreach (var postImage in postImages)
                {
                    post.PostImages.Add(postImage);
                }

                await _unitOfWork.PostRepository.CreateAsync(post);
                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG);
            }
            catch (Exception)
            {
                return new BusinessResult(Const.ERROR_EXCEPTION, "Failed to create post.");
            }
        }

        // Helper method to compare two payments
        /*  private bool PostAreEqual(Post post, Post entityInDb)
          {
              return post.PostId == entityInDb.PostId &&
                     post.Id == entityInDb.Id &&
                     post.Name == entityInDb.Name &&
                     post.Description == entityInDb.Description &&
                     post.CreateAt == entityInDb.CreateAt &&
                     post.UpdateAt == entityInDb.UpdateAt &&
                     post.CreateBy == entityInDb.CreateBy &&
                     post.ElementId == entityInDb.ElementId &&
                     post.Price == entityInDb.Price;
          }*/


        public async Task<IBusinessResult> DeletePost(int id)
        {
            try
            {
                var Post = await _unitOfWork.PostRepository.GetByIdAsync(id);
                if (Post != null)
                {
                    var result = await _unitOfWork.PostRepository.RemoveAsync(Post);
                    if (result)
                    {
                        return new BusinessResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG);
                    }
                    else
                    {
                        return new BusinessResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
                    }
                }
                else
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA__MSG);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PostService.DeletePost failed for id={PostId}", id);
                return new BusinessResult(Const.ERROR_EXCEPTION, "Failed to delete post.");
            }
        }

        public async Task<IBusinessResult> Save()
        {
            try
            {
                var result = await _unitOfWork.PostRepository.SaveAsync();
                if (result > 0)
                {
                    return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG);
                }
                else
                {
                    return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "PostService.Save failed");
                return new BusinessResult(Const.ERROR_EXCEPTION, "Failed to save changes.");
            }
        }


    }
}

