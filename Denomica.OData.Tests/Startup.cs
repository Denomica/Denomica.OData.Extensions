
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.OData.Tests
{
    internal static class Startup
    {

        internal static ServiceProvider GetServiceProvider(string configFolder)
        {
            var configRoot = new ConfigurationBuilder()
                .SetBasePath(configFolder)
                .AddJsonFile("test.settings.json", optional: false)

                .AddEnvironmentVariables("denomica:odata:tests")
                .Build();

            return new ServiceCollection()
                .AddSingleton(configRoot)
                .AddSingleton<IConfiguration>(configRoot)

                .AddLogging()
                .BuildServiceProvider();
        }
    }
}
