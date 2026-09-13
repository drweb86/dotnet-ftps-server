[Languages](README.md)

# የግላዊነት ፖሊሲ

ለመጨረሻ ጊዜ የተዘመነው፦ 13 መስከረም 2026


**FTPS Server** by Siarhei Kuchuk

የመተግበሪያ ስም: FTPS Server
የገንቢ ስም: Siarhei Kuchuk

ሶፍትዌሩ የአካባቢ FTPS (በTLS ላይ FTP) አገልጋይ ነው። የደመና መለያዎች አይፈጥርም።
ገንቢዎ ፋይሎችዎን፣ የይለፍ ቃሎችዎን ወይም የአጠቃቀም ውሂብ የሚቀበል የኋላ አገልግሎት አያስኬድም።

## ገንቢው የማይሰበስበው ውሂብ

መተግበሪያው ማስታወቂያ፣ ትንታኔ፣ የብልሽት ሪፖርተሮች ወይም የክትትል SDKዎች የሉትም። ገንቢው የግል ውሂብ አይሰበስብም፣ አይሸጥም ወይም አያጋራም።

## በኮምፒውተርዎ ላይ የተቀመጠ ውሂብ

የመተግበሪያ ቅንብሮች (የFTPS የተጠቃሚ ስሞችና የይለፍ ቃሎች፣ የአገልጋይ ወደብ፣ የግንኙነት ገደቦች እና አማራጭ የምስክር ወረቀት መንገድና የይለፍ ቃልን ጨምሮ) በዚህ ኮምፒውተር ብቻ ይቀመጣሉ፦

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

መተግበሪያው በራሱ የተፈረመ ምስክር ወረቀት ከፈጠረ እዚህ ይቀመጣል፦

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

የአገልጋይ ምዝግብ ማስታወሻዎች እዚህ ሊጻፉ ይችላሉ፦

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

እነዚህ እሴቶች ወደ ገንቢው አይሰቀሉም። መተግበሪያውን ወይም እነዚያን አቃፊዎች ማስወገድ ያጠፋቸዋል። የተጋሩ **ፋይሎች** በመረጧቸው አቃፊዎች ውስጥ ይቀራሉ፤ መተግበሪያው ወደ የገንቢ አገልጋይ አይቅዳቸውም።

አቃፊዎች በስርዓት አቃፊ መራጭ ይመረጣሉ። መተግበሪያው የፈቀዷቸውን አቃፊዎች ብቻ ያጋራል።

ውሂብዎን ለማከማቸት የገንቢ አገልጋይ አይጠቀምም።

## የአውታረ መረብ አጠቃቀም

### የዝመና ፍተሻ

መተግበሪያው የቅርብ ጊዜውን የGitHub ልቀት ሊጠይቅ ይችላል፦

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub (Microsoft) መደበኛ የHTTPS ጥያቄ ይቀበላል (የIP አድራሻ፣ user-agent፣ ሰዓት)። ገንቢው ያን ትራፊክ አይቀበልም።

### የFTPS አገልጋይ

አገልጋዩ ሲሰራ በአካባቢ አውታረ መረብዎ ላይ ያዳምጣል ስለዚህ የሚያዋቅሯቸው የFTPS ደንበኞች የተጋሩ አቃፊዎችን በያዘጋጇቸው የተጠቃሚ ስሞችና የይለፍ ቃሎች ሊያነቡ ወይም ሊጽፉ ይችላሉ። ያ ትራፊክ በመሣሪያዎችዎ መካከል ይቀራል (እና በአውታረ መረቡ ላይ እነዚያ ማስረጃዎች ያሉት ማንኛውም)። ገንቢው ወገን አይደለም።

ወደብ ማን ሊደርስበት እንደሚችል፣ የትኞቹን አቃፊዎች እንደሚያጋሩ እና እነዚያ የይለፍ ቃሎች ምን ያህል ጠንካራ እንደሆኑ እርስዎ ኃላፊ ነዎት።

### የሚከፍቷቸው አገናኞች

መተግበሪያው እነዚህን ገጾች በስርዓት አሳሽ ሊከፍት ይችላል። እነዚያ ጣቢያዎች የራሳቸው የግላዊነት ፖሊሲዎች አሏቸው፦

- የፕሮጀክት መነሻ ገጽ: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- ፈቃድ: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- የቅርብ ጊዜ ልቀት: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## ሌላ የአካባቢ ባህሪ

አገልጋዩ ሲሰራ መተግበሪያው ማስተላለፎች እንዲቀጥሉ ስርዓተ ክወናው እንቅልፍ እንዲቀንስ ሊጠይቅ ይችላል።

## ልጆች

መተግበሪያው የአውታረ መረብ ፋይል አገልጋይ ነው። ከ13 ዓመት በታች ላሉ ልጆች አይመራም።

## ሦስተኛ ወገኖች

GitHub የዝመና ፍተሻ ጥያቄውን እና የሚከፍቷቸውን ገጾች ከላይ እንደተገለጸ ያስኬዳል። ገንቢው ያን ትራፊክ አይቀበልም።

## ለውጦች

የዚህ ፖሊሲ ዝመናዎች በፕሮጀክት ማከማቻው በዚህ ፋይል ይለጠፋሉ።

## ግንኙነት

የመተግበሪያ ስም: FTPS Server
የገንቢ ስም: Siarhei Kuchuk

ጥያቄዎች፦ [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
