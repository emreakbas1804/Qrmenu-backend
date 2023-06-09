using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataAccess.Context;
using webApi.Context;


namespace webApi.Extensions
{
    public static class MigrationManager
    {
        public static IHost MigrateDatabase(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                using (var ApplicationContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>())
                {
                    try
                    {
                        ApplicationContext.Database.Migrate();
                    }
                    catch (System.Exception)
                    {

                        throw;
                    }
                }

                using (var QrliMenuContext = scope.ServiceProvider.GetRequiredService<QrliMenuContext>())
                {
                    try
                    {
                        QrliMenuContext.Database.Migrate();
                    }
                    catch (System.Exception)
                    {

                        throw;
                    }
                }
            }
            return host;
        }
    }
}