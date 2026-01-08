# Package Versioning Guide

## Overview

All packages in this solution now use centralized version management through `Directory.Build.props`. This ensures consistent versioning across all packages and makes it easy to increment versions.

## Current Version

- **VersionPrefix**: `0.0.6`
- **VersionSuffix**: `alpha`
- **Full Version**: `0.0.6-alpha`

## How to Increment Versions

### Option 1: Update Directory.Build.props (Recommended for releases)

Edit the `Directory.Build.props` file and update the version properties:

```xml
<VersionPrefix>0.0.7</VersionPrefix>
<VersionSuffix>alpha</VersionSuffix>
```

For a stable release, remove or clear the `VersionSuffix`:

```xml
<VersionPrefix>1.0.0</VersionPrefix>
<VersionSuffix></VersionSuffix>
```

### Option 2: Command Line Override (Recommended for CI/CD)

You can override the version at build/pack time without modifying files:

```bash
# Build with a specific version
dotnet build /p:Version=0.0.7-alpha

# Pack with a specific version
dotnet pack /p:Version=0.0.7-alpha

# Use VersionPrefix and VersionSuffix separately
dotnet pack /p:VersionPrefix=1.0.0 /p:VersionSuffix=beta

# Stable release (no suffix)
dotnet pack /p:VersionPrefix=1.0.0 /p:VersionSuffix=
```

### Option 3: Environment Variables (For CI/CD)

Set environment variables before building:

```bash
# PowerShell
$env:VersionPrefix="0.0.7"
$env:VersionSuffix="alpha"
dotnet pack

# Bash
export VersionPrefix=0.0.7
export VersionSuffix=alpha
dotnet pack
```

## Publishing to NuGet.org

### Automated Publishing with GitHub Actions

The repository includes a GitHub Actions workflow (`.github/workflows/deploy.yml`) that automatically publishes packages to NuGet.org. The workflow is designed to work seamlessly with the centralized versioning system.

#### Method 1: Use Version from Directory.Build.props (Recommended for Regular Releases)

1. Update version in `Directory.Build.props`:
   ```xml
   <VersionPrefix>0.0.7</VersionPrefix>
   <VersionSuffix>alpha</VersionSuffix>
   ```

2. Commit and push to `master`:
   ```bash
   git add Directory.Build.props
   git commit -m "Bump version to 0.0.7-alpha"
   git push origin master
   ```

3. The workflow will automatically:
   - Build the solution
   - Run all tests
   - Create packages with version `0.0.7-alpha`
   - Publish to NuGet.org

#### Method 2: Create a Version Tag (Recommended for Stable Releases)

1. Create and push a version tag:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

2. The workflow will automatically:
   - Extract version `1.0.0` from the tag
   - Override any version in Directory.Build.props
   - Build, test, and publish packages with version `1.0.0`

#### Method 3: Manual Workflow Dispatch

1. Go to GitHub Actions tab
2. Select "Deploy to NuGet" workflow
3. Click "Run workflow"
4. Enter a version (e.g., `0.0.7-beta`) or leave empty to use Directory.Build.props
5. Click "Run workflow"

**Note:** The `NUGET_API_KEY` secret must be configured in your repository settings.

### Manual Publishing

1. Update version in `Directory.Build.props`
2. Build and create packages:
   ```bash
   dotnet clean
   dotnet build -c Release
   dotnet pack -c Release
   ```
3. Publish to NuGet:
   ```bash
   dotnet nuget push "**/*.nupkg" --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
   ```


## Per-Project Version Overrides

If you need a specific project to have a different version, you can override it in that project's `.csproj` file:

```xml
<PropertyGroup>
  <Version>0.0.5-legacy</Version>
</PropertyGroup>
```

However, this is not recommended as it breaks version consistency.

## Version Components Explained

- **VersionPrefix**: The main version number (e.g., `1.0.0`, `0.0.6`)
- **VersionSuffix**: The pre-release label (e.g., `alpha`, `beta`, `rc1`). Leave empty for stable releases.
- **Version**: The complete version string (`VersionPrefix-VersionSuffix`)
- **AssemblyVersion**: The assembly version for binary compatibility (automatically set to `VersionPrefix.0`)
- **FileVersion**: The file version for Windows properties (automatically set to `VersionPrefix.0`)

## Semantic Versioning

This project follows [Semantic Versioning 2.0.0](https://semver.org/):

- **MAJOR** version (X.0.0): Incompatible API changes
- **MINOR** version (0.X.0): Backward-compatible functionality additions
- **PATCH** version (0.0.X): Backward-compatible bug fixes
- **Pre-release** suffix: `-alpha`, `-beta`, `-rc1`, etc.

## Troubleshooting

### Version not updating on NuGet

1. Make sure you've incremented the version number
2. Clear local package cache:
   ```bash
   dotnet nuget locals all --clear
   ```
3. Verify the package version before publishing:
   ```bash
   dotnet pack -c Release
   # Check the .nupkg file name to verify version
   ```

### Build shows wrong version

1. Clean the solution:
   ```bash
   dotnet clean
   ```
2. Delete `bin` and `obj` folders:
   ```bash
   Get-ChildItem -Recurse -Directory bin,obj | Remove-Item -Recurse -Force
   ```
3. Rebuild:
   ```bash
   dotnet build -c Release
   ```
