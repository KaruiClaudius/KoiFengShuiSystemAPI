using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Modules.Community.Application.Services;
using KoiFengShuiSystem.Modules.Community.Infrastructure.CloudStorage;
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
            // Binds the legacy "CloundSettings" section (keys unchanged for
            // config compatibility) onto the clean CloudStorageSettings class:
            // CloundName -> CloudName, CloundKey -> ApiKey, CloundSecret -> ApiSecret.
            services.Configure<CloudStorageSettings>(options =>
            {
                options.CloudName = configuration["CloundSettings:CloundName"] ?? string.Empty;
                options.ApiKey = configuration["CloundSettings:CloundKey"] ?? string.Empty;
                options.ApiSecret = configuration["CloundSettings:CloundSecret"] ?? string.Empty;
            });

            services.AddScoped<ICommunityStore, EfCommunityStore>();
            services.AddScoped<ICloudStorage, CloudStorageService>();
            services.AddScoped<IFaqService, FaqService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IAdminPostService, AdminPostService>();
        }
    }
}
