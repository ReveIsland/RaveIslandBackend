namespace RaveIsland.ApiService.Infrastructure.Email;

public sealed class AppOptions
{
    public const string SectionName = "App";

    public string WebBaseUrl { get; set; } = "http://localhost:5173";
}
