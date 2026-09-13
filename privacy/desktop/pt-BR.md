[Languages](README.md)

# Política de privacidade

Última atualização: 13 de setembro de 2026


**FTPS Server** by Siarhei Kuchuk

Nome do aplicativo: FTPS Server
Nome do desenvolvedor: Siarhei Kuchuk

O software é um servidor FTPS local (FTP sobre TLS). Ele não cria contas na nuvem.
O desenvolvedor não opera um servidor que receba seus arquivos, senhas ou dados de uso.

## Dados que o desenvolvedor não coleta

O aplicativo não inclui anúncios, análise, relatórios de falha nem SDKs de rastreamento. O desenvolvedor não coleta, vende nem compartilha dados pessoais.

## Dados armazenados no seu computador

As configurações do aplicativo (incluindo nomes de usuário e senhas FTPS, a porta do servidor, limites de conexão e o caminho e a senha opcionais do certificado) são armazenadas somente neste computador:

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

Se o aplicativo criar um certificado autoassinado, ele é armazenado aqui:

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

Os logs do servidor podem ser gravados aqui:

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

Esses valores não são enviados ao desenvolvedor. Remover o aplicativo ou essas pastas os exclui. Os **arquivos** compartilhados permanecem nas pastas que você escolheu; o aplicativo não os copia para um servidor do desenvolvedor.

As pastas são escolhidas com o seletor de pastas do sistema. O aplicativo compartilha somente as pastas às quais você concede acesso.

Nenhum servidor do desenvolvedor é usado para armazenar seus dados.

## Uso da rede

### Verificação de atualização

O aplicativo pode solicitar a versão mais recente no GitHub:

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

O GitHub (Microsoft) recebe uma solicitação HTTPS normal (endereço IP, user-agent, horário). O desenvolvedor não recebe esse tráfego.

### Servidor FTPS

Enquanto o servidor está em execução, ele escuta na sua rede local para que os clientes FTPS que você configurar possam ler ou gravar nas pastas compartilhadas, com os nomes de usuário e senhas que você definir. Esse tráfego permanece entre seus dispositivos (e qualquer pessoa na rede que tenha essas credenciais). O desenvolvedor não participa.

Você é responsável por quem pode alcançar a porta, quais pastas você compartilha e quão fortes são essas senhas.

### Links que você abre

O aplicativo pode abrir estas páginas no navegador do sistema. Esses sites têm suas próprias políticas de privacidade:

- Página do projeto: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- Licença: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- Versão mais recente: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## Outro comportamento local

Enquanto o servidor está em execução, o aplicativo pode pedir ao sistema operacional para reduzir a suspensão para que as transferências continuem.

## Crianças

O aplicativo é um servidor de arquivos em rede. Não é dirigido a crianças menores de 13 anos.

## Terceiros

O GitHub processa a solicitação de verificação de atualização e as páginas que você abre, conforme acima. O desenvolvedor não recebe esse tráfego.

## Alterações

Atualizações desta política serão publicadas neste arquivo no repositório do projeto.

## Contato

Nome do aplicativo: FTPS Server
Nome do desenvolvedor: Siarhei Kuchuk

Perguntas: [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
