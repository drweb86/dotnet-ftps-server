[Languages](README.md)

# Política de privacidad

Última actualización: 13 de septiembre de 2026


**FTPS Server** by Siarhei Kuchuk

Nombre de la aplicación: FTPS Server
Nombre del desarrollador: Siarhei Kuchuk

El software es un servidor FTPS local (FTP sobre TLS). No crea cuentas en la nube.
El desarrollador no opera un servidor que reciba sus archivos, contraseñas o datos de uso.

## Datos que el desarrollador no recopila

La aplicación no incluye anuncios, analítica, informes de fallos ni SDK de seguimiento. El desarrollador no recopila, vende ni comparte datos personales.

## Datos almacenados en su equipo

La configuración de la aplicación (incluidos nombres de usuario y contraseñas de FTPS, el puerto del servidor, los límites de conexión y la ruta y contraseña opcionales del certificado) se guarda solo en este equipo:

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

Si la aplicación crea un certificado autofirmado, se almacena aquí:

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

Los registros del servidor pueden escribirse aquí:

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

Esos valores no se envían al desarrollador. Quitar la aplicación o esas carpetas los elimina. Los **archivos** compartidos permanecen en las carpetas que eligió; la aplicación no los copia a un servidor del desarrollador.

Las carpetas se eligen con el selector de carpetas del sistema. La aplicación comparte solo las carpetas para las que usted concede permiso.

No se usa ningún servidor del desarrollador para almacenar sus datos.

## Uso de la red

### Comprobación de actualizaciones

La aplicación puede solicitar la última versión de GitHub:

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub (Microsoft) recibe una solicitud HTTPS normal (dirección IP, user-agent, hora). El desarrollador no recibe ese tráfico.

### Servidor FTPS

Mientras el servidor está en ejecución, escucha en su red local para que los clientes FTPS que configure puedan leer o escribir en las carpetas compartidas, con los nombres de usuario y contraseñas que usted defina. Ese tráfico permanece entre sus dispositivos (y cualquiera en la red que tenga esas credenciales). El desarrollador no participa.

Usted es responsable de quién puede alcanzar el puerto, de qué carpetas comparte y de lo robustas que sean esas contraseñas.

### Enlaces que abre

La aplicación puede abrir estas páginas en el navegador del sistema. Esos sitios tienen sus propias políticas de privacidad:

- Página del proyecto: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- Licencia: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- Última versión: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## Otro comportamiento local

Mientras el servidor está en ejecución, la aplicación puede pedir al sistema operativo que reduzca la suspensión para que las transferencias continúen.

## Niños

La aplicación es un servidor de archivos en red. No está dirigida a menores de 13 años.

## Terceros

GitHub procesa la solicitud de actualización y las páginas que abre, como se indica arriba. El desarrollador no recibe ese tráfico.

## Cambios

Las actualizaciones de esta política se publicarán en este archivo en el repositorio del proyecto.

## Contacto

Nombre de la aplicación: FTPS Server
Nombre del desarrollador: Siarhei Kuchuk

Preguntas: [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
