[Languages](README.md)

# Informativa sulla privacy

Ultimo aggiornamento: 13 settembre 2026


**FTPS Server** by Siarhei Kuchuk

Nome dell’applicazione: FTPS Server
Nome dello sviluppatore: Siarhei Kuchuk

Il software è un server FTPS locale (FTP su TLS). Non crea account cloud.
Lo sviluppatore non gestisce un backend che riceva i tuoi file, le password o i dati di utilizzo.

## Dati che lo sviluppatore non raccoglie

L’app non include pubblicità, analisi, segnalazioni di arresti anomali né SDK di tracciamento. Lo sviluppatore non raccoglie, vende né condivide dati personali.

## Dati memorizzati sul computer

Le impostazioni dell’applicazione (inclusi nomi utente e password FTPS, porta del server, limiti di connessione e percorso e password opzionali del certificato) sono salvate solo su questo computer:

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

Se l’app crea un certificato autofirmato, viene salvato qui:

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

I registri del server possono essere scritti qui:

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

Quei valori non vengono inviati allo sviluppatore. La rimozione dell’app o di quelle cartelle li elimina. I **file** condivisi restano nelle cartelle che hai scelto; l’app non li copia su un server dello sviluppatore.

Le cartelle si scelgono con il selettore di cartelle di sistema. L’app condivide solo le cartelle per le quali concedi l’accesso.

Non viene usato alcun server dello sviluppatore per archiviare i tuoi dati.

## Uso della rete

### Controllo aggiornamenti

L’app può richiedere l’ultima release GitHub:

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub (Microsoft) riceve una normale richiesta HTTPS (indirizzo IP, user-agent, ora). Lo sviluppatore non riceve quel traffico.

### Server FTPS

Mentre il server è in esecuzione, è in ascolto sulla rete locale in modo che i client FTPS che configuri possano leggere o scrivere nelle cartelle condivise, con i nomi utente e le password che imposti. Quel traffico resta tra i tuoi dispositivi (e chiunque sulla rete abbia quelle credenziali). Lo sviluppatore non ne è parte.

Sei responsabile di chi può raggiungere la porta, di quali cartelle condividi e di quanto siano robuste quelle password.

### Collegamenti che apri

L’app può aprire queste pagine nel browser di sistema. Quei siti hanno le proprie informative sulla privacy:

- Home del progetto: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- Licenza: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- Ultima versione: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## Altro comportamento locale

Mentre il server è in esecuzione, l’app può chiedere al sistema operativo di ridurre la sospensione così i trasferimenti possono continuare.

## Minori

L’app è un server di file in rete. Non è destinata a minori di 13 anni.

## Terze parti

GitHub elabora la richiesta di aggiornamento e le pagine che apri, come sopra. Lo sviluppatore non riceve quel traffico.

## Modifiche

Gli aggiornamenti di questa informativa saranno pubblicati in questo file nel repository del progetto.

## Contatti

Nome dell’applicazione: FTPS Server
Nome dello sviluppatore: Siarhei Kuchuk

Domande: [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
