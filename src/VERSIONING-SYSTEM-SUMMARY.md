# Versioning System Summary

## Overview

This repository uses a **centralized version management system** with **automatic version increment** that integrates seamlessly with GitHub Actions for automated publishing to NuGet.org.

## Key Components

### 1. Automatic Version Increment (✨ NEW)

**GitVersion:** Automatically calculates versions from git history
- Each commit auto-increments: `0.0.6-alpha.1`, `0.0.6-alpha.2`, etc.
- Control with commit messages: `+semver:major|minor|patch|skip`
- **See [AUTO-VERSION-INCREMENT.md](AUTO-VERSION-INCREMENT.md) for full guide**

**Manual Scripts:** Explicit version control
- `increment-version.ps1` (PowerShell) / `increment-version.sh` (Bash)
- Increment major, minor, or patch versions
- Change suffixes or create stable releases

### 2. Centralized Version in Directory.Build.props

**Location:** `src/Directory.Build.props`

**Properties:**
```xml
<VersionPrefix>0.0.6</VersionPrefix>
<VersionSuffix>alpha</VersionSuffix>
```

**What It Does:**
- Single source of truth for package versions
- All projects inherit the same version automatically
- Easy to update - just change two lines
- Supports stable releases (no suffix) and pre-releases (alpha, beta, rc)

### 3. GitHub Actions Workflow

**Location:** `.github/workflows/deploy.yml`

**Capabilities:**
- Automatically publishes to NuGet.org
- Supports four versioning strategies:
  1. **GitVersion** - Automatic calculation from git history
  2. **Git Tags** - Traditional tag-based versioning
  3. **Manual Input** - Custom version via workflow dispatch
  4. **Directory.Build.props** - Centralized configuration
- Runs tests before publishing
- Skips duplicate packages

### 4. Documentation

- **AUTO-VERSION-INCREMENT.md** - NEW: Complete automatic versioning guide
- **VERSIONING.md** - Comprehensive versioning guide
- **VERSION-UPDATE-GUIDE.md** - Quick reference card
- **GITHUB-ACTIONS-DEPLOYMENT.md** - Detailed GitHub Actions guide
- **docs/workflows.md** - All workflows overview

## How to Release New Versions

### Option 1: Automatic with GitVersion (✨ Recommended)

**Just commit and push!** GitVersion handles version increment automatically.

```bash
git add .
git commit -m "feat: add new feature"
git push origin master
# → Auto-publishes 0.0.6-alpha.1

git commit -m "fix: bug fix"
git push
# → Auto-publishes 0.0.6-alpha.2
```

**Control increment type:**
```bash
git commit -m "feat: minor change +semver:minor"  # Bumps minor
git commit -m "fix: bug fix +semver:patch"        # Bumps patch (default)
git commit -m "feat!: breaking +semver:major"     # Bumps major
```

### Option 2: Manual Script (For Explicit Control)

**Use increment scripts:**
```bash
./increment-version.ps1           # Windows (patch increment)
./increment-version.sh            # Linux/Mac (patch increment)
./increment-version.ps1 -Suffix beta  # Change to beta

git add src/Directory.Build.props
git commit -m "Bump version to 0.0.7-alpha"
git push origin master
```

**Result:** GitHub Actions publishes `OakIdeas.GenericRepository.0.0.7-alpha.nupkg`

### Option 3: Git Tags (For Stable Releases)

**Method:** Create and push a version tag

```bash
# Create tag (no need to edit Directory.Build.props)
git tag v1.0.0
git push origin v1.0.0
```

**Result:** GitHub Actions publishes `OakIdeas.GenericRepository.1.0.0.nupkg`

### For Testing or Special Cases

**Method:** Manual workflow dispatch from GitHub

1. Go to GitHub → Actions → "Deploy to NuGet"
2. Click "Run workflow"
3. Enter custom version (e.g., `0.0.7-preview.1`)
4. Click "Run workflow"

**Result:** GitHub Actions publishes with your custom version

## Version Priority

When multiple version sources exist, the workflow uses this priority:

1. **Manual input** (workflow dispatch) ← Highest priority
2. **Git tag** (v* tags)
3. **GitVersion** (automatic calculation) ← NEW!
4. **Directory.Build.props** ← Fallback

## Benefits of This System

✅ **Single Source of Truth** - Update version in one place  
✅ **Consistent Versions** - All packages use the same version  
✅ **Flexible** - Three ways to specify version  
✅ **Automated** - GitHub Actions handles publishing  
✅ **Safe** - Tests must pass before publishing  
✅ **Traceable** - Git tags create permanent version markers  
✅ **Easy to Override** - Command-line or CI/CD can override when needed

## Affected Packages

All packages use the centralized version:

- ✅ OakIdeas.GenericRepository
- ✅ OakIdeas.GenericRepository.EntityFrameworkCore
- ✅ OakIdeas.GenericRepository.Memory
- ✅ OakIdeas.GenericRepository.Middleware

## Quick Start

### To publish version 0.0.7-alpha:

```bash
# Edit src/Directory.Build.props
vim src/Directory.Build.props
# Change VersionPrefix to 0.0.7

git add src/Directory.Build.props
git commit -m "Bump to 0.0.7-alpha"
git push origin master
```

### To publish version 1.0.0:

```bash
git tag v1.0.0
git push origin v1.0.0
```

That's it! GitHub Actions handles the rest.

## Prerequisites

- `NUGET_API_KEY` secret configured in GitHub repository settings
- Access to push to `master` branch or create tags

## Troubleshooting

See detailed troubleshooting sections in:
- [VERSIONING.md](VERSIONING.md#troubleshooting)
- [GITHUB-ACTIONS-DEPLOYMENT.md](GITHUB-ACTIONS-DEPLOYMENT.md#troubleshooting)

## Next Steps

1. Test the system by pushing a commit to master
2. Watch GitHub Actions workflow execute
3. Verify packages appear on NuGet.org
4. For stable releases, use git tags

## Additional Resources

- [Semantic Versioning 2.0.0](https://semver.org/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [NuGet Package Versioning](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning)
