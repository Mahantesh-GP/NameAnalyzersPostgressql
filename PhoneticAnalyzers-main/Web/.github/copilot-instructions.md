# PhoneticAnalyzers Blazor UI - Copilot Instructions

This workspace contains a comprehensive Blazor Server application for the PhoneticAnalyzers phonetic name matching system.

## Project Overview
- **Type**: Blazor Server Web Application
- **Framework**: .NET 8, ASP.NET Core Blazor Server
- **UI**: Bootstrap 5, modern responsive design
- **Architecture**: Clean Architecture with component-based design
- **Features**: Real-time search, batch operations, phonetic matching visualization
- **Deployment**: Single Azure App Service with API backend

## Key Components
- **Pages**: Dashboard, Person Management, Search, Batch Operations
- **Components**: Reusable UI components with modern design patterns
- **Services**: API client for PhoneticAnalyzers backend
- **State Management**: Blazor Server state with SignalR for real-time updates

## Development Guidelines
- Follow Blazor Server best practices
- Use component lifecycle methods properly
- Implement responsive design with Bootstrap 5
- Maintain separation of concerns
- Use dependency injection for services
- Follow modern C# patterns and conventions

## API Integration
- Integrates with PhoneticAnalyzers Functions API
- Supports all CRUD operations for person management
- Real-time phonetic search with similarity scoring
- Batch processing with progress indicators

## Deployment
- Configured for Azure App Service deployment
- Can be deployed alongside Functions API in same App Service
- Production-ready with proper error handling and logging