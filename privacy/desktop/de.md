[Languages](README.md)

# Datenschutzerklärung

Zuletzt aktualisiert: 13. September 2026


**FTPS Server** by Siarhei Kuchuk

Anwendungsname: FTPS Server
Entwicklername: Siarhei Kuchuk

Die Software ist ein lokaler FTPS-Server (FTP über TLS). Sie erstellt keine Cloud-Konten.
Der Entwickler betreibt kein Backend, das Ihre Dateien, Passwörter oder Nutzungsdaten empfängt.

## Daten, die der Entwickler nicht erhebt

Die App enthält keine Werbung, keine Analyse-, Absturzmelde- oder Tracking-SDKs. Der Entwickler erhebt, verkauft oder teilt keine personenbezogenen Daten.

## Daten auf Ihrem Computer

Die Anwendungseinstellungen (einschließlich FTPS-Benutzernamen und -Passwörter, Serverport, Verbindungslimits sowie optionalem Zertifikatspfad und -passwort) werden nur auf diesem Computer gespeichert:

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

Wenn die App ein selbstsigniertes Zertifikat erstellt, wird es hier abgelegt:

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

Serverprotokolle können hier geschrieben werden:

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

Diese Werte werden nicht an den Entwickler hochgeladen. Das Entfernen der App oder dieser Ordner löscht sie. Freigegebene **Dateien** bleiben in den von Ihnen gewählten Ordnern; die App kopiert sie nicht auf einen Server des Entwicklers.

Ordner werden mit dem System-Ordnerauswahlfenster gewählt. Die App gibt nur Ordner frei, für die Sie die Berechtigung erteilen.

Es wird kein Server des Entwicklers verwendet, um Ihre Daten zu speichern.

## Netzwerknutzung

### Update-Prüfung

Die App kann die neueste GitHub-Version anfordern:

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub (Microsoft) empfängt eine normale HTTPS-Anfrage (IP-Adresse, User-Agent, Zeit). Der Entwickler empfängt diesen Verkehr nicht.

### FTPS-Server

Solange der Server läuft, lauscht er in Ihrem lokalen Netzwerk, damit von Ihnen eingerichtete FTPS-Clients die freigegebenen Ordner mit den von Ihnen festgelegten Benutzernamen und Passwörtern lesen oder beschreiben können. Dieser Verkehr bleibt zwischen Ihren Geräten (und jedem im Netz, der diese Zugangsdaten hat). Der Entwickler ist nicht beteiligt.

Sie sind dafür verantwortlich, wer den Port erreichen kann, welche Ordner Sie freigeben und wie stark diese Passwörter sind.

### Links, die Sie öffnen

Die App kann diese Seiten im Systembrowser öffnen. Diese Sites haben eigene Datenschutzrichtlinien:

- Projekt-Homepage: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- Lizenz: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- Neueste Version: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## Weiteres lokales Verhalten

Solange der Server läuft, kann die App das Betriebssystem bitten, den Ruhezustand zu reduzieren, damit Übertragungen weiterlaufen.

## Kinder

Die App ist ein Netzwerk-Dateiserver. Sie richtet sich nicht an Kinder unter 13 Jahren.

## Dritte

GitHub verarbeitet die Update-Prüfung und die von Ihnen geöffneten Seiten wie oben beschrieben. Der Entwickler empfängt diesen Verkehr nicht.

## Änderungen

Aktualisierungen dieser Richtlinie werden in dieser Datei im Projekt-Repository veröffentlicht.

## Kontakt

Anwendungsname: FTPS Server
Entwicklername: Siarhei Kuchuk

Fragen: [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
