# FtpsServerAvalonia Localization Guide

This document explains how the localization system works in the FtpsServerAvalonia application.

## Overview

The application has been prepared for localization using .NET's resource file (`.resx`) system. All user-facing strings have been extracted into resource files shared from the FtpsServerWindows project.

## Supported Languages

The application currently supports the following languages:

1. **English (Default)** - `Strings.resx`
2. **Russian** - `Strings.ru.resx`
3. **Spanish** - `Strings.es.resx`
4. **Chinese (Simplified)** - `Strings.zh-Hans.resx`
5. **German** - `Strings.de.resx`
6. **French** - `Strings.fr.resx`
7. **Japanese** - `Strings.ja.resx`
8. **Portuguese (Brazilian)** - `Strings.pt-BR.resx`
9. **Korean** - `Strings.ko.resx`

## File Structure

```
FtpsServerAvalonia/
└── Resources/
    ├── Strings.resx                 (Default - English)
    ├── Strings.Designer.cs          (Auto-generated code file)
    ├── Strings.ru.resx              (Russian)
    ├── Strings.es.resx              (Spanish)
    ├── Strings.zh-Hans.resx         (Chinese Simplified)
    ├── Strings.de.resx              (German)
    ├── Strings.fr.resx              (French)
    ├── Strings.ja.resx              (Japanese)
    ├── Strings.pt-BR.resx           (Portuguese Brazilian)
    └── Strings.ko.resx              (Korean)
```

## Avalonia-Specific Implementation

### Usage in Code

```csharp
using FtpsServerAvalonia.Resources;

// Simple string
string title = Strings.AppTitle;

// Formatted string
string error = string.Format(Strings.ErrorIncompleteUserFormat, username);
```

### Usage in AXAML

Avalonia uses slightly different syntax than WPF:

```xml
xmlns:res="using:FtpsServerAvalonia.Resources"

<!-- Simple binding -->
<TextBlock Text="{x:Static res:Strings.AppTitle}" />

<!-- Button content -->
<Button Content="{x:Static res:Strings.MenuStart}" />
```

**Note the difference:**
- Namespace declaration: `xmlns:res="using:FtpsServerAvalonia.Resources"` (Avalonia uses `using:` instead of `clr-namespace:`)
- Binding syntax remains the same: `{x:Static res:Strings.PropertyName}`

## Remaining Localization Work

The following files have been **partially updated** and need completion:

### ✅ Completed
1. **MainWindow.axaml** - Title and main strings updated
2. **MainWindow.axaml.cs** - All error messages localized
3. **Resources folder** - All 9 language files copied
4. **Strings.Designer.cs** - Namespace updated to FtpsServerAvalonia.Resources
5. **FtpsServerAvalonia.csproj** - Resource configuration added

### ⏳ Needs Completion

#### 1. MainMenuControl (Controls/MainMenuControl.axaml)
Add namespace and update strings:
```xml
xmlns:res="using:FtpsServerAvalonia.Resources"

<!-- Update these: -->
Header="{x:Static res:Strings.MenuHeader}"
Header="{x:Static res:Strings.MenuWebSite}"
Header="{x:Static res:Strings.MenuLicense}"
Header="{x:Static res:Strings.MenuLogs}"
Header="{x:Static res:Strings.MenuStart}"
Header="{x:Static res:Strings.MenuStop}"
```

#### 2. MainMenuControl.axaml.cs
```csharp
using FtpsServerAvalonia.Resources;

// Line ~27:
AboutMenuItem.Header = string.Format(Strings.MenuAboutFormat, CopyrightInfo.Version.ToString(3));
```

#### 3. ServerConfigurationControl (Controls/ServerConfigurationControl.axaml)
Add namespace and update all labels:
```xml
xmlns:res="using:FtpsServerAvalonia.Resources"

Text="{x:Static res:Strings.ConfigTitle}"
Text="{x:Static res:Strings.ConfigAddress}"
<!-- ... update all Config* strings -->
```

#### 4. ServerConfigurationControl.axaml.cs
No file dialog in Avalonia (uses async pattern), but update if present.

#### 5. UserItemControl (Controls/UserItemControl.axaml)
```xml
xmlns:res="using:FtpsServerAvalonia.Resources"

Text="{x:Static res:Strings.UserUsername}"
Text="{x:Static res:Strings.UserPassword}"
Text="{x:Static res:Strings.UserFolder}"
Content="{x:Static res:Strings.ConfigBrowse}"
Content="{x:Static res:Strings.UserReadonly}"
Content="{x:Static res:Strings.UserDelete}"
```

#### 6. UserItemControl.axaml.cs
```csharp
using FtpsServerAvalonia.Resources;

// Update folder picker title:
Title = string.Format(Strings.UserSelectFolderFormat, user.Login)
```

#### 7. UpdateCheckExpanderView (Controls/UpdateCheckExpanderView.axaml)
```xml
xmlns:res="using:FtpsServerAvalonia.Resources"

Content="{x:Static res:Strings.UpdateDownload}"
```

#### 8. UpdateCheckExpanderView.axaml.cs
```csharp
using FtpsServerAvalonia.Resources;

// Line ~26:
this.updateNewsTitle.Text = string.Format(Strings.UpdateAvailableFormat, update.Version);
```

## How It Works

The application automatically selects the appropriate language based on the system locale:
- If a matching resource file exists, it will be used
- If no matching resource file exists, it falls back to English

## Multi-Platform Considerations

### Desktop (Windows/Linux/macOS)
- Full localization support via .resx files
- System culture detection works automatically

### Android/iOS
- Resource files work the same way
- May need additional configuration in platform-specific projects
- Test on actual devices for proper culture detection

### Browser (WebAssembly)
- .resx files are supported
- Browser language preference is used
- Satellite assemblies are downloaded as needed

## Testing Localization

### Desktop
Change system display language or set culture in code:
```csharp
// In App.axaml.cs or Program.cs
System.Globalization.CultureInfo.CurrentUICulture =
    new System.Globalization.CultureInfo("ru");
```

### Browser
Test with browser language preferences:
1. Chrome: Settings → Languages
2. Firefox: Settings → Language
3. Edge: Settings → Languages

## Adding a New Language

1. Copy `Strings.resx` to `Strings.[culture-code].resx`
2. Translate all values (keep keys the same)
3. Rebuild the project
4. Satellite assemblies are automatically generated

## Notes

- **Log messages are NOT localized** - they remain in English for debugging
- All user-facing UI elements should be localized
- The application uses standard .NET localization framework
- Resources are shared conceptually with FtpsServerWindows but maintained separately
- No runtime language switching is currently implemented

## Complete Localization Checklist

To finish localization, update these files:

- [ ] Controls/MainMenuControl.axaml
- [ ] Controls/MainMenuControl.axaml.cs
- [ ] Controls/ServerConfigurationControl.axaml
- [ ] Controls/ServerConfigurationControl.axaml.cs (if file dialogs present)
- [ ] Controls/UserItemControl.axaml
- [ ] Controls/UserItemControl.axaml.cs
- [ ] Controls/UpdateCheckExpanderView.axaml
- [ ] Controls/UpdateCheckExpanderView.axaml.cs

Each file needs:
1. Add `xmlns:res="using:FtpsServerAvalonia.Resources"` namespace
2. Replace hardcoded strings with `{x:Static res:Strings.PropertyName}`
3. In code-behind, add `using FtpsServerAvalonia.Resources;`
4. Replace hardcoded strings with `Strings.PropertyName`

## Reference

For complete implementation examples, refer to the FtpsServerWindows project which has full localization implemented.
