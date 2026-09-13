[Languages](README.md)

# Politique de confidentialité

Dernière mise à jour : 13 septembre 2026


**FTPS Server** by Siarhei Kuchuk

Nom de l’application: FTPS Server
Nom du développeur: Siarhei Kuchuk

Le logiciel est un serveur FTPS local (FTP sur TLS). Il ne crée pas de comptes cloud.
Le développeur n’exploite aucun serveur qui reçoive vos fichiers, mots de passe ou données d’utilisation.

## Données que le développeur ne collecte pas

L’application n’inclut ni publicités, ni outils d’analyse, ni rapports de plantage, ni SDK de suivi. Le développeur ne collecte, ne vend ni ne partage de données personnelles.

## Données stockées sur votre ordinateur

Les paramètres de l’application (identifiants et mots de passe FTPS, port du serveur, limites de connexion, chemin et mot de passe de certificat facultatifs) sont stockés uniquement sur cet ordinateur :

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

Si l’application crée un certificat auto-signé, il est stocké ici :

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

Les journaux du serveur peuvent être écrits ici :

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

Ces valeurs ne sont pas envoyées au développeur. La suppression de l’application ou de ces dossiers les efface. Les **fichiers** partagés restent dans les dossiers que vous avez choisis ; l’application ne les copie pas vers un serveur du développeur.

Les dossiers sont choisis avec le sélecteur de dossiers du système. L’application ne partage que les dossiers pour lesquels vous accordez l’accès.

Aucun serveur du développeur n’est utilisé pour stocker vos données.

## Utilisation du réseau

### Vérification des mises à jour

L’application peut demander la dernière version GitHub :

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub (Microsoft) reçoit une requête HTTPS normale (adresse IP, user-agent, heure). Le développeur ne reçoit pas ce trafic.

### Serveur FTPS

Tant que le serveur est en cours d’exécution, il écoute sur votre réseau local afin que les clients FTPS que vous configurez puissent lire ou écrire dans les dossiers partagés, avec les identifiants et mots de passe que vous définissez. Ce trafic reste entre vos appareils (et toute personne sur le réseau qui possède ces identifiants). Le développeur n’y participe pas.

Vous êtes responsable de qui peut atteindre le port, des dossiers que vous partagez et de la robustesse de ces mots de passe.

### Liens que vous ouvrez

L’application peut ouvrir ces pages dans le navigateur système. Ces sites ont leurs propres politiques de confidentialité :

- Page d’accueil du projet: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- Licence: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- Dernière version: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## Autre comportement local

Tant que le serveur tourne, l’application peut demander au système d’exploitation de limiter la mise en veille pour que les transferts continuent.

## Enfants

L’application est un serveur de fichiers réseau. Elle ne s’adresse pas aux enfants de moins de 13 ans.

## Tiers

GitHub traite la requête de vérification des mises à jour et les pages que vous ouvrez, comme ci-dessus. Le développeur ne reçoit pas ce trafic.

## Modifications

Les mises à jour de cette politique seront publiées dans ce fichier dans le dépôt du projet.

## Contact

Nom de l’application: FTPS Server
Nom du développeur: Siarhei Kuchuk

Questions : [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
