[Languages](README.md)

# Maxfiylik siyosati

Oxirgi yangilanish: 2026-yil 13-sentabr


**FTPS Server** by Siarhei Kuchuk

Ilova nomi: FTPS Server
Ishlab chiquvchi nomi: Siarhei Kuchuk

Dastur mahalliy FTPS (TLS ustidagi FTP) serveridir. Bulut hisoblarini yaratmaydi.
Ishlab chiquvchi fayllaringiz, parollaringiz yoki foydalanish ma’lumotlarini qabul qiladigan backend ishlatmaydi.

## Ishlab chiquvchi yig‘maydigan ma’lumotlar

Ilovada reklama, tahlil, nosozlik hisobotlari yoki kuzatuv SDK yo‘q. Ishlab chiquvchi shaxsiy ma’lumotlarni yig‘maydi, sotmaydi yoki ulashmaydi.

## Kompyuteringizda saqlanadigan ma’lumotlar

Ilova sozlamalari (FTPS foydalanuvchi nomlari va parollari, server porti, ulanish cheklovlari hamda ixtiyoriy sertifikat yo‘li va paroli) faqat shu kompyuterda saqlanadi:

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

Ilova o‘zini o‘zi imzolagan sertifikat yaratsa, u shu yerda saqlanadi:

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

Server jurnallari shu yerga yozilishi mumkin:

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

Bu qiymatlar ishlab chiquvchiga yuklanmaydi. Ilovani yoki ushbu jildlarni olib tashlash ularni o‘chiradi. Ulashilgan **fayllar** siz tanlagan jildlarda qoladi; ilova ularni ishlab chiquvchi serveriga nusxalamaydi.

Jildlar tizim jild tanlagichi bilan tanlanadi. Ilova faqat siz ruxsat bergan jildlarni ulashadi.

Ma’lumotlaringizni saqlash uchun ishlab chiquvchi serveri ishlatilmaydi.

## Tarmoqdan foydalanish

### Yangilanishni tekshirish

Ilova GitHubning so‘nggi relizini so‘rashi mumkin:

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub (Microsoft) oddiy HTTPS so‘rovini oladi (IP manzil, user-agent, vaqt). Ishlab chiquvchi bu trafikni olmaydi.

### FTPS serveri

Server ishlayotganda u mahalliy tarmog‘ingizda tinglaydi, shunda sozlagan FTPS mijozlaringiz ulashilgan jildlarni belgilagan foydalanuvchi nomlari va parollar bilan o‘qishi yoki yozishi mumkin. Bu trafik qurilmalaringiz orasida (va tarmoqda shu hisob ma’lumotlariga ega har kim bilan) qoladi. Ishlab chiquvchi ishtirok etmaydi.

Portga kim yetishi, qaysi jildlarni ulashishingiz va parollar qanchalik kuchliligi uchun siz javobgarsiz.

### Ochadigan havolalar

Ilova ushbu sahifalarni tizim brauzerida ochishi mumkin. Bu saytlarning o‘z maxfiylik siyosatlari bor:

- Loyiha bosh sahifasi: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- Litsenziya: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- So‘nggi reliz: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## Boshqa mahalliy xatti-harakat

Server ishlayotganda ilova uzatish davom etishi uchun operatsion tizimdan uyquni kamaytirishni so‘rashi mumkin.

## Bolalar

Ilova tarmoq fayl serveridir. U 13 yoshgacha bolalarga mo‘ljallanmagan.

## Uchinchi tomonlar

GitHub yuqoridagidek yangilanishni tekshirish so‘rovini va ochgan sahifalaringizni qayta ishlaydi. Ishlab chiquvchi bu trafikni olmaydi.

## O‘zgarishlar

Ushbu siyosat yangilanishlari loyiha omboridagi ushbu faylda e’lon qilinadi.

## Aloqa

Ilova nomi: FTPS Server
Ishlab chiquvchi nomi: Siarhei Kuchuk

Savollar: [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
