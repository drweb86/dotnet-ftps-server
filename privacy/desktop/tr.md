[Languages](README.md)

# Gizlilik politikası

Son güncelleme: 13 Eylül 2026


**FTPS Server** by Siarhei Kuchuk

Uygulama adı: FTPS Server
Geliştirici adı: Siarhei Kuchuk

Yazılım yerel bir FTPS (TLS üzerinden FTP) sunucusudur. Bulut hesapları oluşturmaz.
Geliştirici; dosyalarınızı, parolalarınızı veya kullanım verilerinizi alan bir sunucu işletmez.

## Geliştiricinin toplamadığı veriler

Uygulamada reklam, analitik, çökme bildirimi veya izleme SDK’sı yoktur. Geliştirici kişisel verileri toplamaz, satmaz veya paylaşmaz.

## Bilgisayarınızda saklanan veriler

Uygulama ayarları (FTPS kullanıcı adları ve parolaları, sunucu bağlantı noktası, bağlantı sınırları ve isteğe bağlı sertifika yolu ile parolası dahil) yalnızca bu bilgisayarda saklanır:

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

Uygulama kendinden imzalı bir sertifika oluşturursa şurada saklanır:

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

Sunucu günlükleri şuraya yazılabilir:

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

Bu değerler geliştiriciye yüklenmez. Uygulamayı veya bu klasörleri kaldırmak onları siler. Paylaşılan **dosyalar** seçtiğiniz klasörlerde kalır; uygulama bunları bir geliştirici sunucusuna kopyalamaz.

Klasörler sistem klasör seçicisiyle seçilir. Uygulama yalnızca izin verdiğiniz klasörleri paylaşır.

Verilerinizi saklamak için bir geliştirici sunucusu kullanılmaz.

## Ağ kullanımı

### Güncelleme denetimi

Uygulama en son GitHub sürümünü isteyebilir:

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub (Microsoft) olağan bir HTTPS isteği (IP adresi, user-agent, zaman) alır. Geliştirici bu trafiği almaz.

### FTPS sunucusu

Sunucu çalışırken yerel ağınızda dinler; böylece yapılandırdığınız FTPS istemcileri, belirlediğiniz kullanıcı adları ve parolalarla paylaştığınız klasörleri okuyabilir veya yazabilir. Bu trafik cihazlarınız arasında (ve bu kimlik bilgilerine sahip ağdaki herkesle) kalır. Geliştirici taraf değildir.

Bağlantı noktasına kimin ulaşabileceği, hangi klasörleri paylaştığınız ve bu parolaların ne kadar güçlü olduğu sizin sorumluluğunuzdadır.

### Açtığınız bağlantılar

Uygulama bu sayfaları sistem tarayıcısında açabilir. Bu sitelerin kendi gizlilik politikaları vardır:

- Proje ana sayfası: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- Lisans: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- En son sürüm: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## Diğer yerel davranış

Sunucu çalışırken uygulama, aktarımların sürmesi için işletim sisteminden uykuyu azaltmasını isteyebilir.

## Çocuklar

Uygulama bir ağ dosya sunucusudur. 13 yaşın altındaki çocuklara yönelik değildir.

## Üçüncü taraflar

GitHub, yukarıda belirtildiği gibi güncelleme denetimi isteğini ve açtığınız sayfaları işler. Geliştirici bu trafiği almaz.

## Değişiklikler

Bu politikanın güncellemeleri proje deposundaki bu dosyada yayımlanır.

## İletişim

Uygulama adı: FTPS Server
Geliştirici adı: Siarhei Kuchuk

Sorular: [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
