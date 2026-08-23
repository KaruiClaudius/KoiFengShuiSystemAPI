using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Modules.Community.Application.Services;
using KoiFengShuiSystem.Modules.Community.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Kernel.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KoiFengShuiSystem.Modules.Community.Infrastructure
{
    public class CommunityModuleInstaller : IModuleInstaller
    {
        public void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ICommunityStore, EfCommunityStore>();
            services.AddScoped<IFaqService, FaqService>();
        }
    }
}
