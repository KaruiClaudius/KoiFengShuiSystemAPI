using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class ImageService : IImageService
    {
        private readonly UnitOfWorkRepository _unitOfWorkRepository;

        public ImageService()
        {
            _unitOfWorkRepository = new UnitOfWorkRepository();
        }
        public async Task<bool> SaveImageAsync(string imageUrl)
        {
            var image = new Image { ImageUrl = imageUrl };
            var result = await _unitOfWorkRepository.ImageRepository.CreateAsync(image);
            return result > 0;
        }
    }
}
