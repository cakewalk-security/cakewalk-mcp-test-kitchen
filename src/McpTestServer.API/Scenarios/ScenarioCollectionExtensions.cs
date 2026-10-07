namespace McpTestServer.API.Scenarios;

public static class ScenarioCollectionExtensions
{
    public static IServiceCollection AddScenario<TScenario>(this IServiceCollection services)
        where TScenario : class, IScenario, new()
    {
        services.AddSingleton<IScenario, TScenario>();
        return services;
    }

    public static IServiceCollection AddScenarioCatalog(this IServiceCollection services)
    {
        services.AddSingleton<ScenarioCatalog>();
        services.AddSingleton<IScenarioCatalog>(sp =>
        {
            var catalog = sp.GetRequiredService<ScenarioCatalog>();
            foreach (var scenario in sp.GetServices<IScenario>())
            {
                catalog.Register(scenario);
            }

            return catalog;
        });

        return services;
    }
}
