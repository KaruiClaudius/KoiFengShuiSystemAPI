using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Interface;

namespace KoiFengShuiSystem.DataAccess.Repositories.Implement
{
    public class UnitOfWorkRepository : IUnitOfWorkRepository
    {
        private readonly KoiFengShuiContext _unitOfWorkContext;
        private PostRepository? _postRepository;
        private MarketplaceListingRepository? _marketplaceListingsRepository;
        private ImageRepository? _imageRepository;
        private ElementRepository? _elementRepository;
        private MarketCategoryRepository? _marketCategoryRepository;
        private SubcriptionTiersRepository? _subcriptionTiersRepository;

        public UnitOfWorkRepository(KoiFengShuiContext context)
        {
            _unitOfWorkContext = context;
        }

        public PostRepository PostRepository => _postRepository ??= new PostRepository(_unitOfWorkContext);

        public MarketplaceListingRepository MarketplaceListingRepository => _marketplaceListingsRepository ??= new MarketplaceListingRepository(_unitOfWorkContext);

        public ImageRepository ImageRepository => _imageRepository ??= new ImageRepository(_unitOfWorkContext);

        public ElementRepository ElementRepository => _elementRepository ??= new ElementRepository(_unitOfWorkContext);

        public MarketCategoryRepository MarketCategoryRepository => _marketCategoryRepository ??= new MarketCategoryRepository(_unitOfWorkContext);

        public SubcriptionTiersRepository SubcriptionTiersRepository => _subcriptionTiersRepository ??= new SubcriptionTiersRepository(_unitOfWorkContext);

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
