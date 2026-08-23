using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Interface;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;

namespace KoiFengShuiSystem.DataAccess.Repositories.Implement
{
    public class UnitOfWorkRepository : IUnitOfWorkRepository
    {
        private readonly KoiFengShuiContext _unitOfWorkContext;
        private ElementRepository? _elementRepository;

        public UnitOfWorkRepository(KoiFengShuiContext context)
        {
            _unitOfWorkContext = context;
        }

        public ElementRepository ElementRepository => _elementRepository ??= new ElementRepository(_unitOfWorkContext);
    }
}
