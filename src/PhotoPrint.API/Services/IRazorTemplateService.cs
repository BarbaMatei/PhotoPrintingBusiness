namespace PhotoPrint.API.Services;

public interface IRazorTemplateService
{
    Task<string> RenderAsync<T>(string templateName, T model);
}
