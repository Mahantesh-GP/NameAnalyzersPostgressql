# PhoneticAnalyzers Blazor Web UI

A modern, responsive Blazor Server application providing a comprehensive web interface for the PhoneticAnalyzers phonetic name matching system. Built with .NET 8, Bootstrap 5, and modern web development practices.

## 🚀 Features

### Dashboard
- **System Health Monitoring**: Real-time API status with visual indicators
- **Quick Statistics**: Person count, recent searches, system metrics
- **Quick Actions**: Fast access to key features
- **Recent Activity**: Display of latest search results and operations

### Advanced Search
- **Phonetic Name Search**: Real-time search with similarity scoring
- **Multiple Algorithm Support**: Soundex, Metaphone, Double Metaphone, NYSIIS, Match Rating Approach
- **Live Results**: Instant search results as you type
- **Phonetic Codes Display**: Visual representation of generated phonetic codes
- **Similarity Scoring**: Detailed matching scores and rankings

### Person Management
- **Individual Person Entry**: Clean form for adding single persons
- **Real-time Validation**: Client-side form validation with feedback
- **Phonetic Code Preview**: Live generation of phonetic codes during entry
- **Sample Data Integration**: Quick-add buttons for testing
- **Success Feedback**: Comprehensive confirmation with generated codes

### Batch Operations
- **Bulk Import**: Support for CSV and JSON file uploads
- **Manual Batch Entry**: Textarea for manual data input
- **Progress Tracking**: Real-time progress indicators for large operations
- **Error Handling**: Detailed error reporting and validation
- **Results Summary**: Comprehensive import results with statistics

### Modern UI/UX
- **Bootstrap 5**: Modern, responsive design system
- **Bootstrap Icons**: Comprehensive icon library
- **Custom Styling**: Modern gradients, animations, and visual effects
- **Responsive Design**: Mobile-first, works on all devices
- **Dark Mode Ready**: CSS variables for easy theming
- **Accessibility**: ARIA labels and semantic HTML

## 🏗️ Architecture

### Technology Stack
- **.NET 8**: Latest .NET framework with performance improvements
- **ASP.NET Core Blazor Server**: Server-side rendering with SignalR
- **Bootstrap 5**: Modern CSS framework with utility classes
- **C# 12**: Latest C# language features
- **HttpClient**: Modern HTTP client with dependency injection
- **Newtonsoft.Json**: Robust JSON serialization

### Project Structure
```
PhoneticAnalyzers.Web/
├── Components/
│   ├── Layout/           # Layout components (MainLayout, NavMenu)
│   ├── Pages/            # Page components (Home, Search, etc.)
│   └── Shared/           # Reusable components (StatusIndicator)
├── Services/             # API service layer
├── wwwroot/             # Static assets (CSS, JS, images)
├── Properties/          # Launch settings
└── appsettings.json     # Configuration
```

### API Integration
- **PhoneticAnalyzersApiService**: Comprehensive API client
- **Dependency Injection**: Configured in Program.cs
- **Error Handling**: Robust error handling with user feedback
- **Health Checks**: Automatic API availability monitoring
- **Typed DTOs**: Strongly-typed data transfer objects

## 🛠️ Development Setup

### Prerequisites
- **.NET 8 SDK**: Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/)
- **Visual Studio Code**: With C# Dev Kit extension
- **PhoneticAnalyzers API**: Backend API service running

### Quick Start
1. **Clone and Navigate**:
   ```bash
   cd PhoneticAnalyzers/Web
   ```

2. **Configure API Endpoint**:
   Update `appsettings.json`:
   ```json
   {
     "ApiSettings": {
       "BaseAddress": "https://your-api-endpoint.azurewebsites.net/"
     }
   }
   ```

3. **Install Dependencies**:
   ```bash
   dotnet restore
   ```

4. **Build Project**:
   ```bash
   dotnet build
   ```

5. **Run Application**:
   ```bash
   dotnet run
   ```

6. **Open Browser**:
   Navigate to `https://localhost:5153` or `http://localhost:5154`

### Development Tasks
Use VS Code tasks (Ctrl+Shift+P → "Tasks: Run Task"):
- **Build Blazor Application**: Compile the project
- **Run Blazor Application**: Start the development server
- **Watch Blazor Application**: Auto-reload on file changes

### Configuration
- **API Settings**: Configure in `appsettings.json` or environment variables
- **Launch Settings**: Modify ports and environment in `Properties/launchSettings.json`
- **Logging**: Configure logging levels in `appsettings.json`

## 🎨 UI Components

