using System.Reflection;
using KoiFengShuiSystem.Shared.Kernel.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KoiFengShuiSystem.Shared.Kernel.Modules
{
    public static class ModuleInstallerExtensions
    {
        public static IServiceCollection AddModuleInstallersFromAssemblies(
            this IServiceCollection services,
            IConfiguration configuration,
            params Assembly[] assemblies)
        {
            var installerTypes = assemblies
                .SelectMany(assembly => assembly.DefinedTypes)
                .Where(type => !type.IsAbstract && !type.IsInterface)
                .Where(type => typeof(IModuleInstaller).IsAssignableFrom(type.AsType()))
                .Select(type => type.AsType())
                .Distinct()
                .OrderBy(type => type.FullName, StringComparer.Ordinal);

            foreach (var installerType in installerTypes)
            {
                var installer = CreateInstaller(installerType);
                installer.AddServices(services, configuration);
            }

            return services;
        }

        private static IModuleInstaller CreateInstaller(Type installerType)
        {
            try
            {
                var installer = Activator.CreateInstance(installerType) as IModuleInstaller;
                if (installer is not null)
                {
                    return installer;
                }
            }
            catch (Exception ex) when (ex is MissingMethodException or MemberAccessException or TargetInvocationException)
            {
                throw new InvalidOperationException(
                    $"Module installer '{installerType.FullName}' must have a public parameterless constructor.",
                    ex);
            }

            throw new InvalidOperationException(
                $"Module installer '{installerType.FullName}' could not be created. Ensure it has a public parameterless constructor.");
        }
    }
}
