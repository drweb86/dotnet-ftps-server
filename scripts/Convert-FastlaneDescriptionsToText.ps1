Param(
    [string]$MetadataRoot = (Join-Path $PSScriptRoot "..\fastlane\metadata\android")
)

$ErrorActionPreference = "Stop"

function Convert-HtmlToPlainText {
    param([string]$Html)

    $text = $Html -replace "`r`n", "`n" -replace "`r", "`n"

    $text = $text -replace '(?i)<br\s*/?>', "`n"
    $text = $text -replace '(?i)</(p|div|h[1-6]|tr|blockquote|table)>', "`n`n"
    $text = $text -replace '(?i)</li>', "`n"
    $text = $text -replace '(?i)<li[^>]*>', "- "
    $text = $text -replace '(?i)</(ul|ol)>', "`n"
    $text = [regex]::Replace($text, '<[^>]+>', '')
    $text = [System.Net.WebUtility]::HtmlDecode($text)

    $lines = $text -split "`n" | ForEach-Object {
        ($_ -replace '[ \t]+', ' ').Trim()
    }
    $text = $lines -join "`n"
    $text = [regex]::Replace($text, '(\n){3,}', "`n`n")
    return $text.Trim() + "`n"
}

$resolvedRoot = (Resolve-Path $MetadataRoot).Path
$files = Get-ChildItem -Path $resolvedRoot -Filter "full_description.txt" -Recurse -File

if (-not $files) {
    Write-Error "No full_description.txt files found under $resolvedRoot"
}

foreach ($file in $files) {
    $html = Get-Content -Path $file.FullName -Raw -Encoding utf8
    $plain = Convert-HtmlToPlainText -Html $html
    $outputPath = Join-Path $file.DirectoryName "full_description_text.txt"
    [System.IO.File]::WriteAllText($outputPath, $plain, [System.Text.UTF8Encoding]::new($false))
    Write-Output $outputPath
}

Write-Output "Converted $($files.Count) description(s)."