### StatusIndicator
Real-time API health monitoring with visual feedback:
- Green pulse: API online and healthy
- Red pulse: API offline or unhealthy
- Spinner: Checking status

### Responsive Navigation
Bootstrap 5 navbar with:
- Brand logo and title
- Navigation links with active states
- Status indicator integration
- Mobile-responsive hamburger menu

### Form Components
Modern form controls with:
- Bootstrap styling
- Client-side validation
- Real-time feedback
- Error state handling
- Success confirmation

### Data Tables
Responsive tables featuring:
- Sortable columns
- Pagination support
- Mobile-responsive design
- Action buttons
- Status indicators

## 🔧 Customization

### Styling
Modify `wwwroot/app.css` for custom styling:
- CSS variables for theming
- Custom component styles
- Animation definitions
- Responsive breakpoints

### API Integration
Extend `PhoneticAnalyzersApiService.cs`:
- Add new API endpoints
- Implement additional DTOs
- Handle new response types
- Add caching strategies

### Components
Create new Blazor components:
- Follow existing patterns
- Use Bootstrap classes
- Implement proper disposal
- Add parameter validation

## 🚀 Deployment

### Azure App Service
Deploy alongside the PhoneticAnalyzers Functions API:

1. **Publish Profile**:
   ```bash
   dotnet publish -c Release
   ```

2. **Deploy to Azure**:
   - Use Azure CLI or Visual Studio
   - Configure app settings for production
   - Set up continuous deployment

3. **Environment Configuration**:
   ```json
   {
     "ApiSettings": {
       "BaseAddress": "https://your-production-api.azurewebsites.net/"
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information"
       }
     }
   }
   ```

### Docker Support
Create `Dockerfile` for containerization:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PhoneticAnalyzers.Web.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PhoneticAnalyzers.Web.dll"]
```

## 🧪 Testing

### Manual Testing
1. **Navigation**: Test all page transitions
2. **Search**: Verify phonetic search functionality
3. **Forms**: Test validation and submission
4. **Responsive**: Check mobile and desktop layouts
5. **API Integration**: Verify all API calls work

### Performance
- **Blazor Server**: Optimized for server-side rendering
- **SignalR**: Efficient real-time communication
- **Bootstrap**: Minimal CSS payload
- **HTTP Caching**: API response caching

## 📝 API Reference

### Endpoints Used
- `GET /health`: System health check
- `POST /persons`: Add single person
- `POST /persons/batch`: Bulk person import
- `GET /persons/search`: Phonetic name search

### DTO Classes
- **PersonData**: Person creation request
- **PersonIngestResult**: Person creation response
- **SearchResult**: Search response with results
- **PersonDetails**: Individual person information
- **HealthCheckResult**: System health status

## 🛡️ Security

### Best Practices
- **Input Validation**: Client and server-side validation
- **Error Handling**: Secure error messages
- **HTTPS**: SSL/TLS in production
- **CSP Headers**: Content Security Policy
- **CORS**: Properly configured cross-origin requests

### Authentication
Ready for authentication integration:
- **Azure AD**: Enterprise authentication
- **Identity Server**: Custom authentication
- **JWT Tokens**: API authentication
- **Role-based Access**: User permissions

## 📊 Monitoring

### Application Insights
Ready for Azure Application Insights integration:
- **Performance Monitoring**: Page load times
- **Error Tracking**: Exception logging
- **User Analytics**: Usage patterns
- **Custom Metrics**: Business metrics

### Health Checks
Built-in health monitoring:
- **API Availability**: Continuous health checks
- **Visual Indicators**: Status in navigation
- **Error Recovery**: Graceful failure handling

## 🤝 Contributing

### Development Guidelines
1. Follow established patterns
2. Use Bootstrap classes for styling
3. Implement proper error handling
4. Add XML documentation
5. Test responsive design

### Code Style
- **C# Conventions**: Standard .NET conventions
- **Blazor Patterns**: Component lifecycle best practices
- **CSS Organization**: Logical grouping and naming
- **HTML Semantics**: Accessible markup

## 📄 License

This project is part of the PhoneticAnalyzers system. See the main project LICENSE file for details.

## 🆘 Support

### Common Issues
- **Port Conflicts**: Use `netstat` to find conflicting processes
- **API Connection**: Verify API endpoint configuration
- **Build Errors**: Check .NET 8 SDK installation
- **Package Restore**: Run `dotnet restore`

### Getting Help
- Check the main PhoneticAnalyzers documentation
- Review Azure App Service deployment guides
- Consult Blazor Server documentation
- Use GitHub Issues for bug reports

---

**Built with ❤️ using .NET 8 and Blazor Server**