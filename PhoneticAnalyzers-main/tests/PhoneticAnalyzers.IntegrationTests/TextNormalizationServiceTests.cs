using PhoneticAnalyzers.Application.Services.Text;
using Xunit;

namespace PhoneticAnalyzers.IntegrationTests;

/// <summary>
/// Integration tests for text normalization services
/// </summary>
public class TextNormalizationServiceTests
{
    private readonly TextNormalizationService _service;

    public TextNormalizationServiceTests()
    {
        _service = new TextNormalizationService();
    }

    [Fact]
    public void Normalize_ShouldHandleBasicText()
    {
        // Arrange
        var text = "John Smith";

        // Act
        var result = _service.Normalize(text);

        // Assert
        Assert.Equal("john smith", result);
    }

    [Fact]
    public void Normalize_ShouldRemoveDiacritics()
    {
        // Arrange
        var text = "José González";

        // Act
        var result = _service.Normalize(text);

        // Assert
        Assert.Equal("jose gonzalez", result);
    }

    [Fact]
    public void Normalize_ShouldHandlePunctuation()
    {
        // Arrange
        var text = "O'Brien-Smith, Jr.";

        // Act
        var result = _service.Normalize(text);

        // Assert
        Assert.Equal("o brien smith jr", result);
    }

    [Fact]
    public void NormalizeForPhonetic_ShouldRemoveNonAlphabetic()
    {
        // Arrange
        var text = "John-Paul O'Connor123";

        // Act
        var result = _service.NormalizeForPhonetic(text);

        // Assert
        Assert.Equal("johnpauloconnor", result);
    }

    [Fact]
    public void RemoveDiacritics_ShouldHandleVariousAccents()
    {
        // Arrange
        var text = "François Müller Çağlar";

        // Act
        var result = _service.RemoveDiacritics(text);

        // Assert
        Assert.Equal("Francois Muller Caglar", result);
    }

    [Fact]
    public void Transliterate_ShouldHandleCyrillic()
    {
        // Arrange
        var text = "Владимир";

        // Act
        var result = _service.Transliterate(text, "Cyrl");

        // Assert
        Assert.Equal("Vladimir", result);
    }

    [Fact]
    public void Tokenize_ShouldSplitAndNormalizeText()
    {
        // Arrange
        var text = "José María  González-López";

        // Act
        var result = _service.Tokenize(text);

        // Assert
        Assert.Equal(new[] { "jose", "maria", "gonzalez", "lopez" }, result);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Normalize_ShouldHandleEmptyOrNullInput(string input, string expected)
    {
        // Act
        var result = _service.Normalize(input);

        // Assert
        Assert.Equal(expected, result);
    }
}