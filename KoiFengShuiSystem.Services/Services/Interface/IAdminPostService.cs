﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface IAdminPostService
    {
        Task<List<AdminPostResponse>> GetAllAdminPostsAsync();
        Task<AdminPostResponse> GetAdminPostByIdAsync(int id);
        Task<AdminPostResponse> UpdateAdminPostAsync(int id, AdminPostRequest adminPostRequest, List<IFormFile> images);
        Task<AdminPostResponse> CreatePostWithImagesAsync(AdminPostRequest adminPostRequest, List<IFormFile> images);
        Task<bool> DeletePostWithAllRelatedAsync(int postId);
    }
}