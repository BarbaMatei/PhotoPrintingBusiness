using RazorLight;

namespace PhotoPrint.API.Services;

public class RazorTemplateService : IRazorTemplateService
{
    private readonly RazorLightEngine _engine;
    private readonly ILogger<RazorTemplateService> _logger;

    public RazorTemplateService(IWebHostEnvironment environment, ILogger<RazorTemplateService> logger)
    {
        _logger = logger;

        var templatesPath = Path.Combine(environment.ContentRootPath, "EmailTemplates");

        // Ensure directory exists at startup — templates are deployed with the app
        if (!Directory.Exists(templatesPath))
        {
            Directory.CreateDirectory(templatesPath);
        }

        _engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(templatesPath)
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<string> RenderAsync<T>(string templateName, T model)
    {
        var key = $"{templateName}.cshtml";

        _logger.LogDebug("Rendering email template {Template}", key);

        try
        {
            return await _engine.CompileRenderAsync(key, model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render email template {Template}", key);
            throw;
        }
    }
}
