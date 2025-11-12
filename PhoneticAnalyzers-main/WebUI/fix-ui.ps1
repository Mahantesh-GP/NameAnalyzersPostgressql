# Fix Blazor WebAssembly UI compilation errors

Write-Host "Fixing Blazor WebAssembly UI..." -ForegroundColor Cyan

$webUIPath = "c:\Learnings\PhoneticAnalyzer-short\PhoneticAnalyzers-main\WebUI"

# Fix MainLayout
Write-Host "Creating MainLayout.razor..." -ForegroundColor Yellow
$mainLayoutContent = @'
@inherits LayoutComponentBase
@inject NavigationManager Navigation

<MudThemeProvider @bind-IsDarkMode="@_isDarkMode" Theme="_theme" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Elevation="2" Dense="false">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" 
                       Color="Color.Inherit" 
                       Edge="Edge.Start" 
                       OnClick="@ToggleDrawer" />
        <MudText Typo="Typo.h6" Class="ml-3">Phonetic Analyzers</MudText>
        <MudSpacer />
        <MudIconButton Icon="@(Icons.Material.Filled.Info)" 
                       Color="Color.Inherit" 
                       Href="https://github.com/Mahantesh-GP/PhoneticAnalyzers" 
                       Target="_blank" />
        <MudIconButton Icon="@(_isDarkMode ? Icons.Material.Filled.LightMode : Icons.Material.Filled.DarkMode)" 
                       Color="Color.Inherit" 
                       OnClick="@ToggleTheme" />
    </MudAppBar>

    <MudDrawer @bind-Open="_drawerOpen" 
               Elevation="2" 
               ClipMode="DrawerClipMode.Always">
        <MudDrawerHeader>
            <MudText Typo="Typo.h6" Color="Color.Primary">
                <MudIcon Icon="@Icons.Material.Filled.AccountBox" Class="mr-2" />
                Name Search
            </MudText>
        </MudDrawerHeader>
        <MudNavMenu>
            <MudNavLink Href="/" 
                        Icon="@Icons.Material.Filled.Home" 
                        Match="NavLinkMatch.All">
                Home
            </MudNavLink>
            <MudNavLink Href="/search" 
                        Icon="@Icons.Material.Filled.Search">
                Advanced Search
            </MudNavLink>
            <MudNavLink Href="/bulk" 
                        Icon="@Icons.Material.Filled.CloudUpload">
                Bulk Upload
            </MudNavLink>
            <MudDivider Class="my-2" />
            <MudNavLink Href="/about" 
                        Icon="@Icons.Material.Filled.Info">
                About
            </MudNavLink>
        </MudNavMenu>
    </MudDrawer>

    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.ExtraExtraLarge" Class="my-6">
            <ErrorBoundary>
                <ChildContent>
                    @Body
                </ChildContent>
                <ErrorContent Context="ex">
                    <MudAlert Severity="Severity.Error" Class="my-4">
                        <MudText Typo="Typo.h6">An error occurred</MudText>
                        <MudText Typo="Typo.body2">@ex.Message</MudText>
                    </MudAlert>
                </ErrorContent>
            </ErrorBoundary>
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private bool _drawerOpen = true;
    private bool _isDarkMode = true;

    private MudTheme _theme = new MudTheme()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#1976d2",
            Secondary = "#dc004e",
            AppbarBackground = "#1976d2",
            Success = "#4caf50",
            Info = "#2196f3",
            Warning = "#ff9800",
            Error = "#f44336"
        },
        PaletteDark = new PaletteDark()
        {
            Primary = "#90caf9",
            Secondary = "#f48fb1",
            AppbarBackground = "#1e1e1e",
            Success = "#66bb6a",
            Info = "#42a5f5",
            Warning = "#ffa726",
            Error = "#ef5350",
            Background = "#121212",
            Surface = "#1e1e1e",
            DrawerBackground = "#1e1e1e"
        },
        Typography = new Typography()
        {
            Default = new Default()
            {
                FontFamily = new[] { "Roboto", "Helvetica", "Arial", "sans-serif" }
            }
        }
    };

    private void ToggleDrawer()
    {
        _drawerOpen = !_drawerOpen;
    }

    private void ToggleTheme()
    {
        _isDarkMode = !_isDarkMode;
    }
}
'@
Set-Content -Path "$webUIPath\Layout\MainLayout.razor" -Value $mainLayoutContent -Encoding UTF8

# Fix imports
Write-Host "Fixing _Imports.razor..." -ForegroundColor Yellow
$importsContent = @'
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.AspNetCore.Components.WebAssembly.Http
@using Microsoft.JSInterop
@using MudBlazor
@using PhoneticAnalyzers.WebUI
@using PhoneticAnalyzers.WebUI.Layout
@using PhoneticAnalyzers.WebUI.Models
@using PhoneticAnalyzers.WebUI.Services
@using PhoneticAnalyzers.WebUI.Components.Search
@using PhoneticAnalyzers.WebUI.Components.Bulk
'@
Set-Content -Path "$webUIPath\_Imports.razor" -Value $importsContent -Encoding UTF8

Write-Host "`nAttempting to build..." -ForegroundColor Cyan
cd $webUIPath
dotnet build

Write-Host "`nDone! If errors remain, manually add T='string' to MudChip/MudList components." -ForegroundColor Green
