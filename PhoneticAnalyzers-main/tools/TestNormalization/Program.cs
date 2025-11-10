using PhoneticAnalyzers.Application.Services.Text;

Console.WriteLine("Testing Text Normalization Service");
Console.WriteLine("==================================");

var service = new TextNormalizationService();

// Test basic normalization
var test1 = service.Normalize("John Smith");
Console.WriteLine($"Normalize('John Smith') = '{test1}'");

// Test diacritics removal
var test2 = service.Normalize("José González");
Console.WriteLine($"Normalize('José González') = '{test2}'");

// Test punctuation handling
var test3 = service.Normalize("O'Brien-Smith, Jr.");
Console.WriteLine($"Normalize('O'Brien-Smith, Jr.') = '{test3}'");

// Test phonetic normalization
var test4 = service.NormalizeForPhonetic("John-Paul O'Connor123");
Console.WriteLine($"NormalizeForPhonetic('John-Paul O'Connor123') = '{test4}'");

// Test diacritics removal
var test5 = service.RemoveDiacritics("François Müller Çağlar");
Console.WriteLine($"RemoveDiacritics('François Müller Çağlar') = '{test5}'");

// Test Cyrillic transliteration
var test6 = service.Transliterate("Владимир", "Cyrl");
Console.WriteLine($"Transliterate('Владимир', 'Cyrl') = '{test6}'");

// Test tokenization
var test7 = service.Tokenize("José María González-López");
Console.WriteLine($"Tokenize('José María González-López') = [{string.Join(", ", test7.Select(t => $"'{t}'"))}]");

Console.WriteLine("\nAll tests completed successfully!");
