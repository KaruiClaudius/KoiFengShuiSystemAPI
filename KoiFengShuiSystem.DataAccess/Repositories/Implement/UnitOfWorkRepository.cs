using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.DataAccess.Repositories.Implement
{
    public class UnitOfWorkRepository : IUnitOfWorkRepository
    {
        private readonly KoiFengShuiContext _unitOfWorkContext;
        private PostRepository _postRepository;
        private MarketplaceListingRepository _marketplaceListingsRepository;
        private ImageRepository _imageRepository;
        private ElementRepository _elementRepository;
        private MarketCategoryRepository _marketCategoryRepository;
        private SubcriptionTiersRepository _subcriptionTiersRepository;
        public UnitOfWorkRepository()
        {
            _unitOfWorkContext ??= new KoiFengShuiContext(new DbContextOptions<KoiFengShuiContext>());
        }
        public PostRepository PostRepository
        {
            get
            {
                return _postRepository ??= new PostRepository();
            }
        }

        public MarketplaceListingRepository MarketplaceListingRepository
        {
            get
            {
                return _marketplaceListingsRepository ??= new MarketplaceListingRepository();
            }
        }

        public ImageRepository ImageRepository
        {
            get
            {
                return _imageRepository ??= new ImageRepository();
            }
        }
        public ElementRepository ElementRepository
        {
            get
            {
                return _elementRepository ??= new ElementRepository();
            }
        }

        public MarketCategoryRepository MarketCategoryRepository
        {
            get
            {
                return _marketCategoryRepository ??= new MarketCategoryRepository();
            }
        }

        public SubcriptionTiersRepository SubcriptionTiersRepository
        {
            get
            {
                return _subcriptionTiersRepository ??= new SubcriptionTiersRepository();
            }
        }
        ////TO-DO CODE HERE/////////////////

        #region Set transaction isolation levels

        /*
        Read Uncommitted: The lowest level of isolation, allows transactions to read uncommitted data from other transactions. This can lead to dirty reads and other issues.

        Read Committed: Transactions can only read data that has been committed by other transactions. This level avoids dirty reads but can still experience other isolation problems.

        Repeatable Read: Transactions can only read data that was committed before their execution, and all reads are repeatable. This prevents dirty reads and non-repeatable reads, but may still experience phantom reads.

        Serializable: The highest level of isolation, ensuring that transactions are completely isolated from one another. This can lead to increased lock contention, potentially hurting performance.

        Snapshot: This isolation level uses row versioning to avoid locks, providing consistency without impeding concurrency. 
         */

        public int SaveChangesWithTransaction()
        {
            int result = -1;

            //System.DataAccess.IsolationLevel.Snapshot
            using (var dbContextTransaction = _unitOfWorkContext.Database.BeginTransaction())
            {
                try
                {
                    result = _unitOfWorkContext.SaveChanges();
                    dbContextTransaction.Commit();
                }
                catch (Exception)
                {
                    //Log Exception Handling message                      
                    result = -1;
                    dbContextTransaction.Rollback();
                }
            }

            return result;
        }

        public async Task<int> SaveChangesWithTransactionAsync()
        {
            int result = -1;

            //System.DataAccess.IsolationLevel.Snapshot
            using (var dbContextTransaction = _unitOfWorkContext.Database.BeginTransaction())
            {
                try
                {
                    result = await _unitOfWorkContext.SaveChangesAsync();
                    dbContextTransaction.Commit();
                }
                catch (Exception)
                {
                    //Log Exception Handling message                      
                    result = -1;
                    dbContextTransaction.Rollback();
                }
            }

            return result;
        }

        #endregion

    }
}
