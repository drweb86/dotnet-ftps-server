[Languages](README.md)

# プライバシーポリシー

最終更新日：2026年9月13日


**FTPS Server** by Siarhei Kuchuk

アプリケーション名: FTPS Server
開発者名: Siarhei Kuchuk

本ソフトウェアはローカルの FTPS（TLS 上の FTP）サーバーです。クラウドアカウントは作成しません。
開発者は、お客様のファイル、パスワード、利用データを受け取るバックエンドを運用していません。

## 開発者が収集しないデータ

本アプリに広告、解析、クラッシュ報告、追跡 SDK は含まれません。開発者は個人データを収集・販売・共有しません。

## コンピューターに保存されるデータ

アプリ設定（FTPS のユーザー名とパスワード、サーバーポート、接続数の上限、任意の証明書パスとパスワードを含む）はこのコンピューターにのみ保存されます：

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

アプリが自己署名証明書を作成する場合、次の場所に保存されます：

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

サーバーログは次の場所に書き込まれる場合があります：

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

これらの値は開発者にアップロードされません。アプリやこれらのフォルダーを削除すると消えます。共有した **ファイル** は選んだフォルダーに残り、アプリは開発者のサーバーへコピーしません。

フォルダーはシステムのフォルダー選択画面で選びます。アプリは許可したフォルダーだけを共有します。

開発者のサーバーはお客様のデータの保存には使用しません。

## ネットワークの利用

### 更新の確認

アプリは GitHub の最新リリースを要求する場合があります：

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub（Microsoft）は通常の HTTPS リクエスト（IP アドレス、ユーザーエージェント、時刻）を受け取ります。開発者はその通信を受け取りません。

### FTPS サーバー

サーバーの実行中はローカルネットワークで待ち受け、設定した FTPS クライアントが、指定したユーザー名とパスワードで共有フォルダーを読み書きできます。その通信はお客様の端末間（およびその認証情報を持つネットワーク上の者）に留まります。開発者は関与しません。

ポートに誰が到達できるか、どのフォルダーを共有するか、パスワードの強度はお客様の責任です。

### 開くリンク

アプリはこれらのページをシステムのブラウザーで開けます。それらのサイトには独自のプライバシーポリシーがあります：

- プロジェクトのホームページ: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- ライセンス: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- 最新リリース: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## その他のローカル動作

サーバーの実行中、アプリは転送が続くよう、オペレーティングシステムにスリープを減らすよう依頼する場合があります。

## 子ども

本アプリはネットワークファイルサーバーであり、13歳未満の子どもを対象としていません。

## 第三者

GitHub は上記のとおり、更新確認のリクエストと開いたページを処理します。開発者はその通信を受け取りません。

## 変更

本ポリシーの更新は、プロジェクトリポジトリ内のこのファイルに掲載されます。

## 連絡先

アプリケーション名: FTPS Server
開発者名: Siarhei Kuchuk

お問い合わせ： [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
