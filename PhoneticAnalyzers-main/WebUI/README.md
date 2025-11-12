# 🎨 Phonetic Analyzers - Web UI

Modern Blazor WebAssembly application for the Phonetic Analyzers platform with professional UI components and best practices.

## ✨ Features

- **🎯 Advanced Search**: Multi-criteria name search with phonetic matching
- **📁 Bulk Upload**: Process multiple names at once (up to 20)
- **🎨 Modern UI**: Built with MudBlazor component library
- **📱 Responsive**: Works on desktop, tablet, and mobile
- **🌙 Dark Mode**: Toggle between light and dark themes
- **📊 Export**: Download search results as CSV
- **⚡ Fast**: Optimized with caching and lazy loading
- **♿ Accessible**: WCAG compliant components

## 🏗️ Architecture

```
WebUI/
├── Components/              # Reusable UI components
│   ├── Search/             # Search-related components
│   │   ├── SearchForm.razor
│   │   └── SearchResults.razor
│   ├── Bulk/               # Bulk upload components
│   │   ├── BulkUploadForm.razor
│   │   └── BulkResults.razor
│   └── Shared/             # Shared components
│
├── Layout/                  # Application layout
│   └── MainLayout.razor    # Main responsive layout
│
├── Models/                  # Data models
│   └── ApiModels.cs        # API request/response models
│
├── Pages/                   # Application pages
│   ├── Home.razor          # Landing page
│   ├── Search.razor        # Advanced search page
│   ├── Bulk.razor          # Bulk upload page
│   └── About.razor         # About page
│
├── Services/                # Business logic services
│   ├── SearchApiClient.cs  # API client
│   ├── CsvExportService.cs # CSV export utility
│   └── SearchStateService.cs # State management
│
└── wwwroot/                 # Static files
    ├── index.html          # Entry point
    ├── appsettings.json    # Configuration
    └── css/                # Custom styles
```

## 🚀 Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- Node.js (for development tooling)
- Azure Functions running locally or deployed

### Configuration

1. **Update API Base URL** in `wwwroot/appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:7072"
  }
}
```

For production, update to your deployed Azure Functions URL.

### Run Locally

```powershell
# Navigate to WebUI folder
cd WebUI

# Restore dependencies
dotnet restore

# Run the application
dotnet run

# Or use watch for hot reload during development
dotnet watch run
```

The application will be available at `http://localhost:5000` (or the port specified in console output).

## 🎨 UI Components

### SearchForm Component

Reusable search form with validation:

```razor
<SearchForm Model="_searchRequest" 
            Counties="_counties" 
            IsLoading="_isLoading" 
            OnSubmit="PerformSearch" />
```

**Features:**
- Name input with validation
- Max results slider
- Min similarity threshold
- County filter dropdown
- Record type selection
- Trigram/Nickname options

### SearchResults Component

Display search results with rich formatting:

```razor
<SearchResults Response="_searchResponse" />
```

**Features:**
- Results table with sorting
- Similarity score visualization
- Phonetic codes expansion panel
- CSV export button
- Match type chips
- County badges

### BulkUploadForm Component

Multi-name input form:

```razor
<BulkUploadForm Model="_bulkRequest" 
                IsLoading="_isLoading" 
                OnSubmit="PerformBulkSearch" />
```

**Features:**
- Multi-line text input
- Name count indicator
- Configurable max results per search
- Similarity threshold control

### BulkResults Component

Expandable results by search term:

```razor
<BulkResults Response="_bulkResponse" />
```

**Features:**
- Expandable panels per search term
- Match count badges
- Error handling display
- Export all results to CSV

## 🔧 Services

### SearchApiClient

HTTP client for API communication:

```csharp
public interface ISearchApiClient
{
    Task<AdvancedSearchResponse?> AdvancedSearchAsync(AdvancedSearchRequest request);
    Task<BulkSearchResponse?> BulkSearchAsync(BulkSearchRequest request);
    Task<List<CountyInfo>> GetCountiesAsync();
    Task<bool> HealthCheckAsync();
}
```

### CsvExportService

Generate and download CSV files:

