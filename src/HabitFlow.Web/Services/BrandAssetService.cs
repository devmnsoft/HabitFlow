namespace HabitFlow.Web.Services;

public sealed class BrandAssetService : IBrandAssetService
{
    private readonly IWebHostEnvironment environment;

    public BrandAssetService(IWebHostEnvironment environment) => this.environment = environment;

    public bool Exists(string relativeWwwrootPath)
    {
        if (string.IsNullOrWhiteSpace(relativeWwwrootPath) || relativeWwwrootPath.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        var cleanPath = relativeWwwrootPath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(environment.WebRootPath ?? string.Empty, cleanPath);
        return File.Exists(fullPath);
    }
}
