using Microsoft.Extensions.Logging;

namespace PhoneticAnalyzers.WebUI.Services;

/// <summary>
/// State management service for search results and filters
/// </summary>
public class SearchStateService
{
    private readonly ILogger<SearchStateService> _logger;

    public event Action? OnChange;

    public bool IsLoading { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? SuccessMessage { get; private set; }

    public SearchStateService(ILogger<SearchStateService> _logger)
    {
        this._logger = _logger;
    }

    public void SetLoading(bool isLoading)
    {
        IsLoading = isLoading;
        NotifyStateChanged();
    }

    public void SetError(string? message)
    {
        ErrorMessage = message;
        SuccessMessage = null;
        NotifyStateChanged();
    }

    public void SetSuccess(string? message)
    {
        SuccessMessage = message;
        ErrorMessage = null;
        NotifyStateChanged();
    }

    public void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
