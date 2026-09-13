Param(
    [string]$MetadataRoot = (Join-Path $PSScriptRoot "..\fastlane\metadata\android")
)

$ErrorActionPreference = "Stop"

$LanguageNameOverrides = @{
    "am-ET"  = "Amharic"
    "ar-SA"  = "Arabic"
    "bn-IN"  = "Bengali"
    "de-DE"  = "German"
    "en-US"  = "English"
    "es-ES"  = "Spanish"
    "fa-IR"  = "Persian"
    "fr-FR"  = "French"
    "ha-NG"  = "Hausa"
    "hi-IN"  = "Hindi"
    "id-ID"  = "Indonesian"
    "ig-NG"  = "Igbo"
    "it-IT"  = "Italian"
    "ja-JP"  = "Japanese"
    "kk-KZ"  = "Kazakh"
    "ko-KR"  = "Korean"
    "mr-IN"  = "Marathi"
    "my-MM"  = "Burmese"
    "ne-NP"  = "Nepali"
    "om-ET"  = "Oromo"
    "pa-IN"  = "Punjabi"
    "pcm-NG" = "Nigerian Pidgin"
    "pl-PL"  = "Polish"
    "ps-AF"  = "Pashto"
    "pt-BR"  = "Portuguese (Brazil)"
    "ru-RU"  = "Russian"
    "sw-KE"  = "Swahili"
    "ta-IN"  = "Tamil"
    "te-IN"  = "Telugu"
    "th-TH"  = "Thai"
    "tr-TR"  = "Turkish"
    "uk-UA"  = "Ukrainian"
    "ur-PK"  = "Urdu"
    "uz-UZ"  = "Uzbek"
    "vi-VN"  = "Vietnamese"
    "yo-NG"  = "Yoruba"
    "yue-HK" = "Cantonese"
    "zh-CN"  = "Chinese (Simplified)"
}

function Get-EnglishLanguageName {
    param([string]$LocaleCode)

    if ($LanguageNameOverrides.ContainsKey($LocaleCode)) {
        return $LanguageNameOverrides[$LocaleCode]
    }

    try {
        $culture = [System.Globalization.CultureInfo]::GetCultureInfo($LocaleCode)
        if ($culture.EnglishName -and $culture.EnglishName -notmatch '^Unknown') {
            return $culture.EnglishName
        }
    } catch {
    }

    return $LocaleCode
}

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

$utf8 = [System.Text.UTF8Encoding]::new($false)

foreach ($file in $files) {
    $html = Get-Content -Path $file.FullName -Raw -Encoding utf8
    $plain = Convert-HtmlToPlainText -Html $html
    $outputPath = Join-Path $file.DirectoryName "full_description_text.txt"
    [System.IO.File]::WriteAllText($outputPath, $plain, $utf8)
    Write-Output $outputPath

    $localeCode = $file.Directory.Name
    $languageName = Get-EnglishLanguageName -LocaleCode $localeCode
    $languagePath = Join-Path $file.DirectoryName "language_name.txt"
    [System.IO.File]::WriteAllText($languagePath, "$languageName`n", $utf8)
    Write-Output $languagePath
}

Write-Output "Converted $($files.Count) description(s)."
