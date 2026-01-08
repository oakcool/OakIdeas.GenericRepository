# Quick Version Update Reference

## Current Setup
All packages now use centralized versioning from `Directory.Build.props`.

## To Increment Version

### 1. Edit Directory.Build.props
Change these lines (around line 16-17):
```xml
<VersionPrefix>0.0.6</VersionPrefix>
<VersionSuffix>alpha</VersionSuffix>
```

### 2. Common Version Updates

**Next Patch Version (0.0.7-alpha):**
```xml
<VersionPrefix>0.0.7</VersionPrefix>
<VersionSuffix>alpha</VersionSuffix>
```

**Beta Release (0.0.7-beta):**
```xml
<VersionPrefix>0.0.7</VersionPrefix>
<VersionSuffix>beta</VersionSuffix>
```

**Stable Release (1.0.0):**
```xml
<VersionPrefix>1.0.0</VersionPrefix>
<VersionSuffix></VersionSuffix>
```

### 3. Build and Pack (Manual)
```bash
dotnet clean
dotnet build -c Release
```

Packages will be generated in each project's `bin/Release` folder with the new version.

## Publishing Options

### Option A: Automated via GitHub Actions (Recommended)

#### A1. Push to Master with Updated Directory.Build.props
```bash
# Edit Directory.Build.props first, then:
git add Directory.Build.props
git commit -m "Bump version to 0.0.7-alpha"
git push origin master
```
The GitHub Actions workflow will automatically build, test, and publish.

#### A2. Create and Push a Version Tag
```bash
git tag v1.0.0
git push origin v1.0.0
```
The tag version overrides Directory.Build.props version.

#### A3. Manual Workflow Trigger
1. Go to GitHub → Actions → "Deploy to NuGet"
2. Click "Run workflow"
3. Enter version or leave empty to use Directory.Build.props
4. Click "Run workflow"

### Option B: Manual Publishing
```bash
dotnet nuget push "**/*.nupkg" --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
```

## Override from Command Line (Without Editing Files)
```bash
# For testing or CI/CD
dotnet pack -c Release /p:Version=0.0.7-alpha
```

## Affected Packages
- ✅ OakIdeas.GenericRepository
- ✅ OakIdeas.GenericRepository.EntityFrameworkCore
- ✅ OakIdeas.GenericRepository.Memory
- ✅ OakIdeas.GenericRepository.Middleware

All packages will use the same version from Directory.Build.props.

## Note
Test projects (*.Tests) are not packaged and don't need version numbers.
