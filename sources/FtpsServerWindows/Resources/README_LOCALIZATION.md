# FtpsServerWindows Localization Guide

This document explains how the localization system works in the FtpsServerWindows application.

## Overview

The application has been fully prepared for localization using .NET's resource file (`.resx`) system. All user-facing strings have been extracted into resource files and are ready for translation.

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
FtpsServerWindows/
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

## How It Works

### Resource File Structure

Each `.resx` file contains key-value pairs where:
- **Key** (name): A unique identifier for the string (e.g., `AppTitle`, `MenuStart`)
- **Value**: The translated text in the target language
- **Comment**: Optional context for translators (e.g., format placeholders)

### Usage in Code

The resource strings are accessed through the strongly-typed `Strings` class:

```csharp
using FtpsServerWindows.Resources;

// Simple string
string title = Strings.AppTitle;

// Formatted string
string error = string.Format(Strings.ErrorIncompleteUserFormat, username);
```

### Usage in XAML

Resource strings are bound using static markup extensions:

```xaml
xmlns:res="clr-namespace:FtpsServerWindows.Resources"

<!-- Simple binding -->
<TextBlock Text="{x:Static res:Strings.AppTitle}" />

<!-- Button content -->
<Button Content="{x:Static res:Strings.MenuStart}" />
```

## Language Selection

The application automatically selects the appropriate language based on the Windows system locale:

- If a matching resource file exists (e.g., `Strings.ru.resx` for Russian), it will be used
- If no matching resource file exists, it falls back to the default English (`Strings.resx`)

### Supported Culture Codes

- `ru` or `ru-RU` → Russian
- `es` or `es-ES`, `es-MX`, etc. → Spanish
- `zh-Hans` or `zh-CN` → Chinese (Simplified)
- `de` or `de-DE` → German
- `fr` or `fr-FR` → French
- `ja` or `ja-JP` → Japanese
- `pt-BR` → Portuguese (Brazilian)
- `ko` or `ko-KR` → Korean

## Adding a New Language

To add support for a new language:

1. **Create a new resource file**:
   - Copy `Strings.resx` to `Strings.[culture-code].resx`
   - Example: `Strings.it.resx` for Italian

2. **Translate all values**:
   - Keep the **keys** (names) exactly the same
   - Translate only the **values**
   - Preserve format placeholders like `{0}`, `{1}` in the same positions

3. **Update the project file** (if needed):
   - The `.csproj` file already has wildcard patterns to include all `Strings.*.resx` files
   - No manual changes should be needed

4. **Rebuild the project**:
   - Visual Studio will automatically generate the satellite assemblies

## Resource String Categories

### Main Window
- `AppTitle` - Application window title
- `UsersTab` - Users section label
- `AddUserButton` - Add user button

### Main Menu
- `MenuHeader` - Menu header
- `MenuWebSite` - Website menu item
- `MenuLicense` - License menu item
- `MenuLogs` - Logs menu item
- `MenuStart` - Start server button
- `MenuStop` - Stop server button
- `MenuAboutFormat` - About menu with version (format: `{0}` = version)

### Server Configuration
- `ConfigTitle` - Configuration section title
- `ConfigAddress` - Address label
- `ConfigAccessInfo` - Access information text
- `ConfigName` - Computer name label
- `ConfigNetworkIPs` - Network IPs label
- `ConfigNetwork` - Network label
- `ConfigIPAddresses` - IP addresses label
- `ConfigMaxConnections` - Max connections label
- `ConfigUseSelfSigned` - Use self-signed certificate checkbox
- `ConfigBrowse` - Browse button
- `ConfigCertFile` - Certificate file label
- `ConfigCertPassword` - Certificate password label
- `ConfigSelectCertTitle` - Certificate selection dialog title
- `ConfigCertFilter` - Certificate file filter for dialog

### User Management
- `UserUsername` - Username label
- `UserPassword` - Password label
- `UserFolder` - Folder label
- `UserReadonly` - Read-only checkbox
- `UserDelete` - Delete button
- `UserSelectFolderFormat` - Folder selection dialog title (format: `{0}` = username)

### Updates
- `UpdateDownload` - Download button
- `UpdateAvailableFormat` - Update notification (format: `{0}` = version)

### Numeric Controls
- `NumericUp` - Up arrow (▲)
- `NumericDown` - Down arrow (▼)

### Error Messages
- `ErrorTitle` - Error dialog title
- `ErrorSelectCertificate` - Certificate not selected error
- `ErrorAddUser` - No users error
- `ErrorIncompleteUserFormat` - Incomplete user info error (format: `{0}` = username)
- `ErrorStartServerFormat` - Server start error (format: `{0}` = error message)
- `ErrorStopServerFormat` - Server stop error (format: `{0}` = error message)

## Translation Guidelines

1. **Preserve Format Placeholders**:
   - Always keep `{0}`, `{1}`, etc. in the same relative position
   - Example: `"User {0} has errors"` → `"El usuario {0} tiene errores"`

2. **Context Matters**:
   - Read the `<comment>` tags in the resource files for context
   - Consider the UI space constraints (short labels fit better)

3. **Consistency**:
   - Use consistent terminology across all strings
   - Follow the target language's conventions for capitalization

4. **Special Characters**:
   - Arrows (▲, ▼) and symbols (⏵, ⏹) should remain unchanged
   - They are Unicode symbols that work across languages

## Testing Localization

To test a specific language:

1. **Change Windows Display Language**:
   - Go to Windows Settings → Time & Language → Language
   - Add and set the desired language as default
   - Restart the application

2. **Or use CultureInfo in code** (for development):
   ```csharp
   // Add at the start of App.xaml.cs
   System.Threading.Thread.CurrentThread.CurrentUICulture =
       new System.Globalization.CultureInfo("ru");
   ```

## Maintaining Translations

When adding new features:

1. Add new strings to `Strings.resx` with descriptive keys
2. Rebuild to regenerate `Strings.Designer.cs`
3. Add corresponding translations to all `Strings.[culture].resx` files
4. Use the new strings in XAML or C# code
5. Test across languages

## Tools for Editing .resx Files

- **Visual Studio**: Built-in resource editor
- **ResX Resource Manager**: Free Visual Studio extension
- **Zeta Resource Editor**: Standalone .resx editor
- **Any XML/Text Editor**: `.resx` files are XML-based

## Notes

- **Log messages are NOT localized** as requested - they remain in English for debugging purposes
- All user-facing UI elements have been localized
- The application uses the standard .NET localization framework
- Satellite assemblies are automatically generated during build
- No runtime language switching is currently implemented (uses system locale only)

## Future Enhancements

Potential improvements for the localization system:

1. Add a language selector in the UI
2. Implement runtime culture switching without restart
3. Add right-to-left (RTL) layout support for Arabic/Hebrew
4. Create a translation validation tool
5. Add localization for tooltips (if added in future)
