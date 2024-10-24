﻿using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class AdminPostService : IAdminPostService
    {
        private readonly KoiFengShuiContext _context;

        public AdminPostService(KoiFengShuiContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AdminPostResponse>> GetAllPostsAsync()
        {
            var posts = await _context.Posts.Include(p => p.Element).Include(p => p.Account).ToListAsync();
            return posts.Select(p => MapToAdminPostResponse(p));
        }

        public async Task<AdminPostResponse> GetPostByIdAsync(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Element)
                .Include(p => p.Account)
                .FirstOrDefaultAsync(p => p.PostId == id);
            return post == null ? null : MapToAdminPostResponse(post);
        }

        public async Task<AdminPostResponse> CreatePostAsync(PostRequest postRequest)
        {
            var post = new Post
            {
                Id = postRequest.ElementId, 
                Name = postRequest.Name,
                Description = postRequest.Description,
                AccountId = postRequest.AccountId,
                Status = postRequest.Status,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return MapToAdminPostResponse(post);
        }
        public async Task<AdminPostResponse> UpdatePostAsync(int id, PostRequest postRequest)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return null;

            post.Id = postRequest.Id;
            post.Name = postRequest.Name;
            post.Description = postRequest.Description;
            post.UpdateAt = DateTime.Now;
            post.ElementId = postRequest.ElementId;
            post.Status = postRequest.Status;

            await _context.SaveChangesAsync();

            return await GetPostByIdAsync(id);
        }

        public async Task<bool> DeletePostAsync(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return false;

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<AdminPostResponse>> GetAllAdminPostsAsync()
        {
            var adminPosts = await _context.Posts
                .Include(p => p.Element)
                .Include(p => p.Account)
                .Where(p => p.Account.RoleId == 1) // Assuming RoleId 1 is for admin
                .ToListAsync();

            return adminPosts.Select(p => MapToAdminPostResponse(p));
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
                ElementId = post.ElementId,
                Status = post.Status,
                ElementName = post.Element?.ElementName,
                AccountName = post.Account?.FullName
            };
        }
    }
}