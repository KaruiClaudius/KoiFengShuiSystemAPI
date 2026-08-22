using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Interface;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;

namespace KoiFengShuiSystem.DataAccess.Repositories.Implement
{
    public class UnitOfWorkRepository : IUnitOfWorkRepository
    {
        private readonly KoiFengShuiContext _unitOfWorkContext;
        private PostRepository? _postRepository;
        private ImageRepository? _imageRepository;
        private ElementRepository? _elementRepository;

        public UnitOfWorkRepository(KoiFengShuiContext context)
        {
            _unitOfWorkContext = context;
        }

        public PostRepository PostRepository => _postRepository ??= new PostRepository(_unitOfWorkContext);

        public ImageRepository ImageRepository => _imageRepository ??= new ImageRepository(_unitOfWorkContext);

        public ElementRepository ElementRepository => _elementRepository ??= new ElementRepository(_unitOfWorkContext);

        public int SaveChangesWithTransaction()
        {
            int result = -1;

            using (var dbContextTransaction = _unitOfWorkContext.Database.BeginTransaction())
            {
                try
                {
                    result = _unitOfWorkContext.SaveChanges();
                    dbContextTransaction.Commit();
                }
                catch
                {
                    result = -1;
                    dbContextTransaction.Rollback();
                }
            }

            return result;
        }

        public async Task<int> SaveChangesWithTransactionAsync()
        {
            int result = -1;

            using (var dbContextTransaction = _unitOfWorkContext.Database.BeginTransaction())
            {
                try
                {
                    result = await _unitOfWorkContext.SaveChangesAsync();
                    dbContextTransaction.Commit();
                }
                catch
                {
                    result = -1;
                    dbContextTransaction.Rollback();
                }
            }

            return result;
        }
    }
}
