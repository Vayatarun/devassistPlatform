using Microsoft.Build.Locator;

public static class MSBuildInitializer
{
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;

        var instance = MSBuildLocator
            .QueryVisualStudioInstances()
            .OrderByDescending(x => x.Version)
            .First();

        MSBuildLocator.RegisterInstance(instance);

        _initialized = true;
    }
}