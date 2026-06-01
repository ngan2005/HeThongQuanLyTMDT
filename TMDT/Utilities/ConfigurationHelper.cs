using Microsoft.Extensions.Configuration;
using System.IO;

namespace TMDT.Utilities
{
    public static class ConfigurationHelper
    {
        private static IConfiguration _configuration;

        public static IConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    _configuration = builder.Build();
                }
                return _configuration;
            }
        }
    }
}
