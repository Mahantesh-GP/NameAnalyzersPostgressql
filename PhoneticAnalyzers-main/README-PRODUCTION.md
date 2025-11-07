# PhoneticAnalyzers - Production Repository

This is a separate repository cloned from the original PhoneticAnalyzers project. It allows you to maintain a production version with full features while keeping the original repository for experimentation.

## 📁 Repository Structure

This repository contains the **full-featured version** of PhoneticAnalyzers with:
- ✅ Validation infrastructure (FluentValidation)
- ✅ Search functionality
- ✅ Batch upload capabilities
- ✅ ValidationSummary Blazor component
- ✅ JSON deserialization fixes
- ✅ Simplified UI (no Dashboard stats, no Person Management)

## 🔗 Repository Relationship

- **Original Repository**: `https://github.com/Mahantesh-GP/PhoneticAnalyzers.git`
  - Used for experimentation and development
  - Your changes here won't affect this production version

- **This Repository**: `PhoneticAnalyzers-Production` (or your new name)
  - Stable production version
  - Independent from original repository
  - Can be deployed without affecting development work

## 🚀 Setup Instructions

### First-Time Setup

1. **Create New GitHub Repository**
   - Go to https://github.com/new
   - Name: `PhoneticAnalyzers-Production` (or your choice)
   - Make it private or public as needed
   - **Don't** initialize with README (this repo already has one)

2. **Connect to New Repository**
   ```powershell
   cd c:\Learnings\PhoneticAnalyzers-Production
   .\setup-new-repo.ps1 -NewRepoUrl "https://github.com/YOUR-USERNAME/YOUR-NEW-REPO.git"
   ```

3. **Push to New Repository**
   ```powershell
   git push -u origin main
   ```

### Verify Setup

```powershell
# Check current remote
git remote -v

# Should show your NEW repository URL, not the original one
```

## 🛠️ Development Workflow

### Working on Production Version (This Repo)
```powershell
cd c:\Learnings\PhoneticAnalyzers-Production
# Make changes
git add -A
git commit -m "Your production changes"
git push
```

### Working on Experimental Version (Original Repo)
```powershell
cd c:\Learnings\PhoneticAnalyzers
# Make experimental changes
git add -A
git commit -m "Your experimental changes"
git push
```

### Syncing Changes Between Repositories

If you want to bring changes from original to production:
```powershell
cd c:\Learnings\PhoneticAnalyzers-Production

# Add original repo as a remote (one-time setup)
git remote add upstream https://github.com/Mahantesh-GP/PhoneticAnalyzers.git

# Fetch changes from original
git fetch upstream

# Merge specific changes (cherry-pick or merge)
git cherry-pick <commit-hash>
# OR
git merge upstream/main

# Push to production
git push origin main
```

## 📋 Current Features

- **Search**: Phonetic name matching with DoubleMetaphone and Beider-Morse algorithms
- **Batch Upload**: Manual input and CSV file upload for bulk ingestion
- **Validation**: Client-side validation error display with structured messages
- **API Health**: System status monitoring

## 🔧 Running the Application

```powershell
# Start Function Apps (two separate terminals)
cd src\PhoneticAnalyzers.Functions.Ingestion
func start

cd src\PhoneticAnalyzers.Functions.Search
func start --port 7072

# Start Web App (another terminal)
cd Web
dotnet run
```

Access at: http://localhost:5153

## 📝 Notes

- Both repositories share the same commit history up to the clone point
- After setup, they are completely independent
- Changes in one repository won't affect the other
- You can have different features, configurations, or versions in each

## 🆘 Troubleshooting

**Problem**: Accidentally pushed to wrong repository

**Solution**:
```powershell
# Check which repository you're connected to
git remote -v

# If wrong, update the remote
git remote set-url origin <correct-repo-url>
```

**Problem**: Want to reset to original state

**Solution**:
```powershell
# Delete this folder and re-clone
cd c:\Learnings
Remove-Item -Path PhoneticAnalyzers-Production -Recurse -Force
git clone https://github.com/Mahantesh-GP/PhoneticAnalyzers.git PhoneticAnalyzers-Production
```

## 📚 Documentation

For detailed documentation about the PhoneticAnalyzers system, refer to:
- Original repository: https://github.com/Mahantesh-GP/PhoneticAnalyzers
- Azure deployment guides (if applicable)
- API documentation in the original repo

---

**Created**: November 7, 2025  
**Purpose**: Maintain stable production version separate from experimental development  
**Original Repository**: Mahantesh-GP/PhoneticAnalyzers
