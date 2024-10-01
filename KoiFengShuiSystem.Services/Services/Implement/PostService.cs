using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class PostService : IPostService
    {
        private readonly GenericRepository<Post> _postRepository;
        public PostService(GenericRepository<Post> postRepository)
        {
            _postRepository = postRepository;
        }
        public async Task<Post> CreateAsync(Post post)
        {
            _postRepository.PrepareCreate(post);

            // Save changes asynchronously
            await _postRepository.SaveAsync();
            // Return the newly created 
            return post;
        }

        public void Delete(int id)
        {
            var post = _postRepository.GetById(id);
            if (post == null)
                throw new ApplicationException("Account not found");

            _postRepository.PrepareRemove(post);
            _postRepository.Save();
        }

        public IEnumerable<Post> GetAll()
        {
            return _postRepository.GetAll();
        }

        public Post? GetById(int id)
        {
            return _postRepository.GetById(id);
        }

       /* public void Update(int id, UpdateRequest model)
        {
            var post = _postRepository.GetById(id);

            // Validate
            if (post == null)
                throw new ApplicationException("Account not found");
           *//* if (!string.IsNullOrEmpty(model.Email) && model.Email != account.Email && _accountRepository.GetAll().Any(x => x.Email == model.Email))
                throw new ApplicationException("Email '" + model.Email + "' is already taken");*//*

            // Update account properties
            if (!string.IsNullOrEmpty(model.Email))
                account.Email = model.Email;
            if (!string.IsNullOrEmpty(model.Password))
                account.Password = model.Password; // Note: In a real application, you should hash this password

            _accountRepository.PrepareUpdate(account);
            _accountRepository.Save();
        }*/
    }
}
