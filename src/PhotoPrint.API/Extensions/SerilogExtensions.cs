using Serilog;

namespace PhotoPrint.API.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, lc) =>
            lc.ReadFrom.Configuration(ctx.Configuration));

        return builder;
    }
}
