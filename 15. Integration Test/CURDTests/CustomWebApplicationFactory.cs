using Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;     // Added for DbContextOptions and UseInMemoryDatabase
using System.Xml;

namespace CURDTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
   protected override void ConfigureWebHost(IWebHostBuilder builder)
   {
      base.ConfigureWebHost(builder);

      builder.UseEnvironment("Test");

      builder.ConfigureServices(services =>
      {
         var descripter = services.SingleOrDefault(temp => temp.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

         if (descripter != null)
         {
            services.Remove(descripter);
         }

         services.AddDbContext<ApplicationDbContext>(options =>
         {
            options.UseInMemoryDatabase("DatbaseForTesting");
         });

      });
   }
}
