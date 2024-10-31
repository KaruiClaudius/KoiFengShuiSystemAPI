using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using KoiFengShuiSystem.DataAccess;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;

namespace KoiFengShuiSystem.BusinessLogic.Services
{
    public class AdminPostService : IAdminPostService
    {
        private readonly KoiFengShuiContext _context;
        private readonly IImageService _imageService;

        public AdminPostService(KoiFengShuiContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        public async Task<List<AdminPostResponse>> GetAllAdminPostsAsync()
        {
            var posts = await _context.Posts
                .Include(p => p.Element)
                .Include(p => p.Account)
                .Include(p => p.PostImages)
                    .ThenInclude(pi => pi.Image)
                .ToListAsync();

            return posts.Select(MapToAdminPostResponse).ToList();
        }

        public async Task<AdminPostResponse> GetAdminPostByIdAsync(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Element)
                .Include(p => p.Account)
                .Include(p => p.PostImages)
                    .ThenInclude(pi => pi.Image)
                .FirstOrDefaultAsync(p => p.PostId == id);

            return post == null ? null : MapToAdminPostResponse(post);
        }

        public async Task<AdminPostResponse> UpdateAdminPostAsync(int id, AdminPostRequest adminPostRequest, List<string> imageUrls)
        {
            var post = await _context.Posts
                .Include(p => p.PostImages)
                .ThenInclude(pi => pi.Image)
                .FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null)
            {
                return null;
            }

            post.Name = adminPostRequest.Name;
            post.Description = adminPostRequest.Description;
            post.Status = adminPostRequest.Status;
            post.UpdateAt = DateTime.Now;
           
            var existingImageUrls = post.PostImages.Select(pi => pi.Image.ImageUrl).ToList();
            var imagesToRemove = post.PostImages.Where(pi => !imageUrls.Contains(pi.Image.ImageUrl)).ToList();

            foreach (var postImage in imagesToRemove)
            {
                _context.PostImages.Remove(postImage);
                _context.Images.Remove(postImage.Image);
            }

            var newImageUrls = imageUrls.Except(existingImageUrls).ToList();
            foreach (var imageUrl in newImageUrls)
            {
                var newImage = new Image { ImageUrl = imageUrl };
                _context.Images.Add(newImage);
                await _context.SaveChangesAsync();

                var postImage = new PostImage
                {
                    PostId = post.PostId,
                    ImageId = newImage.ImageId,
                    ImageDescription = "Default description"
                };
                _context.PostImages.Add(postImage);
            }

            await _context.SaveChangesAsync();

            return await GetAdminPostByIdAsync(post.PostId);
        }

        public async Task<AdminPostResponse> CreatePostWithImagesAsync(AdminPostRequest adminPostRequest, List<string> imageUrls)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                //post ifo
                var postCategoryExists = await _context.PostCategories.AnyAsync(pc => pc.Id == adminPostRequest.Id);
                if (!postCategoryExists)
                {
                    throw new ArgumentException("The provided Id does not exist in the PostCategory table.");
                }

                var post = new Post
                {
                    Id = adminPostRequest.Id,
                    Name = adminPostRequest.Name,
                    Description = adminPostRequest.Description,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow,
                    AccountId = adminPostRequest.AccountId,
                    Status = adminPostRequest.Status
                };

                _context.Posts.Add(post);
                await _context.SaveChangesAsync();
                //image
                foreach (var imageUrl in imageUrls)
                {
                    var newImage = new Image { ImageUrl = imageUrl };
                    _context.Images.Add(newImage);
                    await _context.SaveChangesAsync();

                    var postImage = new PostImage
                    {
                        PostId = post.PostId,
                        ImageId = newImage.ImageId,
                        ImageDescription = "Default description"//auto set postimage ImageDescription
                    };
                    _context.PostImages.Add(postImage);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetAdminPostByIdAsync(post.PostId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<bool> DeletePostWithAllRelatedAsync(int postId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
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
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private AdminPostResponse MapToAdminPostResponse(Post post)
        {
            return new AdminPostResponse
            {
                PostId = post.PostId,
                Id = post.Id,
                Name = post.Name,
                Description = post.Description,
                CreateAt = post.CreateAt,
                UpdateAt = post.UpdateAt,
                AccountId = post.AccountId,            
                Status = post.Status,               
                AccountName = post.Account?.FullName,
                ImageUrls = post.PostImages.Select(pi => pi.Image.ImageUrl).ToList()
            };
        }
    }
}