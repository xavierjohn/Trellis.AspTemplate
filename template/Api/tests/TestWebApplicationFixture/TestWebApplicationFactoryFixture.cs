namespace Api.Tests;

using MartinCostello.Logging.XUnit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using TodoSample.AntiCorruptionLayer;
using Trellis.EntityFrameworkCore;
using Xunit.v3;

public class TestWebApplicationFactoryFixture : WebApplicationFactory<Program>, ITestOutputHelperAccessor
{
    private readonly SqliteConnection _connection;

    public TestWebApplicationFactoryFixture()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        // Keep a persistent connection for in-memory SQLite
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public ITestOutputHelper? OutputHelper { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(p => p.AddXUnit(this));

        builder.ConfigureServices(services =>
        {
            // Remove the production database registration
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection)
                       .AddTrellisInterceptors());
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}

[CollectionDefinition(Id)]
public class TestWebApplicationFactoryCollectionFixture : ICollectionFixture<TestWebApplicationFactoryFixture>
{
    public const string Id = "Test web application factory fixture collection";
}
