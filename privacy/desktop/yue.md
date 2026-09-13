[Languages](README.md)

# 私隱政策

最近更新日期：2026年9月13日


**FTPS Server** by Siarhei Kuchuk

應用程式名稱: FTPS Server
開發者姓名: Siarhei Kuchuk

本軟件係本地 FTPS（TLS 上嘅 FTP）伺服器。唔會建立雲端帳戶。
開發者唔會營運接收你檔案、密碼或使用數據嘅後端。

## 開發者唔收集嘅數據

應用程式冇廣告、分析、崩潰報告或追蹤 SDK。開發者唔收集、出售或分享個人數據。

## 儲存在你電腦嘅數據

應用程式設定（包括 FTPS 用戶名同密碼、伺服器埠、連線上限，以及可選嘅憑證路徑同密碼）只儲存在呢部電腦：

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

如果應用程式建立自簽憑證，會儲存在呢度：

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

伺服器日誌可能寫入呢度：

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

呢啲數值唔會上傳俾開發者。移除應用程式或呢啲資料夾會刪除佢哋。共享嘅 **檔案** 仍然喺你揀嘅資料夾；應用程式唔會複製去開發者伺服器。

資料夾用系統資料夾選擇器揀。應用程式只共享你授權嘅資料夾。

唔會用開發者伺服器嚟儲存你嘅數據。

## 網絡使用

### 更新檢查

應用程式可能會向 GitHub 請求最新版本：

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub（Microsoft）會收到普通 HTTPS 請求（IP 地址、user-agent、時間）。開發者收唔到呢啲流量。

### FTPS 伺服器

伺服器運行緊嘅時候會喺你本地網絡監聽，等你設定嘅 FTPS 客戶端可以用你設嘅用戶名同密碼讀寫共享資料夾。流量只喺你嘅裝置之間（同網絡上持有呢啲憑證嘅任何人）。開發者唔係其中一方。

邊個可以連到個埠、你共享邊啲資料夾、同密碼強度都係你嘅責任。

### 你開啟嘅連結

應用程式可以喺系統瀏覽器開啟呢啲頁面。呢啲網站有自己嘅私隱政策：

- 專案主頁: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- 授權: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- 最新版本: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## 其他本機行為

伺服器運行緊嘅時候，應用程式可能會請作業系統減少休眠，等傳輸可以繼續。

## 兒童

呢個應用程式係網絡檔案伺服器，唔以 13 歲以下兒童為對象。

## 第三方

GitHub 會處理更新檢查同你開啟嘅頁面，如上所述。開發者收唔到呢啲流量。

## 變更

本政策嘅更新會發布喺專案倉庫呢份檔案。

## 聯絡

應用程式名稱: FTPS Server
開發者姓名: Siarhei Kuchuk

問題： [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
