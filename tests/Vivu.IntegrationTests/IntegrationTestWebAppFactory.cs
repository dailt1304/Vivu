using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Vivu.Infrastructure.Data;
using Vivu.WebApi;
namespace Vivu.IntegrationTests
{
    public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgis/postgis:15-3.3-alpine")
            .WithDatabase("vivu_test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        public async Task InitializeAsync()
        {
            await _dbContainer.StartAsync();

            using (var scope = Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<VivuDbContext>();

                // Create extensions BEFORE applying migrations
                await dbContext.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS unaccent;");
                await dbContext.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
                
                // Create the f_unaccent functions BEFORE migrations
                await dbContext.Database.ExecuteSqlRawAsync(@"
                    CREATE OR REPLACE FUNCTION f_unaccent(text)
                      RETURNS text AS
                    $func$
                      SELECT public.unaccent('public.unaccent', $1)
                    $func$  LANGUAGE sql IMMUTABLE PARALLEL SAFE;
                ");

                await dbContext.Database.ExecuteSqlRawAsync(@"
                    CREATE OR REPLACE FUNCTION f_unaccent(character varying)
                      RETURNS text AS
                    $func$
                      SELECT public.unaccent('public.unaccent', $1::text)
                    $func$  LANGUAGE sql IMMUTABLE PARALLEL SAFE;
                ");

                await dbContext.Database.EnsureCreatedAsync();

            }
        }
        public new Task DisposeAsync() => _dbContainer.DisposeAsync().AsTask();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Secret", "DayLaMotCaiKeyBiMatDaiDeTest123456789" },
                { "Jwt:Issuer", "VivuTest" },
                { "Jwt:Audience", "VivuUser" },
                { "Jwt:ExpiryMinutes", "60" }
            });
            });

            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<VivuDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<VivuDbContext>(options =>
                {
                    options.UseNpgsql(_dbContainer.GetConnectionString(),
                        o => o.UseNetTopologySuite());
                });
            });
        }
    }
}
