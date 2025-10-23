# Bài tập: Multiple Socket Connection – NT106.Q12
## Thành viên thực hiện

| MSSV     | Họ và tên          |
| -------- | ------------------ |
| 22521251 | Nguyễn Duy Thế Sơn |
| 24520262 | Nguyễn Tấn Danh    |
| 24521940 | Phan Lê Tuấn       |
| 24521230 | Hứa Thiện Nhân     |
| 22520973 | Ngô Vũ Hạo Nguyên  |

## Giới thiệu

Đây là bài tập môn học **Lập trình mạng căn bản (NT106.Q12)**, sử dụng **C# Windows Forms** và **TCP Socket** để xây dựng ứng dụng nhiều kết nối giữa client và server.

## Chức năng chính

* Kết nối nhiều client đến cùng một server qua TCP.
* Gửi và nhận tin nhắn giữa các client thông qua server.
* Đăng nhập người dùng có lưu thông tin trong cơ sở dữ liệu SQL.
* Giao diện trực quan trên Windows Forms.

## Cấu trúc thư mục

```
TcpUserServer/    --> Mã nguồn chương trình server
TcpUserClient/    --> Mã nguồn chương trình client
UserDB.sql        --> Tập tin SQL tạo cơ sở dữ liệu người dùng
ex3.sln           --> File solution của Visual Studio
```

## Cách chạy chương trình

1. Mở file **ex3.sln** bằng Visual Studio.
2. Chạy dự án **TcpUserServer** trước để khởi động server.
3. Sau đó chạy **TcpUserClient** và kết nối đến server.
4. Đăng nhập bằng tài khoản trong **UserDB.sql** hoặc tạo mới nếu được hỗ trợ.

## Công nghệ sử dụng

* **Ngôn ngữ:** C#
* **Giao diện:** Windows Forms
* **Giao tiếp mạng:** TCP Socket
* **Cơ sở dữ liệu:** SQL Server

## Ghi chú

* Repository này là **public**, được dùng để nộp bài tập môn học.
* Mọi thành viên đều cùng thực hiện và đóng góp mã nguồn.
