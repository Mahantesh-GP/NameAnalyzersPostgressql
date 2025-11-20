using System;
using Npgsql;
using System.IO;

var connectionString = "Host=localhost;Port=5432;Database=phonetic_native;Username=postgres;Password=postgres";
var sqlFile = "..\\sql\\05_search.sql";

try
{
    var sql = File.ReadAllText(sqlFile);
    
    using var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    
    Console.WriteLine("Reloading search function...");
    
    using var cmd = new NpgsqlCommand(sql, conn);
    cmd.ExecuteNonQuery();
    
    Console.WriteLine("✓ Search function reloaded successfully!");
    
    // Test the function
    Console.WriteLine("\nTesting search for 'Bill':");
    using var testCmd = new NpgsqlCommand(@"
        SELECT full_name, match_type, similarity_score 
        FROM search_persons('Bill', 10, 0.3, NULL, NULL, TRUE, TRUE)
        LIMIT 5", conn);
    using var reader = testCmd.ExecuteReader();
    
    while (reader.Read())
    {
        Console.WriteLine($"  {reader.GetString(0),-25} | {reader.GetString(1),-20} | {reader.GetDouble(2):P0}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    if (ex.InnerException != null)
        Console.WriteLine($"Inner: {ex.InnerException.Message}");
}
