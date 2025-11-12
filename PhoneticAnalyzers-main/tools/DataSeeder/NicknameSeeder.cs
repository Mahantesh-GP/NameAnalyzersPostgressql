using Microsoft.Extensions.Logging;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.ValueObjects;
using PhoneticAnalyzers.Infrastructure.Persistence;

namespace PhoneticAnalyzers.Tools;

/// <summary>
/// Seeds the database with common nickname mappings
/// </summary>
public static class NicknameSeeder
{
    /// <summary>
    /// Seeds the NicknameMaps table with common nickname mappings
    /// </summary>
    public static async Task SeedNicknamesAsync(PhoneticAnalyzersDbContext context, ILogger logger)
    {
        logger.LogInformation("Starting nickname seeding process...");

        // Check if nicknames already exist
        var existingCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(context.NicknameMaps);
        
        if (existingCount > 0)
        {
            logger.LogInformation("NicknameMaps table already contains {Count} mappings. Skipping seed.", existingCount);
            return;
        }

        var nicknameMappings = GetCommonNicknameMappings();
        logger.LogInformation("Seeding {Count} nickname mappings...", nicknameMappings.Count);

        await context.NicknameMaps.AddRangeAsync(nicknameMappings);
        var savedCount = await context.SaveChangesAsync();

        logger.LogInformation("Successfully seeded {Count} nickname mappings", savedCount);
    }

