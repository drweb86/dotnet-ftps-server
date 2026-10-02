# Release process

A. Decrement changelog version.

B. Run `scripts/patch-version.ps1`.

C. Create release with create tag matching version.

D. Distribute:

- Microsoft Store: https://partner.microsoft.com/dashboard . Upload MSIX files there.
- Google Play: https://play.google.com/console . Upload `ftpsserver_*_android.aab` (general flavor, not the China AAB) and the Fastlane listing in `fastlane/metadata/android`. Public listing: https://play.google.com/store/apps/details?id=com.siarheikuchuk.ftpsserver .
- FDroid: 1 day after: verify if https://f-droid.org/ru/packages/com.siarheikuchuk.ftpsserver/ is updated.
- RuStore: https://console.rustore.ru/apps/2063728851/versions . Publish new version.
- NUGet (separate pipeline for that exists), should be done when library is changed.
- Huawei Store: https://developer.huawei.com/consumer/en/service/josp/agc/index.html#/myApp .
- Ubuntu: verify that pipeline is green.
- Win-Get: to be ignroed until they accept app.