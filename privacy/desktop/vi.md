[Languages](README.md)

# Chính sách quyền riêng tư

Cập nhật lần cuối: 13 tháng 9 năm 2026


**FTPS Server** by Siarhei Kuchuk

Tên ứng dụng: FTPS Server
Tên nhà phát triển: Siarhei Kuchuk

Phần mềm là máy chủ FTPS cục bộ (FTP qua TLS). Phần mềm không tạo tài khoản đám mây.
Nhà phát triển không vận hành máy chủ nhận tệp, mật khẩu hoặc dữ liệu sử dụng của bạn.

## Dữ liệu nhà phát triển không thu thập

Ứng dụng không có quảng cáo, phân tích, báo cáo sự cố hay SDK theo dõi. Nhà phát triển không thu thập, bán hoặc chia sẻ dữ liệu cá nhân.

## Dữ liệu lưu trên máy tính của bạn

Cài đặt ứng dụng (bao gồm tên người dùng và mật khẩu FTPS, cổng máy chủ, giới hạn kết nối, cũng như đường dẫn và mật khẩu chứng chỉ tùy chọn) chỉ được lưu trên máy tính này:

- Windows: `%LocalAppData%\FtpsServerApp\settings.json`
- Linux: `~/.local/share/FtpsServerApp/settings.json`

Nếu ứng dụng tạo chứng chỉ tự ký, chứng chỉ được lưu tại:

- Windows: `%LocalAppData%\FtpsServerLibrary\Certificates`
- Linux: `~/.local/share/FtpsServerLibrary/Certificates`

Nhật ký máy chủ có thể được ghi tại:

- Windows: `%AppData%\ftps-server\logs`
- Linux: `~/.config/ftps-server/logs`

Các giá trị đó không được tải lên nhà phát triển. Gỡ ứng dụng hoặc các thư mục đó sẽ xóa chúng. **Tệp** đã chia sẻ vẫn nằm trong thư mục bạn chọn; ứng dụng không sao chép chúng sang máy chủ của nhà phát triển.

Thư mục được chọn bằng bộ chọn thư mục của hệ thống. Ứng dụng chỉ chia sẻ các thư mục bạn cấp quyền.

Không dùng máy chủ của nhà phát triển để lưu dữ liệu của bạn.

## Sử dụng mạng

### Kiểm tra cập nhật

Ứng dụng có thể yêu cầu bản phát hành GitHub mới nhất:

`https://api.github.com/repos/drweb86/dotnet-ftps-server/releases/latest`

GitHub (Microsoft) nhận một yêu cầu HTTPS thông thường (địa chỉ IP, user-agent, thời gian). Nhà phát triển không nhận lưu lượng đó.

### Máy chủ FTPS

Khi máy chủ đang chạy, nó lắng nghe trên mạng cục bộ của bạn để các máy khách FTPS bạn cấu hình có thể đọc hoặc ghi các thư mục đã chia sẻ, bằng tên người dùng và mật khẩu bạn đặt. Lưu lượng đó ở giữa các thiết bị của bạn (và bất kỳ ai trên mạng có thông tin đăng nhập đó). Nhà phát triển không phải bên tham gia.

Bạn chịu trách nhiệm về ai có thể tới cổng, thư mục nào bạn chia sẻ và mật khẩu mạnh đến mức nào.

### Liên kết bạn mở

Ứng dụng có thể mở các trang này trong trình duyệt hệ thống. Các trang đó có chính sách quyền riêng tư riêng:

- Trang chủ dự án: [github.com/drweb86/dotnet-ftps-server](https://github.com/drweb86/dotnet-ftps-server)
- Giấy phép: [LICENSE](https://raw.githubusercontent.com/drweb86/dotnet-ftps-server/refs/heads/main/LICENSE)
- Bản phát hành mới nhất: [github.com/drweb86/dotnet-ftps-server/releases/latest](https://github.com/drweb86/dotnet-ftps-server/releases/latest)

## Hành vi cục bộ khác

Khi máy chủ đang chạy, ứng dụng có thể yêu cầu hệ điều hành giảm ngủ để việc truyền tiếp tục.

## Trẻ em

Ứng dụng là máy chủ tệp mạng. Ứng dụng không hướng tới trẻ em dưới 13 tuổi.

## Bên thứ ba

GitHub xử lý yêu cầu kiểm tra cập nhật và các trang bạn mở, như trên. Nhà phát triển không nhận lưu lượng đó.

## Thay đổi

Cập nhật chính sách này sẽ được đăng trong tệp này trong kho lưu trữ dự án.

## Liên hệ

Tên ứng dụng: FTPS Server
Tên nhà phát triển: Siarhei Kuchuk

Câu hỏi: [github.com/drweb86/dotnet-ftps-server/issues](https://github.com/drweb86/dotnet-ftps-server/issues)
