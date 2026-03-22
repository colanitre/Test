using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RpgApi.Data;

namespace RpgApi.IntegrationTests;

public class RpgTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove every descriptor that relates to RpgContext to avoid dual-provider conflicts
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<RpgContext>) ||
                    d.ServiceType == typeof(RpgContext) ||
                    d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            services.AddDbContext<RpgContext>(options =>
            {
                options.UseInMemoryDatabase($"RpgTestDb_{Guid.NewGuid()}");
            });
        });

        builder.UseEnvironment("Testing");
    }
}
