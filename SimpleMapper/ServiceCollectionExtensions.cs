using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SimpleMapper.Internal;

namespace SimpleMapper;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSimpleMapper(this IServiceCollection services, params Assembly[] assembliesToScan)
    {
        var configExpression = new MapperConfigurationExpression();

        var profiles = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IMapProfile).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .Select(t => (IMapProfile)Activator.CreateInstance(t)!);

        foreach (var profile in profiles)
            profile.Configure(configExpression);

        services.AddSingleton(configExpression);
        services.AddSingleton<IMapper, Mapper>();
        services.AddSingleton<IMapperConfiguration>(sp => (IMapperConfiguration)sp.GetRequiredService<IMapper>());

        return services;
    }
}