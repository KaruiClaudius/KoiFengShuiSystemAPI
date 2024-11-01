using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.DataAccess.Repositories.Implement
{
    public class SubcriptionTiersRepository : GenericRepository<SubcriptionTier>
    {
        public SubcriptionTiersRepository() { }
        public SubcriptionTiersRepository(KoiFengShuiContext context) => _context = context;
    }
}
