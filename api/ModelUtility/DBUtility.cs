using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace MyApp.Namespace.ModelUtility
{
    public static class DBUtility
    {
        private static IConfiguration? _configuration;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static string GetConnectionString()
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException("DBUtility has not been initialized. Call Initialize() first.");
            }

            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
            }

            return connectionString;
        }

        public static MySqlConnection GetConnection()
        {
            var connectionString = GetConnectionString();
            return new MySqlConnection(connectionString);
        }
    }
}

