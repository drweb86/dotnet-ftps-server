[Languages](README.md)

# Kebijakan privasi

Terakhir diperbarui: 13 September 2026


**FTPS Server** by Siarhei Kuchuk

Nama aplikasi: FTPS Server
Nama pengembang: Siarhei Kuchuk

Perangkat lunak ini adalah server FTPS lokal (FTP melalui TLS). Perangkat lunak ini tidak membuat akun cloud.
Pengembang tidak mengoperasikan backend yang menerima berkas, kata sandi, atau data penggunaan Anda.

## Data yang tidak dikumpulkan pengembang

Aplikasi ini tidak menyertakan iklan, analitik, pelapor kerusakan, atau SDK pelacakan. Pengembang tidak mengumpulkan, menjual, atau membagikan data pribadi.

## Data yang disimpan di komputer Anda

Pengaturan aplikasi (termasuk nama pengguna dan kata sandi FTPS, port server, batas koneksi, serta jalur dan kata sandi sertifikat opsional) hanya disimpan di komputer ini:

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

Jika aplikasi membuat sertifikat yang ditandatangani sendiri, sertifikat itu disimpan di sini:

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

Log server dapat ditulis di sini:

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

Nilai-nilai itu tidak diunggah ke pengembang. Menghapus aplikasi atau folder tersebut akan menghapusnya. **Berkas** yang dibagikan tetap di folder yang Anda pilih; aplikasi tidak menyalinnya ke server pengembang.

Folder dipilih dengan pemilih folder sistem. Aplikasi hanya membagikan folder yang Anda izinkan.

Tidak ada server pengembang yang digunakan untuk menyimpan data Anda.

## Penggunaan jaringan

### Pemeriksaan pembaruan

Aplikasi dapat meminta rilis GitHub terbaru:

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub (Microsoft) menerima permintaan HTTPS biasa (alamat IP, user-agent, waktu). Pengembang tidak menerima lalu lintas itu.

### Server FTPS

Saat server berjalan, server mendengarkan di jaringan lokal Anda agar klien FTPS yang Anda konfigurasikan dapat membaca atau menulis folder yang Anda bagikan, dengan nama pengguna dan kata sandi yang Anda tetapkan. Lalu lintas itu tetap di antara perangkat Anda (dan siapa pun di jaringan yang memiliki kredensial tersebut). Pengembang bukan pihak di dalamnya.

Anda bertanggung jawab atas siapa yang dapat mencapai port, folder mana yang Anda bagikan, dan seberapa kuat kata sandi itu.

### Tautan yang Anda buka

Aplikasi dapat membuka halaman ini di peramban sistem. Situs-situs itu memiliki kebijakan privasi sendiri:

- Beranda proyek: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- Lisensi: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- Rilis terbaru: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## Perilaku lokal lainnya

Saat server berjalan, aplikasi dapat meminta sistem operasi mengurangi tidur agar transfer dapat berlanjut.

## Anak-anak

Aplikasi ini adalah server berkas jaringan. Aplikasi ini tidak ditujukan kepada anak-anak di bawah 13 tahun.

## Pihak ketiga

GitHub memproses permintaan pemeriksaan pembaruan dan halaman yang Anda buka, seperti di atas. Pengembang tidak menerima lalu lintas itu.

## Perubahan

Pembaruan kebijakan ini akan diposting di berkas ini dalam repositori proyek.

## Kontak

Nama aplikasi: FTPS Server
Nama pengembang: Siarhei Kuchuk

Pertanyaan: [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
