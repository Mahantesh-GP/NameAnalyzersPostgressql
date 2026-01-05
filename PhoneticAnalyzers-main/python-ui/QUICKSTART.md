# Quick Start Guide

## Prerequisites

- Python 3.11+
- .NET 9 SDK (for the C# API)
- PostgreSQL 14+ with `phonetic_native` database configured (used by the API)
- Poetry (recommended) or pip

## Installation

### Using Poetry (Recommended)

```powershell
# Navigate to the python-ui folder
cd python-ui

# Install dependencies
poetry install

# Copy environment configuration
cp .env.example .env

# Edit .env with your settings (see Configuration)
```

### Using pip

```powershell
cd python-ui
python -m venv venv
.\venv\Scripts\Activate.ps1
pip install -r requirements.txt
cp .env.example .env
```

## Configuration

The Python UI calls the .NET Core API. Configure the UI `.env`:

```env
DATABASE_URL=postgresql://postgres:postgres@localhost:5432/phonetic_native
HOST=0.0.0.0
PORT=8000
DEBUG=true
LOG_LEVEL=info
API_BASE_URL=http://localhost:5100
```

## Running the Application

### 1) Start the .NET Core API

```powershell
cd ../sql-native-search/api
dotnet restore
dotnet build
dotnet run
```

The API listens at: **http://localhost:5100** (see `Properties/launchSettings.json`).

### 2) Start the Python UI

### Using Poetry

```powershell
poetry run python run.py
```

### Using pip

```powershell
.\venv\Scripts\Activate.ps1
python run.py
```

The UI will start at: **http://localhost:8000**

## Testing the Search

1. Open http://localhost:8000 in your browser
2. Enter search criteria:
   - **First Name**: John
   - **Last Name**: Smith
   - **County**: Orange
3. Select match strategies:
   - ☑ Nickname (enabled by default)
   - ☐ Fuzzy (Trigram)
   - ☐ Phonetic
4. Choose view type:
   - **List**: All results in a single list
   - **Grouped**: Results grouped by match type
5. Click **Search**

## UI Endpoints

FastAPI docs for the UI are at: **http://localhost:8000/docs**

### Main Endpoints

- `POST /api/search` - UI search (calls C# API `/api/search/advanced`)
- `GET /api/suggestions` - Autocomplete suggestions (calls C# API `/api/search/suggestions`)
- `GET /api/counties` - List of counties (calls C# API `/api/counties`)
- `GET /health` - UI health check

## Development

### Code Quality

```powershell
# Format code
poetry run black .

# Lint code
poetry run ruff check .

# Type checking
poetry run mypy app
```

### Running Tests

```powershell
poetry run pytest tests/ -v
```

## Common Issues

### API or Database Connection Failed

1. Verify the .NET API is running and reachable: **http://localhost:5100/swagger**
2. Check PostgreSQL is running: `Get-Service -Name postgresql*`
3. Verify database exists: `psql -U postgres -c "\l"`
4. Verify credentials in `sql-native-search/api/appsettings.json` and UI `.env`
5. Test DB connection: `psql -U postgres -d phonetic_native -c "SELECT 1"`

### Port Already in Use

Change the port in `.env`:
```env
PORT=8001
```

### Module Not Found

Reinstall dependencies:
```powershell
poetry install --no-cache
# or
pip install -r requirements.txt --force-reinstall
```

## Features

✅ **Multi-strategy search**: Exact, Nickname, Fuzzy (Trigram), Phonetic  
✅ **Smart result distribution**: Proportional allocation among enabled strategies  
✅ **Autocomplete**: Real-time suggestions for names and counties  
✅ **Two view modes**: List view and grouped view  
✅ **Responsive design**: Works on mobile, tablet, and desktop  
✅ **Fast performance**: <100ms search response time  
✅ **Modern UI**: TailwindCSS with gradient backgrounds and smooth animations  

## Next Steps

- Customize the UI colors in `templates/base.html` (Tailwind classes)
- Add more search filters in `templates/components/search_form.html`
- Extend API endpoints in `app/api/search.py`
- Add authentication if needed (see README.md for examples)

## Need Help?

- Check the main [README.md](README.md) for detailed documentation
- Review [ARCHITECTURE-OVERVIEW.md](../ARCHITECTURE-OVERVIEW.md) for system design
- Open an issue on GitHub for bug reports or feature requests