    /// <summary>
    /// Gets common nickname mappings
    /// </summary>
    private static List<NicknameMap> GetCommonNicknameMappings()
    {
        var mappings = new List<NicknameMap>();
        var locale = Locale.Create("en-US");
        var confidence = 0.95m; // High confidence for well-known nicknames

        // Define nickname data as: (Canonical Name, Nicknames)
        var nicknameData = new Dictionary<string, string[]>
        {
            { "Robert", new[] { "Bob", "Rob", "Bobby", "Robbie", "Bert" } },
            { "William", new[] { "Will", "Bill", "Billy", "Willy", "Liam" } },
            { "Richard", new[] { "Rick", "Dick", "Rich", "Ricky", "Richie" } },
            { "Michael", new[] { "Mike", "Mick", "Mickey", "Mikey" } },
            { "James", new[] { "Jim", "Jimmy", "Jamie", "Jimbo" } },
            { "John", new[] { "Johnny", "Jack", "Jon" } },
            { "David", new[] { "Dave", "Davy", "Davey" } },
            { "Joseph", new[] { "Joe", "Joey", "Jo" } },
            { "Thomas", new[] { "Tom", "Tommy", "Thom" } },
            { "Charles", new[] { "Charlie", "Chuck", "Chas", "Chaz" } },
            { "Christopher", new[] { "Chris", "Topher", "Kit", "Kris" } },
            { "Daniel", new[] { "Dan", "Danny", "Dani" } },
            { "Matthew", new[] { "Matt", "Matty" } },
            { "Anthony", new[] { "Tony", "Ant" } },
            { "Donald", new[] { "Don", "Donnie", "Donny" } },
            { "Kenneth", new[] { "Ken", "Kenny", "Kennie" } },
            { "Steven", new[] { "Steve", "Stevie" } },
            { "Stephen", new[] { "Steve", "Stevie" } },
            { "Andrew", new[] { "Andy", "Drew" } },
            { "Edward", new[] { "Ed", "Eddie", "Eddy", "Ted", "Teddy", "Ned" } },
            { "Joshua", new[] { "Josh" } },
            { "George", new[] { "Georgie" } },
            { "Kevin", new[] { "Kev" } },
            { "Timothy", new[] { "Tim", "Timmy" } },
            { "Lawrence", new[] { "Larry", "Lars", "Laurie" } },
            { "Raymond", new[] { "Ray" } },
            { "Patrick", new[] { "Pat", "Patty", "Rick" } },
            { "Benjamin", new[] { "Ben", "Benny", "Benji" } },
            { "Nicholas", new[] { "Nick", "Nicky", "Nico" } },
            { "Samuel", new[] { "Sam", "Sammy" } },
            { "Gregory", new[] { "Greg", "Gregg" } },
            { "Alexander", new[] { "Alex", "Xander", "Alec", "Lex" } },
            { "Jonathan", new[] { "Jon", "Johnny", "Nathan" } },
            { "Ronald", new[] { "Ron", "Ronnie", "Ronny" } },
            { "Frederick", new[] { "Fred", "Freddy", "Freddie", "Fritz" } },
            { "Jeremy", new[] { "Jerry", "Jem" } },
            { "Gerald", new[] { "Jerry", "Gerry" } },
            { "Eugene", new[] { "Gene" } },
            { "Albert", new[] { "Al", "Bert", "Bertie" } },
            { "Henry", new[] { "Hank", "Harry", "Hal" } },
            { "Douglas", new[] { "Doug", "Dougie" } },
            { "Peter", new[] { "Pete", "Petey" } },
            
            // Female names
            { "Elizabeth", new[] { "Liz", "Beth", "Betty", "Lizzie", "Betsy", "Eliza", "Lisa" } },
            { "Margaret", new[] { "Maggie", "Meg", "Peggy", "Marge", "Margo", "Margie", "Daisy" } },
            { "Catherine", new[] { "Cathy", "Kate", "Katie", "Kathy", "Cat", "Kay" } },
            { "Katherine", new[] { "Kate", "Katie", "Kathy", "Kathryn", "Cat", "Kay" } },
            { "Jennifer", new[] { "Jen", "Jenny", "Jennie" } },
            { "Susan", new[] { "Sue", "Susie", "Suzy", "Suzie" } },
            { "Jessica", new[] { "Jess", "Jessie" } },
            { "Sarah", new[] { "Sally", "Sara" } },
            { "Nancy", new[] { "Nan", "Nanny" } },
            { "Patricia", new[] { "Pat", "Patty", "Patsy", "Trish", "Tricia" } },
            { "Linda", new[] { "Lindy", "Lynn" } },
            { "Barbara", new[] { "Barb", "Barbie", "Babs" } },
            { "Dorothy", new[] { "Dot", "Dottie", "Dolly" } },
            { "Helen", new[] { "Nell", "Nellie" } },
            { "Sandra", new[] { "Sandy", "Sandi" } },
            { "Deborah", new[] { "Deb", "Debbie", "Debby" } },
            { "Rebecca", new[] { "Becky", "Becca", "Bex" } },
            { "Kimberly", new[] { "Kim", "Kimmy" } },
            { "Michelle", new[] { "Shelly", "Shelley", "Micky" } },
            { "Amanda", new[] { "Mandy", "Manda" } },
            { "Stephanie", new[] { "Steph", "Steffi", "Stevie" } },
            { "Nicole", new[] { "Nikki", "Nicky", "Nic" } },
            { "Melissa", new[] { "Missy", "Mel", "Lissa" } },
            { "Christine", new[] { "Chris", "Chrissy", "Tina", "Christie" } },
            { "Christina", new[] { "Chris", "Chrissy", "Tina", "Christie" } },
            { "Rachel", new[] { "Rae", "Ray" } },
            { "Samantha", new[] { "Sam", "Sammy" } },
            { "Victoria", new[] { "Vicky", "Vicki", "Tori", "Vic" } },
            { "Abigail", new[] { "Abby", "Gail", "Abbie" } },
            { "Emily", new[] { "Em", "Emmy", "Emmie" } },
            { "Danielle", new[] { "Dani", "Danny" } },
            { "Virginia", new[] { "Ginny", "Ginger", "Virgie" } }
        };

        // Create bidirectional mappings
        foreach (var (canonical, nicknames) in nicknameData)
        {
            foreach (var nickname in nicknames)
            {
                var nicknameMap = NicknameMap.Create(
                    canonicalName: canonical,
                    nickname: nickname,
                    locale: locale,
                    isBidirectional: true,
                    confidence: confidence
                );

                mappings.Add(nicknameMap);
            }
        }

        return mappings;
    }
}
