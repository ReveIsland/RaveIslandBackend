using System.Reflection;

namespace RaveIsland.ApiService.Common;

public static class EndpointExtensions
{
    public static WebApplication MapFeatureEndpoints(this WebApplication app)
    {
        var endpointTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } &&
                        t.GetMethod(nameof(IEndpoint.Map), BindingFlags.Public | BindingFlags.Static) is not null);

        foreach (var type in endpointTypes)
        {
            var mapMethod = type.GetMethod(nameof(IEndpoint.Map), BindingFlags.Public | BindingFlags.Static)!;
            mapMethod.Invoke(null, [app]);
        }

        return app;
    }
}
