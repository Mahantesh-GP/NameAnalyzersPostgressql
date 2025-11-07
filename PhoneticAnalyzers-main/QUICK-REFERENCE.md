# Quick Reference: Managing Two Repositories

## 📂 Your Repository Structure

```
c:\Learnings\
├── PhoneticAnalyzers\              ← Original (experimental/development)
│   └── .git → github.com/Mahantesh-GP/PhoneticAnalyzers.git
│
└── PhoneticAnalyzers-Production\   ← New (stable/production)
    └── .git → YOUR-NEW-REPO (to be set up)
```

## 🎯 Quick Commands

### Check Which Repo You're In
```powershell
# Shows current directory and git remote
pwd; git remote -v
```

### Switch Between Repositories
```powershell
# Go to original (experimental)
cd c:\Learnings\PhoneticAnalyzers

# Go to production
cd c:\Learnings\PhoneticAnalyzers-Production
```

## 🚀 One-Time Setup for New Repository

### Step 1: Create GitHub Repository
1. Go to: https://github.com/new
2. Name: `PhoneticAnalyzers-Production` (or your choice)
3. **Don't** check "Initialize with README"
4. Click "Create repository"
5. Copy the URL shown (e.g., `https://github.com/Mahantesh-GP/PhoneticAnalyzers-Production.git`)

### Step 2: Connect Production Folder to New Repo
```powershell
cd c:\Learnings\PhoneticAnalyzers-Production
.\setup-new-repo.ps1 -NewRepoUrl "YOUR-COPIED-URL-HERE"
```

### Step 3: Push Code
```powershell
git push -u origin main
```

### Step 4: Verify
Visit: https://github.com/YOUR-USERNAME/PhoneticAnalyzers-Production

## ✅ Daily Workflow

### Working on Production Changes
```powershell
cd c:\Learnings\PhoneticAnalyzers-Production
# Make your changes
git add -A
git commit -m "Production update: ..."
git push
```

### Working on Experimental Changes
```powershell
cd c:\Learnings\PhoneticAnalyzers
# Make your changes
git add -A
git commit -m "Experiment: ..."
git push
```

## 🔄 Syncing Changes (Optional)

### Copy Specific Commit from Original to Production
```powershell
cd c:\Learnings\PhoneticAnalyzers-Production

# One-time setup: add original as upstream
git remote add upstream https://github.com/Mahantesh-GP/PhoneticAnalyzers.git

# Fetch latest from original
git fetch upstream

# Find commit hash in original repo
cd c:\Learnings\PhoneticAnalyzers
git log --oneline -10  # Shows recent commits with hashes

# Go back to production and cherry-pick
cd c:\Learnings\PhoneticAnalyzers-Production
git cherry-pick <commit-hash>
git push
```

### Copy All Changes from Original to Production
```powershell
cd c:\Learnings\PhoneticAnalyzers-Production
git remote add upstream https://github.com/Mahantesh-GP/PhoneticAnalyzers.git
git fetch upstream
git merge upstream/main
git push
```

## ⚠️ Safety Tips

1. **Always check which directory you're in** before committing:
   ```powershell
   pwd
   git remote -v
   ```

2. **Use different terminal colors or labels** to distinguish repositories

3. **Keep a text file** with your repo URLs:
   - Original: https://github.com/Mahantesh-GP/PhoneticAnalyzers.git
   - Production: https://github.com/Mahantesh-GP/PhoneticAnalyzers-Production.git

4. **Before pushing**, verify remote:
   ```powershell
   git remote -v
   ```

## 🆘 "Oops!" Recovery

### Pushed to Wrong Repository
```powershell
# Check where you pushed
git log --oneline -1

# If it's in the wrong repo:
# Option 1: Revert on wrong repo
cd <wrong-repo-folder>
git revert HEAD
git push

# Option 2: Reset to previous commit (if no one else pulled)
git reset --hard HEAD~1
git push --force  # Use with caution!
```

### Need to Start Fresh
```powershell
cd c:\Learnings
Remove-Item -Path PhoneticAnalyzers-Production -Recurse -Force
git clone https://github.com/Mahantesh-GP/PhoneticAnalyzers.git PhoneticAnalyzers-Production
# Then run setup-new-repo.ps1 again
```

## 📊 Comparison

| Feature | Original Repo | Production Repo |
|---------|--------------|-----------------|
| **Purpose** | Development/experimentation | Stable production code |
| **Location** | `c:\Learnings\PhoneticAnalyzers` | `c:\Learnings\PhoneticAnalyzers-Production` |
| **GitHub URL** | `.../PhoneticAnalyzers.git` | `.../PhoneticAnalyzers-Production.git` |
| **Changes affect** | Only this repo | Only this repo |
| **Best for** | Testing, breaking changes | Stable features, deployments |

## 💡 Pro Tips

1. **Use Git Bash or Windows Terminal** with folder name in prompt
2. **Set up GitHub Desktop** to manage both repos visually
3. **Use VS Code workspaces** to open both folders in separate windows
4. **Tag releases** in production repo: `git tag v1.0.0`

---

**Remember**: After initial setup, both repositories are completely independent!
