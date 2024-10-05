using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.BusinessLogic.ViewModel;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;
using KoiFengShuiSystem.Shared.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KoiFengShuiSystem.Common;
namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class PostService : IPostService
    {
        private readonly UnitOfWorkRepository _unitOfWork;

        public PostService()
        {
            _unitOfWork = new UnitOfWorkRepository();
        }

        public async Task<IBusinessResult> GetAll()
        {
            try
            {
                var posts = await _unitOfWork.PostRepository.GetAllWithElementAsync();
                if (posts == null)
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);
                }
                else
                {
                    return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, posts);
                }
            }
            catch (Exception e)
            {
                return new BusinessResult(-4, e.Message.ToString());
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
                return new BusinessResult(Const.ERROR_EXCEPTION, e.Message);
            }
        }

        public async Task<IBusinessResult> CreatePost(Post post)
        {
            try
            {
                var entityInDb = await _unitOfWork.PostRepository.GetByIdAsync(post.PostId);

                // If Post already exists, return an error indicating duplicate Post
                if (entityInDb != null)
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, "Post already exists. Cannot create a new Post with the same ID.");
                }

                // If Post doesn't exist, create a new one
                await _unitOfWork.PostRepository.CreateAsync(post);
                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG);
            }
            catch (Exception ex)
            {
                return new BusinessResult(-4, ex.Message);
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
                return new BusinessResult(-4, ex.ToString());
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
                return new BusinessResult(-4, e.Message.ToString());
            }
        }


    }
}

