# Parameter help description
[Parameter(Mandatory = $true)]
[string]$RootDir

Write-Host "Merging locale in ./crowdin/temp"

# Find all directories containing *.json files
$localeDirs = Get-ChildItem -Path "./crowdin/temp" -Directory | Where-Object {
    Test-Path (Join-Path $_.FullName "*.json")
}

if ($localeDirs.Count -eq 0) {
    Write-Host "No locale folders found containing JSON files."
    exit 0
}

foreach ($dir in $localeDirs) {
    $locale = $dir.Name
    $jsonFiles = Get-ChildItem -Path $dir.FullName -Filter *.json

    if ($jsonFiles.Count -eq 0) {
        Write-Host "Skipping $locale (no JSON files)"
        continue
    }

    Write-Host "Merging $($jsonFiles.Count) JSON files for locale [$locale]..."

    $merged = @{}

    foreach ($file in $jsonFiles) {
        try {
            $json = Get-Content $file.FullName -Raw | ConvertFrom-Json -ErrorAction Stop

            foreach ($key in $json.PSObject.Properties.Name) {
                $merged[$key] = $json.$key
            }
        }
        catch {
            Write-Warning "Failed to parse JSON in $($file.FullName): $_"
        }
    }

    $outputPath = Join-Path "./src/AdvancedRoadTools/lang" "$locale.json"
    $merged | ConvertTo-Json -Depth 10 | Out-File -Encoding utf8 $outputPath
}

Remove-Item -Path "./crowdin/temp" -Recurse -Force

Write-Host "Done merging all locale JSON files."