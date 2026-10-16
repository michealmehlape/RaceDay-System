using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

// Starts the real API in memory, but with a throwaway database
public class ApiFactory : WebApplicationFactory<Program>
{
    // A unique name per factory, so every test gets its own empty database
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OrganiserAccessCode"] = TestHelpers.OrganiserCode
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server registration of the DbContext
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericArguments().Contains(typeof(RaceDayDbContext)) &&
                 d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration"))).ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            // Use an in-memory database instead
            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }

    // Each client has its own cookie jar, so each one is its own "browser"
    public HttpClient NewClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true
    });
}