using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Application.Services;
using KoiFengShuiSystem.Modules.FengShui.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Kernel.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KoiFengShuiSystem.Modules.FengShui.Infrastructure
{
    public class FengShuiModuleInstaller : IModuleInstaller
    {
        public void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();
            services.AddScoped<EfFengShuiReadStore>();
            services.AddScoped<IFengShuiReadStore>(sp => new CachedFengShuiReadStore(
                sp.GetRequiredService<EfFengShuiReadStore>(),
                sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>()));
            services.AddScoped<IPartnerShopStore, EfPartnerShopStore>();
            services.AddScoped<ICompatibilityService, CompatibilityService>();
            services.AddScoped<IConsultationService, ConsultationService>();
            services.AddScoped<IElementService, ElementService>();
            services.AddScoped<IPartnerShopService, PartnerShopService>();
        }
    }
}
