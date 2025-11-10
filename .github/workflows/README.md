# GitHub Actions Workflows

This directory contains GitHub Actions workflows for the Meshcore.NET project.

## Release Workflow

The `release.yml` workflow automates the process of creating releases for this project.

### How It Works

The workflow is triggered when you push a version tag to the repository. The tag should follow the semantic versioning format: `v*.*.*` (e.g., `v1.0.0`, `v1.2.3`, `v2.0.0-beta`).

### Steps Performed

1. **Checkout code** - Gets the latest code from the repository
2. **Setup .NET** - Installs .NET 9.0 SDK
3. **Restore dependencies** - Restores NuGet packages
4. **Build (Debug)** - Builds the project in Debug configuration
5. **Build (Release)** - Builds the project in Release configuration
6. **Pack NuGet package** - Creates a NuGet package (.nupkg)
7. **Extract version** - Extracts version number from the tag
8. **Create Release Notes** - Generates automated release notes
9. **Create GitHub Release** - Creates a GitHub release with artifacts

### Release Artifacts

The workflow includes the following artifacts in the release:

- **NuGet Package** (`Meshcore.NET.*.nupkg`) - Ready to publish to NuGet.org
- **Release DLL** (`meshcore-lib.dll` from Release build)
- **Debug DLL** (`meshcore-lib.dll` from Debug build)

### How to Create a Release

1. **Ensure your code is ready for release**
   ```bash
   # Make sure all changes are committed
   git status
   
   # Make sure you're on the main branch (or your default branch)
   git checkout main
   git pull
   ```

2. **Create and push a version tag**
   ```bash
   # Create a tag with your version number
   git tag -a v1.0.0 -m "Release version 1.0.0"
   
   # Push the tag to GitHub
   git push origin v1.0.0
   ```

3. **Monitor the workflow**
   - Go to the "Actions" tab in your GitHub repository
   - You should see the "Create Release" workflow running
   - Wait for it to complete (usually takes 1-2 minutes)

4. **Check the release**
   - Go to the "Releases" section of your GitHub repository
   - You should see a new release with your version number
   - The release will include:
     - Automatically generated release notes
     - Downloadable artifacts (NuGet package and DLLs)

### Customizing the Release

#### Update Version in .csproj

Before creating a release tag, update the version in `meshcore-lib/meshcore-lib.csproj`:

```xml
<PropertyGroup>
    <Version>1.0.0</Version>
    <!-- Update this to match your tag version -->
</PropertyGroup>
```

#### Modify Release Notes

The workflow automatically generates release notes. To customize them:

1. Edit the `Create Release Notes` step in `.github/workflows/release.yml`
2. Modify the content within the `EOF` markers
3. You can use GitHub's auto-generated release notes by removing the `body_path` parameter and adding:
   ```yaml
   generate_release_notes: true
   ```

### Publishing to NuGet.org

The workflow creates a NuGet package but doesn't automatically publish it. To publish:

1. **Download the .nupkg file** from the GitHub release

2. **Publish to NuGet.org** manually:
   ```bash
   dotnet nuget push Meshcore.NET.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
   ```

3. **Or add automatic publishing** to the workflow by adding this step:
   ```yaml
   - name: Publish to NuGet
     run: dotnet nuget push artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json --skip-duplicate
   ```
   
   Note: You'll need to add your NuGet API key as a repository secret named `NUGET_API_KEY`.

### Pre-releases

To create a pre-release version, use a tag with a pre-release identifier:

```bash
# Examples of pre-release tags
git tag -a v1.0.0-alpha -m "Alpha release"
git tag -a v1.0.0-beta.1 -m "Beta release 1"
git tag -a v1.0.0-rc.1 -m "Release candidate 1"

git push origin v1.0.0-alpha
```

To mark the release as a pre-release in the workflow, modify the `prerelease` parameter in `.github/workflows/release.yml`:

```yaml
- name: Create GitHub Release
  uses: softprops/action-gh-release@v1
  with:
    prerelease: ${{ contains(github.ref, 'alpha') || contains(github.ref, 'beta') || contains(github.ref, 'rc') }}
```

### Troubleshooting

#### Workflow doesn't trigger
- Make sure you pushed the tag: `git push origin v1.0.0`
- Check that the tag follows the `v*.*.*` pattern
- Verify you have the workflow file in `.github/workflows/release.yml`

#### Build fails
- Check the Actions tab for detailed error messages
- Ensure the project builds locally: `dotnet build --configuration Release`
- Verify all dependencies are properly referenced

#### Release creation fails
- Check that the repository has proper permissions
- The workflow needs `contents: write` permission (already configured)
- Ensure the `GITHUB_TOKEN` has the necessary permissions

### Examples

#### Creating version 1.0.0
```bash
git tag -a v1.0.0 -m "First stable release"
git push origin v1.0.0
```

#### Creating version 2.1.5
```bash
git tag -a v2.1.5 -m "Bug fixes and improvements"
git push origin v2.1.5
```

#### Creating a beta release
```bash
git tag -a v2.0.0-beta.1 -m "Beta testing for v2.0.0"
git push origin v2.0.0-beta.1
```

### Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Semantic Versioning](https://semver.org/)
- [Publishing NuGet Packages](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
