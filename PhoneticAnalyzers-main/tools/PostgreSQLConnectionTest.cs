using Npgsql;
using System;

namespace PhoneticAnalyzers.ConnectionTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🔗 PostgreSQL Connection Test");
            Console.WriteLine("================================");
            
            // Get connection string from user input or environment
            Console.Write("Enter your Azure PostgreSQL connection string: ");
            var connectionString = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Default Azure format - replace with your details
                connectionString = "Host=your-server.postgres.database.azure.com;Database=phonetic_analyzers;Username=pgadmin;Password=yourpassword;SSL Mode=Require;Trust Server Certificate=true;";
                Console.WriteLine($"Using default format: {MaskPassword(connectionString)}");
            }
            
            try
            {
                Console.WriteLine("\n🔍 Testing connection...");
                
                using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();
                
                Console.WriteLine("✅ Connection successful!");
                
                // Test a simple query
                using var command = new NpgsqlCommand("SELECT version();", connection);
                var version = await command.ExecuteScalarAsync();
                
                Console.WriteLine($"📊 PostgreSQL Version: {version}");
                
                // Test pg_trgm extension
                using var extCommand = new NpgsqlCommand("SELECT 1 FROM pg_extension WHERE extname = 'pg_trgm';", connection);
                var hasTrgm = await extCommand.ExecuteScalarAsync();
                
                if (hasTrgm != null)
                {
                    Console.WriteLine("✅ pg_trgm extension is available");
                }
                else
                {
                    Console.WriteLine("⚠️ pg_trgm extension not found - may need to be enabled");
                }
                
                // List databases
                using var dbCommand = new NpgsqlCommand("SELECT datname FROM pg_database WHERE datistemplate = false;", connection);
                using var reader = await dbCommand.ExecuteReaderAsync();
                
                Console.WriteLine("\n📋 Available databases:");
                while (await reader.ReadAsync())
                {
                    Console.WriteLine($"  - {reader.GetString(0)}");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Connection failed: {ex.Message}");
                Console.WriteLine("\n🔧 Common fixes:");
                Console.WriteLine("  1. Check firewall rules in Azure Portal");
                Console.WriteLine("  2. Verify connection string format");
                Console.WriteLine("  3. Ensure SSL Mode=Require for Azure");
                Console.WriteLine("  4. Check username/password");
                
                if (ex.Message.Contains("SSL"))
                {
                    Console.WriteLine("\n💡 Try adding: Trust Server Certificate=true");
                }
                
                if (ex.Message.Contains("authentication"))
                {
                    Console.WriteLine("\n💡 Check your username and password");
                }
                
                if (ex.Message.Contains("timeout") || ex.Message.Contains("network"))
                {
                    Console.WriteLine("\n💡 Check firewall rules in Azure Portal");
                }
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        private static string MaskPassword(string connectionString)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"Password\s*=\s*[^;]+",
                "Password=***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
        }
    }
}