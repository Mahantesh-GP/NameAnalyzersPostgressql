using System;
using System.IO;
using Npgsql;

namespace TestDataLoader
{
    class Program
    {
        static void Main(string[] args)
        {
            var connectionString = "Host=localhost;Port=5432;Database=phonetic_native;Username=postgres;Password=postgres";

            // CHECK MODE - if argument provided
            if (args.Length > 0 && args[0] == "check")
            {
                CheckData(connectionString);
                return;
            }

            // RELOAD MODE - reload search function
            if (args.Length > 0 && args[0] == "reload")
            {
                ReloadSearchFunction(connectionString);
                return;
            }
            
            var records = new[]
            {
                // Exact matches (20 records)
                ("John Smith", "KING", "King County", "I"),
                ("Jane Doe", "PIER", "Pierce County", "I"),
                ("Robert Johnson", "SNOH", "Snohomish County", "B"),
                ("Mary Williams", "KING", "King County", "I"),
                ("David Brown", "PIER", "Pierce County", "I"),
                ("Jennifer Davis", "SNOH", "Snohomish County", "I"),
                ("Michael Wilson", "KING", "King County", "B"),
                ("Linda Moore", "PIER", "Pierce County", "I"),
                ("James Taylor", "SNOH", "Snohomish County", "I"),
                ("Patricia Anderson", "KING", "King County", "I"),
                ("Charles Thomas", "PIER", "Pierce County", "B"),
                ("Barbara Jackson", "SNOH", "Snohomish County", "I"),
                ("Joseph White", "KING", "King County", "I"),
                ("Susan Harris", "PIER", "Pierce County", "I"),
                ("Thomas Martin", "SNOH", "Snohomish County", "B"),
                ("Sarah Thompson", "KING", "King County", "I"),
                ("Daniel Garcia", "PIER", "Pierce County", "I"),
                ("Nancy Martinez", "SNOH", "Snohomish County", "I"),
                ("Matthew Robinson", "KING", "King County", "B"),
                ("Lisa Clark", "PIER", "Pierce County", "I"),
                
                // Nickname matches (20 records)
                ("William Anderson", "KING", "King County", "I"),
                ("Robert Williams", "PIER", "Pierce County", "I"),
                ("James Wilson", "SNOH", "Snohomish County", "I"),
                ("Michael Brown", "KING", "King County", "I"),
                ("Richard Davis", "PIER", "Pierce County", "B"),
                ("Elizabeth Miller", "SNOH", "Snohomish County", "I"),
                ("Margaret Jones", "KING", "King County", "I"),
                ("Christopher Garcia", "PIER", "Pierce County", "I"),
                ("William Thompson", "SNOH", "Snohomish County", "B"),
                ("Robert Martinez", "KING", "King County", "I"),
                ("James Rodriguez", "PIER", "Pierce County", "I"),
                ("Michael Lee", "SNOH", "Snohomish County", "I"),
                ("Richard Walker", "KING", "King County", "B"),
                ("Elizabeth Hall", "PIER", "Pierce County", "I"),
                ("Margaret Allen", "SNOH", "Snohomish County", "I"),
                ("Christopher Young", "KING", "King County", "I"),
                ("William King", "PIER", "Pierce County", "B"),
                ("Robert Wright", "SNOH", "Snohomish County", "I"),
                ("James Lopez", "KING", "King County", "I"),
                ("Michael Hill", "PIER", "Pierce County", "I"),
                
                // Phonetic matches (25 records with more variety)
                ("Jon Smyth", "KING", "King County", "I"),  // John Smith variant
                ("John Smythe", "PIER", "Pierce County", "I"),  // John Smith variant
                ("Jhon Smith", "SNOH", "Snohomish County", "I"),  // John Smith variant
                ("Jayne Dough", "KING", "King County", "I"),  // Jane Doe variant
                ("Jane Dowe", "PIER", "Pierce County", "I"),  // Jane Doe variant
                ("Kathrine Peterson", "SNOH", "Snohomish County", "I"),  // Catherine variant
                ("Katherine Smith", "KING", "King County", "I"),  // Catherine variant
                ("Kathryn Walker", "PIER", "Pierce County", "B"),  // Catherine variant
                ("Catherine Williams", "SNOH", "Snohomish County", "I"),  // Base name
                ("Steven Thompson", "KING", "King County", "B"),  // Stephen variant
                ("Stephen Johnson", "PIER", "Pierce County", "I"),  // Base name
                ("Stefan Campbell", "SNOH", "Snohomish County", "B"),  // Stephen variant
                ("Phillip Martinez", "KING", "King County", "I"),  // Philip variant
                ("Philip Anderson", "PIER", "Pierce County", "I"),  // Base name
                ("Filip Turner", "SNOH", "Snohomish County", "I"),  // Philip variant
                ("Kristopher White", "KING", "King County", "I"),  // Christopher variant
                ("Christopher Bennett", "PIER", "Pierce County", "I"),  // Base name
                ("Kristoffer Edwards", "SNOH", "Snohomish County", "I"),  // Christopher variant
                ("Geoffrey Harris", "KING", "King County", "I"),  // Jeffrey variant
                ("Jeffrey Brown", "PIER", "Pierce County", "I"),  // Base name
                ("Jeffery Perez", "SNOH", "Snohomish County", "I"),  // Jeffrey variant
                ("Alison Clark", "KING", "King County", "I"),  // Allison variant
                ("Allison Taylor", "PIER", "Pierce County", "I"),  // Base name
                ("Allisson Parker", "SNOH", "Snohomish County", "I"),  // Allison variant
                ("Kristine Robinson", "KING", "King County", "I"),  // Christine variant
                
                // Fuzzy matches (20 records)
                ("John Smithe", "KING", "King County", "I"),
                ("Jane Deo", "PIER", "Pierce County", "I"),
                ("Robrt Johnson", "SNOH", "Snohomish County", "B"),
                ("Wiliam Anderson", "KING", "King County", "I"),
                ("Elizabet Miller", "PIER", "Pierce County", "I"),
                ("Margret Jones", "SNOH", "Snohomish County", "I"),
                ("Christophr Garcia", "KING", "King County", "I"),
                ("Michal Brown", "PIER", "Pierce County", "I"),
                ("Jhn Smith", "SNOH", "Snohomish County", "B"),
                ("Jame Doe", "KING", "King County", "I"),
                ("Robet Williams", "PIER", "Pierce County", "I"),
                ("Willliam Thompson", "SNOH", "Snohomish County", "I"),
                ("Elizabth Davis", "KING", "King County", "B"),
                ("Margarett Wilson", "PIER", "Pierce County", "I"),
                ("Christofer Martinez", "SNOH", "Snohomish County", "I"),
                ("Micheal Rodriguez", "KING", "King County", "I"),
                ("Richrd Lee", "PIER", "Pierce County", "B"),
                ("Patrica Walker", "SNOH", "Snohomish County", "I"),
                ("Barbra Hall", "KING", "King County", "I"),
                ("Danniel Allen", "PIER", "Pierce County", "I")
            };

            try
            {
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();
                Console.WriteLine("Connected to database!");
                Console.WriteLine($"Loading {records.Length} test records...\n");

                int count = 0;
                foreach (var (name, countyCode, countyName, type) in records)
                {
                    // ingest_person(p_external_id, p_full_name, p_county, p_flag)
                    var externalId = $"TEST-{Guid.NewGuid()}";
                    using var cmd = new NpgsqlCommand("SELECT ingest_person(@external_id, @name, @county, @type)", conn);
                    cmd.Parameters.AddWithValue("external_id", externalId);
                    cmd.Parameters.AddWithValue("name", name);
                    cmd.Parameters.AddWithValue("county", countyCode);
                    cmd.Parameters.AddWithValue("type", type);
                    
                    cmd.ExecuteNonQuery();
                    count++;
                    
                    if (count % 10 == 0)
                        Console.WriteLine($"Inserted {count}/{records.Length} records...");
                }

                Console.WriteLine($"\n✓ Successfully inserted {count} test records!");
                Console.WriteLine("\nTest these searches in your UI:");
                Console.WriteLine("  - 'Bill' → should show William entries in Nickname column");
                Console.WriteLine("  - 'John Smith' → should show Exact + Phonetic + Fuzzy matches");
                Console.WriteLine("  - 'Elizabeth' → should show Exact + Nickname (Liz) + Fuzzy variants");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void CheckData(string connectionString)
        {
            try
            {
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();
                Console.WriteLine("Connected to database!\n");

                // Check total count
                using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM person", conn))
                {
                    Console.WriteLine($"Total persons: {cmd.ExecuteScalar()}\n");
                }

                // Check for John Smith specifically
                using (var cmd = new NpgsqlCommand("SELECT person_id, full_name, county FROM person WHERE LOWER(full_name) LIKE '%john%smith%' ORDER BY person_id", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("All 'John Smith' records:");
                    var count = 0;
                    while (reader.Read())
                    {
                        count++;
                        Console.WriteLine($"  {count}. ID={reader.GetInt32(0)}, Name={reader.GetString(1)}, County={reader.GetString(2)}");
                    }
                    if (count == 0) Console.WriteLine("  ❌ NONE FOUND!");
                    Console.WriteLine();
                }

                // Check recent insertions
                using (var cmd = new NpgsqlCommand("SELECT person_id, full_name, county FROM person ORDER BY person_id DESC LIMIT 20", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    Console.WriteLine("Last 20 inserted records:");
                    while (reader.Read())
                    {
                        Console.WriteLine($"  ID={reader.GetInt32(0)}, Name={reader.GetString(1)}, County={reader.GetString(2)}");
                    }
                }

                // Debug search function
                Console.WriteLine("\n\nTesting search_persons('john smith'):");
                using (var cmd = new NpgsqlCommand(@"
                    SELECT full_name, match_type, similarity_score 
                    FROM search_persons('john smith', 50, 0.01, NULL, NULL, TRUE, TRUE)
                    LIMIT 20", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    var count = 0;
                    while (reader.Read())
                    {
                        count++;
                        Console.WriteLine($"  {count}. {reader.GetString(0),-25} | {reader.GetString(1),-20} | {reader.GetDouble(2):P0}");
                    }
                    if (count == 0) Console.WriteLine("  NO RESULTS!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void ReloadSearchFunction(string connectionString)
        {
            try
            {
                using var conn = new NpgsqlConnection(connectionString);
                conn.Open();
                
                Console.WriteLine("Reloading search_persons function...");
                
                var sqlPath = "05_search.sql";
                var sql = File.ReadAllText(sqlPath);
                
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                
                Console.WriteLine("✓ Function reloaded successfully!\n");
                
                // Test the function
                Console.WriteLine("Testing search for 'Bill':");
                using var testCmd = new NpgsqlCommand(@"
                    SELECT full_name, match_type, similarity_score 
                    FROM search_persons('Bill', 10, 0.3, NULL, NULL, TRUE, TRUE)
                    LIMIT 10", conn);
                using var reader = testCmd.ExecuteReader();
                
                var count = 0;
                while (reader.Read())
                {
                    count++;
                    Console.WriteLine($"  {count}. {reader.GetString(0),-25} | {reader.GetString(1),-20} | {reader.GetDouble(2):P0}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
