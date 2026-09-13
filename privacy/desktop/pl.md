[Languages](README.md)

# Polityka prywatności

Ostatnia aktualizacja: 13 września 2026 r.


**FTPS Server** by Siarhei Kuchuk

Nazwa aplikacji: FTPS Server
Imię i nazwisko dewelopera: Siarhei Kuchuk

Oprogramowanie to lokalny serwer FTPS (FTP przez TLS). Nie tworzy kont w chmurze.
Deweloper nie prowadzi serwera, który odbierałby Twoje pliki, hasła ani dane o użytkowaniu.

## Dane, których deweloper nie zbiera

Aplikacja nie zawiera reklam, analityki, zgłaszania awarii ani SDK śledzących. Deweloper nie zbiera, nie sprzedaje ani nie udostępnia danych osobowych.

## Dane przechowywane na komputerze

Ustawienia aplikacji (w tym nazwy użytkowników i hasła FTPS, port serwera, limity połączeń oraz opcjonalna ścieżka i hasło certyfikatu) są przechowywane tylko na tym komputerze:

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

Jeśli aplikacja tworzy certyfikat z podpisem własnym, jest on przechowywany tutaj:

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

Dzienniki serwera mogą być zapisywane tutaj:

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

Te wartości nie są wysyłane do dewelopera. Usunięcie aplikacji lub tych folderów je usuwa. Udostępnione **pliki** pozostają w folderach, które wybrałeś; aplikacja nie kopiuje ich na serwer dewelopera.

Foldery wybiera się systemowym wyborem folderów. Aplikacja udostępnia tylko foldery, do których udzielisz uprawnienia.

Serwer dewelopera nie jest używany do przechowywania Twoich danych.

## Korzystanie z sieci

### Sprawdzanie aktualizacji

Aplikacja może poprosić o najnowsze wydanie GitHub:

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub (Microsoft) otrzymuje zwykłe żądanie HTTPS (adres IP, user-agent, czas). Deweloper nie otrzymuje tego ruchu.

### Serwer FTPS

Gdy serwer działa, nasłuchuje w sieci lokalnej, aby skonfigurowani przez Ciebie klienci FTPS mogli odczytywać lub zapisywać udostępnione foldery przy użyciu ustawionych nazw i haseł. Ten ruch pozostaje między Twoimi urządzeniami (oraz każdym w sieci, kto ma te dane logowania). Deweloper nie jest stroną.

Odpowiadasz za to, kto może dotrzeć do portu, które foldery udostępniasz i jak silne są te hasła.

### Łącza, które otwierasz

Aplikacja może otworzyć te strony w przeglądarce systemowej. Te witryny mają własne polityki prywatności:

- Strona projektu: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- Licencja: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- Najnowsze wydanie: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## Inne zachowanie lokalne

Gdy serwer działa, aplikacja może poprosić system operacyjny o ograniczenie uśpienia, aby transfery mogły trwać.

## Dzieci

Aplikacja to sieciowy serwer plików. Nie jest przeznaczona dla dzieci poniżej 13. roku życia.

## Strony trzecie

GitHub przetwarza żądanie sprawdzania aktualizacji i strony, które otwierasz, jak powyżej. Deweloper nie otrzymuje tego ruchu.

## Zmiany

Aktualizacje tej polityki będą publikowane w tym pliku w repozytorium projektu.

## Kontakt

Nazwa aplikacji: FTPS Server
Imię i nazwisko dewelopera: Siarhei Kuchuk

Pytania: [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
