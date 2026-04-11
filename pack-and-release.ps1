# run-pack-and-release.ps1

param(
    [switch]$DryRun
)

# Run the pack script and capture output
$output = & .\pack.ps1

# Display the pack output
Write-Host $output

# Parse the version numbers from the output
# Expected format: "..._<major>_<minor>_<patch>.pext"
if ($output -match '_(\d+)_(\d+)_(\d+)\.pext"') {
    $major = $matches[1]
    $minor = $matches[2]
    $patch = $matches[3]
    
    $version = "v$major.$minor.$patch"
    $versionNumber = "$major.$minor.$patch"
    Write-Host "Detected version: $version"
    
    # Extract the full file path
    if ($output -match '"([^"]+\.pext)"') {
        $filePath = $matches[1]
        Write-Host "File path: $filePath"
        
        # Extract just the filename
        $fileName = Split-Path $filePath -Leaf
        
        # Get repository info (owner/repo)
        $repoInfo = gh repo view --json nameWithOwner -q .nameWithOwner
        
        # Construct the download URL
        $downloadUrl = "https://github.com/$repoInfo/releases/download/$version/$fileName"
        
        # Get current date in YYYY-MM-DD format
        $currentDate = Get-Date -Format "yyyy-MM-dd"
        
        # Check if file exists
        if (Test-Path $filePath) {
            # Check if release already exists
            $releaseExists = gh release view $version 2>&1
            $releaseAlreadyExists = $LASTEXITCODE -eq 0
            
            if ($DryRun) {
                Write-Host "`n=== DRY RUN MODE ===" -ForegroundColor Yellow
                if ($releaseAlreadyExists) {
                    Write-Host "Release $version already exists - would update it" -ForegroundColor Yellow
                    Write-Host "Would upload file to existing release:" -ForegroundColor Yellow
                }
                else {
                    Write-Host "Would create new GitHub release with:" -ForegroundColor Yellow
                }
                Write-Host "  Tag: $version" -ForegroundColor Yellow
                Write-Host "  Title: Release $version" -ForegroundColor Yellow
                Write-Host "  File: $filePath" -ForegroundColor Yellow
                Write-Host "  Notes: Auto-generated" -ForegroundColor Yellow
                Write-Host "`nCommand that would be executed:" -ForegroundColor Yellow
                if ($releaseAlreadyExists) {
                    Write-Host "gh release upload $version `"$filePath`" --clobber" -ForegroundColor Cyan
                }
                else {
                    Write-Host "gh release create $version --title `"Release $version`" --generate-notes `"$filePath`"" -ForegroundColor Cyan
                }
                Write-Host "`nArtifact URL would be:" -ForegroundColor Yellow
                Write-Host $downloadUrl -ForegroundColor Cyan
                
                Write-Host "`nWould append to manifest.yaml:" -ForegroundColor Yellow
                $manifestEntry = @"
  - Version: $versionNumber
    RequiredApiVersion: 6.12.0.0
    ReleaseDate: $currentDate
    PackageUrl: $downloadUrl
    Changelog:
      - <ADD TEXT HERE>
"@
                Write-Host $manifestEntry -ForegroundColor Cyan
            }
            else {
                if ($releaseAlreadyExists) {
                    Write-Host "Release $version already exists - uploading file to existing release..." -ForegroundColor Yellow
                    
                    # Upload to existing release (--clobber replaces if file already exists)
                    gh release upload $version $filePath --clobber
                    
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "Successfully uploaded $filePath to existing release $version" -ForegroundColor Green
                        Write-Host "`nArtifact URL:" -ForegroundColor Green
                        Write-Host $downloadUrl
                    }
                    else {
                        Write-Error "Failed to upload file to existing release"
                        exit 1
                    }
                }
                else {
                    Write-Host "Creating GitHub release $version..."
                    
                    # Create the release and upload the file
                    gh release create $version `
                        --title "Release $version" `
                        --generate-notes `
                        $filePath
                    
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "Successfully created release $version and uploaded $filePath" -ForegroundColor Green
                        Write-Host "`nArtifact URL:" -ForegroundColor Green
                        Write-Host $downloadUrl
                    }
                    else {
                        Write-Error "Failed to create GitHub release"
                        exit 1
                    }
                }
                
                # Append to manifest.yaml
                $manifestEntry = @"
  - Version: $versionNumber
    RequiredApiVersion: 6.12.0.0
    ReleaseDate: $currentDate
    PackageUrl: $downloadUrl
    Changelog:
      - <ADD TEXT HERE>
"@
                
                Write-Host "`nAppending to manifest.yaml..." -ForegroundColor Green
                Add-Content -Path "./installer-manifest.yaml" -Value $manifestEntry
                Write-Host "Successfully updated manifest.yaml" -ForegroundColor Green
            }
        }
        else {
            Write-Error "File not found: $filePath"
            exit 1
        }
    }
    else {
        Write-Error "Could not extract file path from output"
        exit 1
    }
}
else {
    Write-Error "Could not parse version numbers from output: $output"
    exit 1
}