// Sample starter code for the class assignment. This code is not guaranteed to be correct or complete.
// and the student is expected to test their final application and resolve all issues. Bugs found in
// this code should be reported to the instructor. They must be fixed by the student as part of the assignment.
//
// Additionally, this code is not guaranteed to be the optimal solution for a production server. I have
// intentionally left out some best practices to make the code easier to understand. Additionally, the console
// logger used for OpenTelemetry is not recommended for production use. Instead, you should use a more robust
// collector as defined with OpenTelemtry.
//
// Please search all files for "TODO:" to see places where you need to write your own code.

using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Telemetry.Trace;

using AzureFileServer.FileServer;

namespace AzureFileServer;


class Program
{

    static void Main(String[] args)
    {
         WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Instead of hardcoding the values for various strings, we are going to put them into
        // a configuration file to make it easier to read. This is especially useful when you
        // want to have the container act one way in release vs debug, or locally vs in Azure.
        // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0

        IConfiguration configuration = builder.Configuration;

        string serviceName = configuration["Logging:ServiceName"];
        string serviceVersion = configuration["Logging:ServiceVersion"];

        // Configure important OpenTelemetry settings, the console exporter, and instrumentation library
        builder.Services.AddOpenTelemetry().WithTracing(tcb =>
        {
            tcb
            .AddSource(serviceName)
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
            .AddAspNetCoreInstrumentation() // Automatically generate log lines for HTTP requests
            .AddJsonConsoleExporter(); // Output log lines to the console
        });

        FileServerHandlers instance = new FileServerHandlers(configuration);

        WebApplication app = builder.Build();

        app.MapGet("/", instance.HomeDelegate);

        app.MapGet("/healthcheck", instance.HealthCheckDelegate);
        app.MapGet("/download", instance.DownloadFileDelegate);
        app.MapGet("/listfiles", instance.ListFilesDelegate);
        app.MapGet("/delete", instance.DeleteFileDelegate);
               
        app.MapPost("/uploadfile", instance.UploadFileDelegate);

        app.Run();
    }
}