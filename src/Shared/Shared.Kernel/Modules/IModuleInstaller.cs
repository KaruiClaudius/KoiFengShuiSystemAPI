using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KoiFengShuiSystem.Shared.Kernel.Modules
{
    public interface IModuleInstaller
    {
        void AddServices(IServiceCollection services, IConfiguration configuration);
    }
}
