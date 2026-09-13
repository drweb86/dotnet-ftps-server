[Languages](README.md)

# 개인정보 처리방침

최종 업데이트: 2026년 9월 13일


**FTPS Server** by Siarhei Kuchuk

애플리케이션 이름: FTPS Server
개발자 이름: Siarhei Kuchuk

본 소프트웨어는 로컬 FTPS(TLS 위의 FTP) 서버입니다. 클라우드 계정을 만들지 않습니다.
개발자는 파일, 비밀번호, 사용 데이터를 받는 백엔드를 운영하지 않습니다.

## 개발자가 수집하지 않는 데이터

앱에는 광고, 분석, 오류 보고, 추적 SDK가 없습니다. 개발자는 개인정보를 수집, 판매, 공유하지 않습니다.

## 컴퓨터에 저장되는 데이터

앱 설정(FTPS 사용자 이름과 비밀번호, 서버 포트, 연결 한도, 선택적 인증서 경로와 비밀번호 포함)은 이 컴퓨터에만 저장됩니다:

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

앱이 자체 서명 인증서를 만들면 다음 위치에 저장됩니다:

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

서버 로그는 다음 위치에 기록될 수 있습니다:

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

해당 값은 개발자에게 업로드되지 않습니다. 앱이나 해당 폴더를 제거하면 삭제됩니다. 공유한 **파일**은 선택한 폴더에 남으며, 앱은 개발자 서버로 복사하지 않습니다.

폴더는 시스템 폴더 선택기로 고릅니다. 앱은 권한을 부여한 폴더만 공유합니다.

개발자 서버는 사용자 데이터를 저장하는 데 사용되지 않습니다.

## 네트워크 사용

### 업데이트 확인

앱은 GitHub의 최신 릴리스를 요청할 수 있습니다:

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub(Microsoft)는 일반적인 HTTPS 요청(IP 주소, user-agent, 시각)을 받습니다. 개발자는 해당 트래픽을 받지 않습니다.

### FTPS 서버

서버가 실행 중일 때 로컬 네트워크에서 수신 대기하여, 구성한 FTPS 클라이언트가 설정한 사용자 이름과 비밀번호로 공유 폴더를 읽거나 쓸 수 있습니다. 해당 트래픽은 기기 사이(그리고 그 자격 증명을 가진 네트워크상의 누구든지)에만 머무릅니다. 개발자는 당사자가 아닙니다.

포트에 누가 접근할 수 있는지, 어떤 폴더를 공유하는지, 비밀번호가 얼마나 강한지는 사용자의 책임입니다.

### 여는 링크

앱은 시스템 브라우저에서 다음 페이지를 열 수 있습니다. 해당 사이트에는 자체 개인정보 처리방침이 있습니다:

- 프로젝트 홈페이지: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- 라이선스: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- 최신 릴리스: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## 기타 로컬 동작

서버가 실행 중일 때 앱은 전송이 계속되도록 운영 체제에 절전을 줄이도록 요청할 수 있습니다.

## 아동

본 앱은 네트워크 파일 서버이며 13세 미만 아동을 대상으로 하지 않습니다.

## 제3자

GitHub는 위와 같이 업데이트 확인 요청과 연 페이지를 처리합니다. 개발자는 해당 트래픽을 받지 않습니다.

## 변경

이 방침의 업데이트는 프로젝트 저장소의 이 파일에 게시됩니다.

## 문의

애플리케이션 이름: FTPS Server
개발자 이름: Siarhei Kuchuk

질문: [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
