using System.Text;

namespace PhoneticAnalyzers.WebUI.Services;

/// <summary>
/// Utility service for CSV export functionality
/// </summary>
public interface ICsvExportService
{
    string GenerateCsv<T>(IEnumerable<T> data, Func<T, string[]> rowSelector, string[] headers);
    byte[] GenerateCsvBytes<T>(IEnumerable<T> data, Func<T, string[]> rowSelector, string[] headers);
}

public class CsvExportService : ICsvExportService
{
    public string GenerateCsv<T>(IEnumerable<T> data, Func<T, string[]> rowSelector, string[] headers)
    {
        var sb = new StringBuilder();
        
        // Add headers
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));
        
        // Add data rows
        foreach (var item in data)
        {
            var row = rowSelector(item);
            sb.AppendLine(string.Join(",", row.Select(EscapeCsvField)));
        }
        
        return sb.ToString();
    }

    public byte[] GenerateCsvBytes<T>(IEnumerable<T> data, Func<T, string[]> rowSelector, string[] headers)
    {
        var csv = GenerateCsv(data, rowSelector, headers);
        return Encoding.UTF8.GetBytes(csv);
    }

    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        // Escape if contains comma, quote, or newline
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}
