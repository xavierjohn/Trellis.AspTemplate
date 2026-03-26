namespace Application.Tests;

using TodoSample.Application;
using Microsoft.Extensions.Hosting;

public class Startup
{
    public static void ConfigureHost(IHostBuilder hostBuilder) =>
        hostBuilder
            .ConfigureServices((context, services) =>
            {
                services.AddApplication()
                .AddMockDependencies();
            });
}
