using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.DataAccess.Repositories.Interface
{
    public interface IUnitOfWorkRepository
    {
        public int SaveChangesWithTransaction();
        public Task<int> SaveChangesWithTransactionAsync();
       
    }
}
