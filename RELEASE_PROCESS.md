# Release process

A. Decrement changelog version.

B. Run `scripts/patch-version.ps1`.

C. Create release with create tag matching version.

D. Distribute:

- Microsoft Store: https://partner.microsoft.com/dashboard . Upload MSIX files there.
- Google Play: https://play.google.com/console . Upload `ftpsserver_*_android.aab` (general flavor, not the China AAB) and the Fastlane listing in `fastlane/metadata/android`. Public listing: https://play.google.com/store/apps/details?id=com.siarheikuchuk.ftpsserver .
```
Откройте приложение в Play Console.
Слева откройте Тестирование и выпуск → Рабочая версия. Если кнопка в письме открыла Обзор выпусков, на этой странице тоже выберите блок Рабочая версия, не «Закрытое тестирование».
Нажмите Создать выпуск. Если черновик уже есть, откройте его через Изменить выпуск.
В блоке Наборы App Bundle нажмите Загрузить и выберите ftpsserver_android.aab.
Дождитесь галочки у файла. 
Название выпуска оставьте как есть.
В Примечания к выпуску добавьте русский текст. Если консоль требует ещё и язык по умолчанию, вставьте тот же текст туда:

На странице выпуска оставьте полное развёртывание, 100%. 
Нажмите Начать развертывание в рабочей версии и подтвердите Развернуть.
```
- FDroid: 1 day after: verify if https://f-droid.org/ru/packages/com.siarheikuchuk.ftpsserver/ is updated.
- RuStore: https://console.rustore.ru/apps/2063728851/versions . Publish new version.
- NUGet (separate pipeline for that exists), should be done when library is changed.
- Huawei Store: https://developer.huawei.com/consumer/en/service/josp/agc/index.html#/myApp .
- Ubuntu: verify that pipeline is green.
- Win-Get: PR