```csharp
public interface ICsvExportService
{
    string GenerateCsv<T>(IEnumerable<T> data, Func<T, string[]> rowSelector, string[] headers);
    byte[] GenerateCsvBytes<T>(IEnumerable<T> data, Func<T, string[]> rowSelector, string[] headers);
}
```

### SearchStateService

Application-wide state management:

```csharp
public class SearchStateService
{
    public bool IsLoading { get; }
    public string? ErrorMessage { get; }
    public string? SuccessMessage { get; }
    
    public void SetLoading(bool isLoading);
    public void SetError(string? message);
    public void SetSuccess(string? message);
}
```

## 🎨 Theming

The application uses MudBlazor's theming system with custom colors:

**Light Theme:**
- Primary: #1976d2 (Blue)
- Secondary: #dc004e (Pink)
- Success: #4caf50 (Green)
- Info: #2196f3 (Light Blue)
- Warning: #ff9800 (Orange)
- Error: #f44336 (Red)

**Dark Theme:**
- Primary: #90caf9 (Light Blue)
- Secondary: #f48fb1 (Light Pink)
- Background: #121212 (Dark Gray)
- Surface: #1e1e1e (Medium Dark)

Toggle theme with the sun/moon icon in the app bar.

## 📦 Dependencies

- **MudBlazor** (^7.8.0): Material Design component library
- **.NET 8.0**: Latest .NET framework
- **System.Net.Http.Json**: JSON serialization for HTTP
- **Microsoft.JSInterop**: JavaScript interop for file downloads

## 🌐 API Integration

The WebUI communicates with Azure Functions backend:

**Endpoints:**
- `POST /api/search/advanced` - Advanced search
- `POST /api/search/bulk` - Bulk search
- `GET /api/counties` - Get counties list
- `GET /api/search/health` - Health check

**Configuration:**
Set the API base URL in `appsettings.json` or via environment variable:

```bash
export ApiSettings__BaseUrl="https://your-function-app.azurewebsites.net"
```

## 🚀 Deployment

### Azure Static Web Apps

```bash
# Build for production
dotnet publish -c Release

# Deploy to Azure Static Web Apps
az staticwebapp create \
  --name phonetic-analyzers-ui \
  --resource-group rg-phoneticanalyzers \
  --source ./bin/Release/net8.0/publish/wwwroot
```

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WebUI/PhoneticAnalyzers.WebUI.csproj", "WebUI/"]
RUN dotnet restore "WebUI/PhoneticAnalyzers.WebUI.csproj"
COPY . .
WORKDIR "/src/WebUI"
RUN dotnet publish -c Release -o /app/publish

FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
```

## 🧪 Testing

### Run Unit Tests

```powershell
dotnet test
```

### Browser Testing

Recommended browsers:
- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)

## 📝 Best Practices Implemented

✅ **Component Reusability**: Modular components for maintainability  
✅ **Dependency Injection**: All services registered via DI  
✅ **Error Handling**: Comprehensive try-catch with user-friendly messages  
✅ **Loading States**: Visual feedback during async operations  
✅ **Responsive Design**: Mobile-first approach with MudBlazor Grid  
✅ **Accessibility**: WCAG 2.1 AA compliant  
✅ **Performance**: Lazy loading, caching, and optimized rendering  
✅ **Logging**: Structured logging via ILogger  
✅ **Type Safety**: Strong typing throughout application  
✅ **Clean Code**: SOLID principles and clean architecture

## 🔍 Troubleshooting

### API Connection Issues

If you see "Network error" messages:

1. Check API is running: `http://localhost:7072/api/search/health`
2. Verify `appsettings.json` has correct BaseUrl
3. Check browser console for CORS errors
4. Ensure Functions are configured with CORS policy

### Build Errors

```powershell
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Runtime Errors

Check browser console (F12) for JavaScript errors and check application logs for backend errors.

## 📚 Additional Resources

- [MudBlazor Documentation](https://mudblazor.com/)
- [Blazor WebAssembly Documentation](https://docs.microsoft.com/aspnet/core/blazor/)
- [Main Project README](../README.md)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add/update tests
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

---

**Built with ❤️ using Blazor WebAssembly and MudBlazor**
