#!/usr/bin/env python3
"""Generate Fastlane metadata from Windows .resx translations."""
from __future__ import annotations

import re
import shutil
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
RESX_DIR = ROOT / "sources" / "FtpsServerWindows" / "Resources"
ICON_SRC = ROOT / "sources" / "FtpsServerAvalonia" / "FtpsServerAvalonia.Android" / "Icon.png"
OUT = ROOT / "fastlane" / "metadata" / "android"
VERSION_CODE = "20260831"

FOOTER = {
    "en-US": (
        "This is the Android app (Kotlin). No ads. Open source (CC0). "
        "Each user chooses a folder with the system picker. "
        "A notification keeps transfers running with the screen off.\n\n"
        "Requires Android 6.0 or newer.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "de-DE": (
        "Dies ist die Android-App (Kotlin). Keine Werbung. Open Source (CC0). "
        "Jeder Benutzer wählt einen Ordner über die Systemauswahl. "
        "Eine Benachrichtigung hält Übertragungen bei ausgeschaltetem Bildschirm aktiv.\n\n"
        "Erfordert Android 6.0 oder neuer.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "es-ES": (
        "Esta es la aplicación para Android (Kotlin). Sin anuncios. Código abierto (CC0). "
        "Cada usuario elige una carpeta con el selector del sistema. "
        "Una notificación mantiene las transferencias con la pantalla apagada.\n\n"
        "Requiere Android 6.0 o posterior.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "fr-FR": (
        "Ceci est l’application Android (Kotlin). Sans publicité. Open source (CC0). "
        "Chaque utilisateur choisit un dossier avec le sélecteur système. "
        "Une notification maintient les transferts écran éteint.\n\n"
        "Nécessite Android 6.0 ou plus récent.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "ru-RU": (
        "Это приложение для Android (Kotlin). Без рекламы. Открытый код (CC0). "
        "Каждый пользователь выбирает папку системным диалогом. "
        "Уведомление держит передачи активными при выключенном экране.\n\n"
        "Требуется Android 6.0 или новее.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "zh-CN": (
        "这是 Android 应用（Kotlin）。无广告。开源（CC0）。"
        "每位用户通过系统选择器选定文件夹。"
        "通知可在息屏时保持传输。\n\n"
        "需要 Android 6.0 或更高版本。\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "ja-JP": (
        "これは Android アプリ（Kotlin）です。広告なし。オープンソース（CC0）。"
        "各ユーザーはシステムのフォルダー選択で共有先を指定します。"
        "通知により画面オフでも転送を継続します。\n\n"
        "Android 6.0 以降が必要です。\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "ko-KR": (
        "Android 앱(Kotlin)입니다. 광고 없음. 오픈 소스(CC0). "
        "각 사용자는 시스템 선택기로 폴더를 고릅니다. "
        "알림이 화면이 꺼진 동안에도 전송을 유지합니다.\n\n"
        "Android 6.0 이상이 필요합니다.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "pt-BR": (
        "Este é o aplicativo Android (Kotlin). Sem anúncios. Código aberto (CC0). "
        "Cada usuário escolhe uma pasta pelo seletor do sistema. "
        "Uma notificação mantém as transferências com a tela desligada.\n\n"
        "Requer Android 6.0 ou mais recente.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "it-IT": (
        "Questa è l’app Android (Kotlin). Senza pubblicità. Open source (CC0). "
        "Ogni utente sceglie una cartella con il selettore di sistema. "
        "Una notifica mantiene i trasferimenti a schermo spento.\n\n"
        "Richiede Android 6.0 o successivo.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "pl-PL": (
        "To aplikacja na Androida (Kotlin). Bez reklam. Open source (CC0). "
        "Każdy użytkownik wybiera folder systemowym selektorem. "
        "Powiadomienie utrzymuje transfery przy wyłączonym ekranie.\n\n"
        "Wymaga Androida 6.0 lub nowszego.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "uk-UA": (
        "Це застосунок для Android (Kotlin). Без реклами. Відкритий код (CC0). "
        "Кожен користувач обирає теку системним вибором. "
        "Сповіщення тримає передавання активним при вимкненому екрані.\n\n"
        "Потрібен Android 6.0 або новіший.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "tr-TR": (
        "Bu Android uygulamasıdır (Kotlin). Reklamsız. Açık kaynak (CC0). "
        "Her kullanıcı sistem seçicisiyle bir klasör seçer. "
        "Bildirim, ekran kapalıyken aktarımları sürdürür.\n\n"
        "Android 6.0 veya daha yenisi gerekir.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "ar-SA": (
        "هذا تطبيق أندرويد (Kotlin). بدون إعلانات. مفتوح المصدر (CC0). "
        "يختار كل مستخدم مجلدًا عبر منتقي النظام. "
        "يُبقي الإشعار النقل نشطًا والشاشة مغلقة.\n\n"
        "يتطلب أندرويد 6.0 أو أحدث.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "fa-IR": (
        "این برنامه اندروید (Kotlin) است. بدون تبلیغ. متن‌باز (CC0). "
        "هر کاربر پوشه را با انتخابگر سیستم برمی‌گزیند. "
        "اعلان انتقال را با صفحه خاموش نگه می‌دارد.\n\n"
        "اندروید ۶.۰ یا جدیدتر لازم است.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "hi-IN": (
        "यह Android ऐप (Kotlin) है। विज्ञापन नहीं। ओपन सोर्स (CC0)। "
        "हर उपयोगकर्ता सिस्टम पिकर से फ़ोल्डर चुनता है। "
        "सूचना स्क्रीन बंद होने पर भी स्थानांतरण चालू रखती है।\n\n"
        "Android 6.0 या नया चाहिए।\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "id-ID": (
        "Ini aplikasi Android (Kotlin). Tanpa iklan. Sumber terbuka (CC0). "
        "Setiap pengguna memilih folder dengan pemilih sistem. "
        "Notifikasi menjaga transfer saat layar mati.\n\n"
        "Membutuhkan Android 6.0 atau lebih baru.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "th-TH": (
        "นี่คือแอป Android (Kotlin) ไม่มีโฆษณา โอเพนซอร์ส (CC0) "
        "ผู้ใช้แต่ละคนเลือกโฟลเดอร์ด้วยตัวเลือกของระบบ "
        "การแจ้งเตือนทำให้การถ่ายโอนทำงานต่อเมื่อปิดหน้าจอ\n\n"
        "ต้องใช้ Android 6.0 ขึ้นไป\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "vi-VN": (
        "Đây là ứng dụng Android (Kotlin). Không quảng cáo. Mã nguồn mở (CC0). "
        "Mỗi người dùng chọn thư mục bằng bộ chọn hệ thống. "
        "Thông báo giữ truyền tệp khi tắt màn hình.\n\n"
        "Cần Android 6.0 trở lên.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "bn-IN": (
        "এটি Android অ্যাপ (Kotlin)। বিজ্ঞাপন নেই। ওপেন সোর্স (CC0)। "
        "প্রতিটি ব্যবহারকারী সিস্টেম পিকার দিয়ে ফোল্ডার বেছে নেন। "
        "বিজ্ঞপ্তি স্ক্রিন বন্ধ থাকলেও স্থানান্তর চালু রাখে।\n\n"
        "Android 6.0 বা নতুন প্রয়োজন।\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "ta-IN": (
        "இது Android செயலி (Kotlin). விளம்பரம் இல்லை. திறந்த மூலம் (CC0). "
        "ஒவ்வொரு பயனரும் கணினி தேர்வியால் கோப்புறையைத் தேர்ந்தெடுக்கிறார். "
        "அறிவிப்பு திரை அணைந்திருந்தாலும் பரிமாற்றத்தைத் தொடர வைக்கிறது.\n\n"
        "Android 6.0 அல்லது புதியது தேவை.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "te-IN": (
        "ఇది Android యాప్ (Kotlin). ప్రకటనలు లేవు. ఓపెన్ సోర్స్ (CC0). "
        "ప్రతి వినియోగదారు సిస్టమ్ పికర్‌తో ఫోల్డర్ ఎంచుకుంటారు. "
        "నోటిఫికేషన్ స్క్రీన్ ఆఫ్‌లో కూడా బదిలీని కొనసాగిస్తుంది.\n\n"
        "Android 6.0 లేదా కొత్తది కావాలి.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "mr-IN": (
        "हे Android अॅप (Kotlin) आहे. जाहिराती नाहीत. ओपन सोर्स (CC0). "
        "प्रत्येक वापरकर्ता सिस्टम पिकरने फोल्डर निवडतो. "
        "सूचना स्क्रीन बंद असतानाही हस्तांतरण सुरू ठेवते.\n\n"
        "Android 6.0 किंवा नवीन हवे.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "pa-IN": (
        "ਇਹ Android ਐਪ (Kotlin) ਹੈ। ਇਸ਼ਤਿਹਾਰ ਨਹੀਂ। ਓਪਨ ਸੋਰਸ (CC0)। "
        "ਹਰ ਵਰਤੋਂਕਾਰ ਸਿਸਟਮ ਪਿਕਰ ਨਾਲ ਫੋਲਡਰ ਚੁਣਦਾ ਹੈ। "
        "ਸੂਚਨਾ ਸਕਰੀਨ ਬੰਦ ਹੋਣ ’ਤੇ ਵੀ ਟ੍ਰਾਂਸਫਰ ਚਾਲੂ ਰੱਖਦੀ ਹੈ।\n\n"
        "Android 6.0 ਜਾਂ ਨਵਾਂ ਚਾਹੀਦਾ ਹੈ।\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "ur-PK": (
        "یہ اینڈرائیڈ ایپ (Kotlin) ہے۔ بغیر اشتہارات۔ اوپن سورس (CC0)۔ "
        "ہر صارف سسٹم پکر سے فولڈر چنتا ہے۔ "
        "اطلاع اسکرین بند ہونے پر بھی منتقلی جاری رکھتی ہے۔\n\n"
        "اینڈرائیڈ 6.0 یا نیا درکار ہے۔\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "ne-NP": (
        "यो Android एप (Kotlin) हो। विज्ञापन छैन। खुला स्रोत (CC0)। "
        "प्रत्येक प्रयोगकर्ताले सिस्टम पिकरले फोल्डर छान्छ। "
        "सूचना स्क्रिन बन्द हुँदा पनि स्थानान्तरण चालु राख्छ।\n\n"
        "Android 6.0 वा नयाँ चाहिन्छ।\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "my-MM": (
        "ဤသည် Android အက်ပ် (Kotlin) ဖြစ်သည်။ ကြော်ငြာမရှိ။ ပွင့်လင်းအရင်းအမြစ် (CC0)။ "
        "အသုံးပြုသူတိုင်း စနစ်ရွေးချယ်မှုဖြင့် ဖိုလ်ဒါရွေးသည်။ "
        "အသိပေးချက်က မျက်နှာပြင်ပိတ်နေစဉ် လွှဲပြောင်းမှုကို ဆက်ထားသည်။\n\n"
        "Android 6.0 နှင့်အထက် လိုအပ်သည်။\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "kk-KZ": (
        "Бұл Android қолданбасы (Kotlin). Жарнама жоқ. Ашық код (CC0). "
        "Әр пайдаланушы жүйе таңдағышымен қалта таңдайды. "
        "Хабарландыру экран өшірулі кезде беруді жалғастырады.\n\n"
        "Android 6.0 немесе жаңарақ қажет.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "uz-UZ": (
        "Bu Android ilovasi (Kotlin). Reklamasiz. Ochiq kod (CC0). "
        "Har bir foydalanuvchi tizim tanlagichida jild tanlaydi. "
        "Bildirishnoma ekran o‘chiqligida ham uzatishni davom ettiradi.\n\n"
        "Android 6.0 yoki yangiroq kerak.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "am-ET": (
        "ይህ የአንድሮይድ መተግበሪያ (Kotlin) ነው። ማስታወቂያ የለም። ክፍት ምንጭ (CC0)። "
        "እያንዳንዱ ተጠቃሚ በስርዓት መራጭ አቃፊ ይመርጣል። "
        "ማሳወቂያው ማያው ሲጠፋም ማስተላለፍን ይቀጥላል።\n\n"
        "አንድሮይድ 6.0 ወይም አዲስ ያስፈልጋል።\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "sw-KE": (
        "Hii ni programu ya Android (Kotlin). Bila matangazo. Chanzo huria (CC0). "
        "Kila mtumiaji huchagua folda kwa kichaguzi cha mfumo. "
        "Arifa huendeleza uhamishaji skrini ikiwa imezimwa.\n\n"
        "Inahitaji Android 6.0 au mpya zaidi.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "ha-NG": (
        "Wannan app ɗin Android ne (Kotlin). Babu talla. Open source (CC0). "
        "Kowane mai amfani yana zaɓar folder da zaɓin tsarin. "
        "Sanarwa tana ci gaba da canja wurin yayin da allon ya kashe.\n\n"
        "Yana buƙatar Android 6.0 ko sabo.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "yo-NG": (
        "Èyí ni app Android (Kotlin). Kò sí ìpolówó. Open source (CC0). "
        "Olùmúlò kọ̀ọ̀kan yan folder pẹ̀lú aṣàyàn ètò. "
        "Ìfitónilétí máa ń tẹ̀síwájú ìgbésí nígbà tí ìbòjú bá pa.\n\n"
        "Ó nílò Android 6.0 tàbí tuntun.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "ig-NG": (
        "Nke a bụ ngwa Android (Kotlin). Enweghị mgbasa ozi. Open source (CC0). "
        "Onye ọrụ ọ bụla na-ahọrọ nchekwa site na nhọrọ sistemụ. "
        "Ọkwa na-eme ka nnyefe gaa n’ihu mgbe ihuenyo gbanyụrụ.\n\n"
        "Ọ chọrọ Android 6.0 ma ọ bụ ọhụrụ.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "om-ET": (
        "Kun app Android (Kotlin) dha. Beeksisa hin qabu. Madda banaa (CC0). "
        "Fayyadamaa tokkoon tokkoon galmee filannaa sirnaatiin filata. "
        "Beeksisaon argii cufaa yeroo geeddaruu itti fufsiisa.\n\n"
        "Android 6.0 ykn haaraa barbaachisa.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "ps-AF": (
        "دا د انډرایډ اپلیکیشن (Kotlin) دی. پرته له اعلانونو. خلاص سرچینه (CC0). "
        "هر کارن د سیستم ټاکونکي سره فولډر ټاکي. "
        "خبرتیا د پردې بندېدو سره هم لېږد روان ساتي.\n\n"
        "انډرایډ 6.0 یا نوی اړین دی.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "yue-HK": (
        "呢個係 Android 應用（Kotlin）。無廣告。開源（CC0）。"
        "每個用戶用系統選擇器揀資料夾。"
        "通知可以喺熄屏時繼續傳輸。\n\n"
        "需要 Android 6.0 或更新版本。\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
    "pcm-NG": (
        "Dis na Android app (Kotlin). No advert. Open source (CC0). "
        "Each user go choose folder wit di system picker. "
        "Notification go keep transfer even if screen don off.\n\n"
        "E need Android 6.0 or newer.\n"
        "https://github.com/drweb86/dotnet-ftps-server"
    ),
}

CHANGELOG = {
    "en-US": "Android app is now native Kotlin instead of Avalonia/.NET (~5 MB; FOSS stores can build from source). Same package id and signing key, so existing installs can update.",
    "de-DE": "Die Android-App ist jetzt natives Kotlin statt Avalonia/.NET (~5 MB; FOSS-Stores können aus dem Quellcode bauen). Dieselbe Paket-ID und derselbe Signaturschlüssel, bestehende Installationen können aktualisiert werden.",
    "es-ES": "La app Android ahora es Kotlin nativo en lugar de Avalonia/.NET (~5 MB; las tiendas FOSS pueden compilar desde el código). El mismo id de paquete y clave de firma, las instalaciones existentes pueden actualizarse.",
    "fr-FR": "L’app Android est désormais en Kotlin natif au lieu d’Avalonia/.NET (~5 Mo ; les dépôts FOSS peuvent compiler depuis les sources). Même identifiant de paquet et même clé de signature : les installations existantes peuvent se mettre à jour.",
    "ru-RU": "Android-приложение теперь на Kotlin, а не Avalonia/.NET (~5 МБ; FOSS-магазины могут собирать из исходников). Тот же идентификатор пакета и ключ подписи — существующие установки можно обновить.",
    "zh-CN": "Android 应用改为原生 Kotlin，不再使用 Avalonia/.NET（约 5 MB，FOSS 商店可从源码构建）。包名与签名密钥不变，可覆盖更新已安装版本。",
    "ja-JP": "Android アプリは Avalonia/.NET ではなくネイティブ Kotlin になりました（約 5 MB。FOSS ストアはソースからビルド可能）。パッケージ ID と署名鍵は同じなので、既存のインストールを更新できます。",
    "ko-KR": "Android 앱이 Avalonia/.NET 대신 네이티브 Kotlin입니다(약 5 MB, FOSS 스토어에서 소스 빌드 가능). 패키지 ID와 서명 키가 같아 기존 설치를 업데이트할 수 있습니다.",
    "pt-BR": "O app Android agora é Kotlin nativo em vez de Avalonia/.NET (~5 MB; lojas FOSS podem compilar a partir do código). O mesmo id de pacote e chave de assinatura, instalações existentes podem atualizar.",
    "it-IT": "L’app Android è ora Kotlin nativo invece di Avalonia/.NET (~5 MB; i negozi FOSS possono compilare dal codice). Stesso id del pacchetto e stessa chiave di firma: gli install già presenti possono aggiornarsi.",
    "pl-PL": "Aplikacja Android jest teraz natywnym Kotlinem zamiast Avalonia/.NET (~5 MB; sklepy FOSS mogą budować ze źródeł). Ten sam identyfikator pakietu i klucz podpisu — istniejące instalacje można zaktualizować.",
    "uk-UA": "Застосунок Android тепер на Kotlin, а не Avalonia/.NET (~5 МБ; FOSS-крамниці можуть збирати з коду). Той самий ідентифікатор пакета й ключ підпису — наявні встановлення можна оновити.",
    "tr-TR": "Android uygulaması Avalonia/.NET yerine yerel Kotlin (~5 MB; FOSS mağazaları kaynaktan derleyebilir). Aynı paket kimliği ve imza anahtarı, mevcut kurulumlar güncellenebilir.",
    "ar-SA": "تطبيق أندرويد أصبح Kotlin أصليًا بدل Avalonia/.NET (حوالي 5 ميغابايت؛ يمكن لمتاجر FOSS البناء من المصدر). نفس معرّف الحزمة ومفتاح التوقيع، يمكن تحديث التثبيتات الحالية.",
    "fa-IR": "برنامه اندروید اکنون Kotlin بومی است نه Avalonia/.NET (حدود ۵ مگابایت؛ فروشگاه‌های FOSS می‌توانند از منبع بسازند). همان شناسه بسته و کلید امضا؛ نصب‌های موجود قابل به‌روزرسانی‌اند.",
    "hi-IN": "Android ऐप अब Avalonia/.NET के बजाय नेटिव Kotlin है (~5 MB; FOSS स्टोर स्रोत से बिल्ड कर सकते हैं)। वही पैकेज आईडी और साइन कुंजी, मौजूदा इंस्टॉल अपडेट हो सकते हैं।",
    "id-ID": "Aplikasi Android kini Kotlin native, bukan Avalonia/.NET (~5 MB; toko FOSS dapat membangun dari sumber). Id paket dan kunci tanda tangan sama, instalasi yang ada dapat diperbarui.",
    "th-TH": "แอป Android เป็น Kotlin พื้นเมืองแทน Avalonia/.NET (ประมาณ 5 MB ร้าน FOSS สร้างจากซอร์สได้) ใช้รหัสแพ็กเกจและคีย์ลงนามเดิม อัปเดตทับการติดตั้งเดิมได้",
    "vi-VN": "Ứng dụng Android giờ là Kotlin gốc thay Avalonia/.NET (~5 MB; cửa hàng FOSS có thể biên dịch từ mã nguồn). Cùng id gói và khóa ký, bản cài sẵn có thể cập nhật.",
    "bn-IN": "Android অ্যাপ এখন Avalonia/.NET-এর বদলে নেটিভ Kotlin (~5 MB; FOSS স্টোর উৎস থেকে বিল্ড করতে পারে)। একই প্যাকেজ আইডি ও স্বাক্ষর কী, বিদ্যমান ইনস্টল আপডেট করা যায়।",
    "ta-IN": "Android செயலி இப்போது Avalonia/.NET அல்லாமல் சொந்த Kotlin (~5 MB; FOSS கடைகள் மூலத்திலிருந்து உருவாக்கலாம்). அதே தொகுப்பு அடையாளம் மற்றும் கையொப்ப விசை, உள்ள நிறுவல்களைப் புதுப்பிக்கலாம்.",
    "te-IN": "Android యాప్ ఇప్పుడు Avalonia/.NET కాకుండా స్థానిక Kotlin (~5 MB; FOSS స్టోర్లు సోర్స్ నుండి బిల్డ్ చేయవచ్చు). అదే ప్యాకేజ్ ఐడి మరియు సైన్ కీ, ఉన్న ఇన్‌స్టాల్‌లను అప్‌డేట్ చేయవచ్చు.",
    "mr-IN": "Android अॅप आता Avalonia/.NET ऐवजी नेटिव्ह Kotlin आहे (~5 MB; FOSS स्टोअर स्रोतून बिल्ड करू शकतात). तोच पॅकेज आयडी आणि साइन की, विद्यमान इंस्टॉल अपडेट होऊ शकतात.",
    "pa-IN": "Android ਐਪ ਹੁਣ Avalonia/.NET ਦੀ ਥਾਂ ਨੇਟਿਵ Kotlin ਹੈ (~5 MB; FOSS ਸਟੋਰ ਸਰੋਤ ਤੋਂ ਬਿਲਡ ਕਰ ਸਕਦੇ ਹਨ)। ਉਹੀ ਪੈਕੇਜ ਆਈਡੀ ਅਤੇ ਸਾਈਨ ਕੁੰਜੀ, ਮੌਜੂਦਾ ਇੰਸਟਾਲ ਅੱਪਡੇਟ ਹੋ ਸਕਦੇ ਹਨ।",
    "ur-PK": "اینڈرائیڈ ایپ اب Avalonia/.NET کے بجائے نیٹو Kotlin ہے (تقریباً 5 میگابائٹ؛ FOSS اسٹور ماخذ سے بلڈ کر سکتے ہیں)۔ وہی پیکیج آئی ڈی اور سائن کلید، موجودہ انسٹال اپ ڈیٹ ہو سکتے ہیں۔",
    "ne-NP": "Android एप अब Avalonia/.NET होइन नेटिभ Kotlin हो (~5 MB; FOSS स्टोर स्रोतबाट बिल्ड गर्न सक्छन्)। उही प्याकेज आईडी र साइन कुञ्जी, भएका इन्स्टल अपडेट गर्न सकिन्छ।",
    "my-MM": "Android အက်ပ်သည် Avalonia/.NET အစား native Kotlin ဖြစ်သည် (~5 MB; FOSS ဆိုင်များက ရင်းမြစ်မှ တည်ဆောက်နိုင်)။ ပက်ကေ့ချ် ID နှင့် လက်မှတ်ကီး အတူတူဖြစ်၍ ရှိပြီးသား ထည့်သွင်းမှုကို အပ်ဒိတ်လုပ်နိုင်သည်။",
    "kk-KZ": "Android қолданбасы енді Avalonia/.NET емес, жергілікті Kotlin (~5 МБ; FOSS дүкендері көзден құрастыра алады). Сол пакет идентификаторы мен қолтаңба кілті — бар орнатуларды жаңартуға болады.",
    "uz-UZ": "Android ilovasi endi Avalonia/.NET emas, native Kotlin (~5 MB; FOSS do‘konlari manbadan yig‘ishi mumkin). Xuddi shu paket id va imzo kaliti, mavjud o‘rnatishlarni yangilash mumkin.",
    "am-ET": "የአንድሮይድ መተግበሪያ አሁን Avalonia/.NET ሳይሆን ተወላጅ Kotlin ነው (~5 ሜባ፤ የFOSS መደብሮች ከምንጭ መገንባት ይችላሉ)። ተመሳሳይ የጥቅል መታወቂያ እና የፊርማ ቁልፍ፣ ያሉት ጭነቶች ሊዘምኑ ይችላሉ።",
    "sw-KE": "Programu ya Android sasa ni Kotlin asilia badala ya Avalonia/.NET (~5 MB; maduka ya FOSS yanaweza kujenga kutoka chanzo). Kitambulisho cha kifurushi na ufunguo wa saini ni vilevile, usakinishaji uliopo unaweza kusasishwa.",
    "ha-NG": "App ɗin Android yanzu Kotlin ne na asali maimakon Avalonia/.NET (~5 MB; shagunan FOSS na iya ginawa daga tushe). Irin wannan id ɗin fakitin da maɓallin sa hannu, shigarwar da ke wurin za a iya sabunta su.",
    "yo-NG": "App Android nísinsinyìí jẹ́ Kotlin abínibí dípò Avalonia/.NET (~5 MB; àwọn ilé-ìtajà FOSS lè kọ́ láti orísun). Id ìdìpọ̀ àti kọ́kọ́rọ́ ìbuwọ́lu kannáà, àwọn ìgbékalẹ̀ tó wà lè ṣe ìmúdójúìwọ̀n.",
    "ig-NG": "Ngwa Android ugbu a bụ Kotlin nke obodo kama Avalonia/.NET (~5 MB; ụlọ ahịa FOSS nwere ike wuo site na isi mmalite). Otu id ngwugwu na igodo mbinye aka, nrụnye dị ugbu a nwere ike melite.",
    "om-ET": "App Android amma Avalonia/.NET oso hin taane Kotlin uumamaa (~5 MB; suuqonni FOSS madda irraa ijaaruu danda’u). Id paakejii fi furtuu mallattoo wal-fakkataa, fe’iinsa jiru haaromsuu danda’a.",
    "ps-AF": "د انډرایډ اپلیکیشن اوس Avalonia/.NET پر ځای اصلي Kotlin دی (شاوخوا ۵ مېګابایټ؛ FOSS پلورنځي له سرچینې جوړولی شي). هماغه د کڅوړې پېژند او لاسلیک کیلي، موجود نصبونه تازه کېدای شي.",
    "yue-HK": "Android 應用而家係原生 Kotlin，唔再用 Avalonia/.NET（約 5 MB，FOSS 商店可以由原始碼編譯）。套件名稱同簽名匙不變，可以覆蓋更新已安裝版本。",
    "pcm-NG": "Di Android app na native Kotlin now, no be Avalonia/.NET (~5 MB; FOSS stores fit build from source). Na di same package id and signing key, so old install fit update.",
}


def parse_resx(path: Path) -> dict[str, str]:
    tree = ET.parse(path)
    out: dict[str, str] = {}
    for data in tree.getroot().findall("data"):
        name = data.get("name")
        if not name:
            continue
        value_el = data.find("value")
        if value_el is None or value_el.text is None:
            continue
        out[name] = value_el.text
    return out


def clean_full_description(text: str) -> str:
    kept = []
    for line in text.splitlines():
        if re.search(r"WPF|x64|ARM64", line, re.I):
            continue
        kept.append(line.rstrip())
    while kept and not kept[-1].strip():
        kept.pop()
    return "\n".join(kept).strip()


def write_text(path: Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    text = content.strip() + "\n"
    path.write_text(text, encoding="utf-8", newline="\n")


def generate_graphics() -> None:
    from PIL import Image, ImageDraw, ImageFont

    images = OUT / "en-US" / "images"
    images.mkdir(parents=True, exist_ok=True)
    icon = Image.open(ICON_SRC).convert("RGBA")
    icon_512 = Image.new("RGBA", (512, 512), (0, 0, 0, 255))
    fitted = icon.copy()
    fitted.thumbnail((512, 512), Image.Resampling.LANCZOS)
    x = (512 - fitted.width) // 2
    y = (512 - fitted.height) // 2
    icon_512.paste(fitted, (x, y), fitted)
    icon_512.convert("RGB").save(images / "icon.png", "PNG")

    graphic = Image.new("RGB", (1024, 500), (0, 0, 0))
    draw = ImageDraw.Draw(graphic)
    badge = icon.copy()
    badge.thumbnail((360, 360), Image.Resampling.LANCZOS)
    gx = 80
    gy = (500 - badge.height) // 2
    graphic.paste(badge.convert("RGB"), (gx, gy))
    font = None
    for candidate in (
        Path(r"C:\Windows\Fonts\segoeui.ttf"),
        Path(r"C:\Windows\Fonts\arial.ttf"),
    ):
        if candidate.exists():
            font = ImageFont.truetype(str(candidate), 72)
            break
    if font is None:
        font = ImageFont.load_default()
    draw.text((480, 210), "FTPS Server", fill=(255, 255, 255), font=font)
    graphic.save(images / "featureGraphic.png", "PNG")


def main() -> None:
    if OUT.exists():
        shutil.rmtree(OUT)
    locales: list[str] = []
    for resx in sorted(RESX_DIR.glob("Strings*.resx")):
        data = parse_resx(resx)
        locale = data.get("_Technical_WingetLocale")
        title = data.get("AppTitle")
        short = data.get("Winget_ShortDescription")
        full = data.get("Winget_Description")
        if not locale or not title or not short or not full:
            raise SystemExit(f"Missing Fastlane source keys in {resx.name}")
        if len(title) > 30:
            raise SystemExit(f"title > 30 for {locale}: {title!r}")
        if len(short) > 80:
            raise SystemExit(f"short_description > 80 for {locale}: {short!r}")
        footer = FOOTER.get(locale)
        changelog = CHANGELOG.get(locale)
        if not footer or not changelog:
            raise SystemExit(f"Missing footer/changelog for {locale}")
        if len(changelog.encode("utf-8")) > 500:
            raise SystemExit(f"changelog > 500 bytes for {locale}")
        full_android = clean_full_description(full) + "\n\n" + footer
        if len(full_android) > 4000:
            raise SystemExit(f"full_description > 4000 for {locale}")
        loc_dir = OUT / locale
        write_text(loc_dir / "title.txt", title)
        write_text(loc_dir / "short_description.txt", short)
        write_text(loc_dir / "full_description.txt", full_android)
        write_text(loc_dir / "changelogs" / f"{VERSION_CODE}.txt", changelog)
        locales.append(locale)
    generate_graphics()
    print(f"Wrote Fastlane for {len(locales)} locales: {', '.join(locales)}")


if __name__ == "__main__":
    main()
