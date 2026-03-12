# 🏠 Booking Homestay - Hệ thống Đặt Phòng Homestay

Dự án website đặt phòng Homestay được xây dựng bằng ASP.NET Core 9.0 MVC, cung cấp nền tảng cho việc quản lý và đặt phòng homestay trực tuyến.

## 📋 Mục Lục

- [Tổng Quan](#-tổng-quan)
- [Công Nghệ Sử Dụng](#-công-nghệ-sử-dụng)
- [Tính Năng Chính](#-tính-năng-chính)
- [Yêu Cầu Hệ Thống](#-yêu-cầu-hệ-thống)
- [Hướng Dẫn Cài Đặt](#-hướng-dẫn-cài-đặt)
- [Cấu Hình](#-cấu-hình)
- [Chạy Dự Án](#-chạy-dự-án)
- [Cấu Trúc Dự Án](#-cấu-trúc-dự-án)
- [Database Migration](#-database-migration)
- [Testing](#-testing)

## 🎯 Tổng Quan

Booking Homestay là một hệ thống quản lý và đặt phòng homestay toàn diện, cho phép:
- Người dùng tìm kiếm, xem chi tiết và đặt phòng homestay
- Chủ homestay quản lý phòng, giá cả và đơn đặt phòng
- Quản trị viên quản lý toàn bộ hệ thống

## 🛠 Công Nghệ Sử Dụng

### Backend Framework
- **ASP.NET Core 9.0** - Framework chính
- **Entity Framework Core 9.0** - ORM
- **SQL Server** - Database chính
- **ASP.NET Core Identity** - Quản lý authentication & authorization

### Third-party Services
- **Google OAuth 2.0** - Đăng nhập qua Google
- **MoMo Payment Gateway** - Thanh toán trực tuyến
- **Elasticsearch 7.17** - Tìm kiếm và phân tích dữ liệu
- **Google Maps API** - Hiển thị vị trí homestay
- **SMTP (Gmail)** - Gửi email thông báo

### Libraries & Packages
- **ClosedXML** - Xuất file Excel
- **EPPlus** - Xử lý file Excel
- **NEST** - Elasticsearch client cho .NET

## ✨ Tính Năng Chính

### Người Dùng (Khách Hàng)
- ✅ Đăng ký/Đăng nhập (Email hoặc Google OAuth)
- ✅ Tìm kiếm homestay theo khu vực, giá, tiện nghi
- ✅ Xem thông tin chi tiết homestay và phòng
- ✅ Đặt phòng và thanh toán (MoMo)
- ✅ Quản lý lịch sử đặt phòng
- ✅ Đánh giá và bình luận homestay
- ✅ Hủy đặt phòng (theo chính sách)

### Chủ Homestay
- ✅ Đăng ký và quản lý homestay
- ✅ Thêm/Sửa/Xóa phòng
- ✅ Quản lý giá, khuyến mãi, phụ thu
- ✅ Xem và xử lý đơn đặt phòng
- ✅ Quản lý hợp đồng
- ✅ Thống kê doanh thu

### Quản Trị Viên
- ✅ Quản lý người dùng
- ✅ Duyệt và quản lý homestay
- ✅ Quản lý khu vực, tiện nghi
- ✅ Quản lý tin tức
- ✅ Báo cáo thống kê tổng thể

## 💻 Yêu Cầu Hệ Thống

- **.NET 9.0 SDK** hoặc mới hơn
- **SQL Server 2019** hoặc mới hơn (hoặc SQL Server Express)
- **Visual Studio 2022** (khuyến nghị) hoặc VS Code
- **Elasticsearch 7.17** (tùy chọn - cho tính năng tìm kiếm nâng cao)
- **Git** - Để clone repository

## 📦 Hướng Dẫn Cài Đặt

### 1. Clone Repository

```bash
git clone https://github.com/NoraAyato/BookingHomeStayCSharp.git
cd BookingHomeStayCSharp
```

### 2. Restore NuGet Packages

```bash
cd DoAnCs
dotnet restore
```

### 3. Cài Đặt SQL Server

Đảm bảo bạn đã cài đặt SQL Server và có thể kết nối đến instance.

### 4. Cài Đặt Elasticsearch (Tùy chọn)

Nếu muốn sử dụng tính năng tìm kiếm nâng cao:

```bash
# Download Elasticsearch 7.17.x từ elastic.co
# Giải nén và chạy elasticsearch.bat (Windows)
```

## ⚙ Cấu Hình

### 1. Tạo File Cấu Hình Local

Tạo file `appsettings.Development.json` trong thư mục `DoAnCs/`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=DoAnCs;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  },
  "GoogleMaps": {
    "ApiKey": "YOUR_GOOGLE_MAPS_API_KEY"
  },
  "Elasticsearch": {
    "Url": "https://localhost:9200",
    "IndexName": "homestay-index",
    "Username": "elastic",
    "Password": "YOUR_ELASTICSEARCH_PASSWORD"
  },
  "MoMoSettings": {
    "PartnerCode": "MOMO",
    "AccessKey": "YOUR_MOMO_ACCESS_KEY",
    "SecretKey": "YOUR_MOMO_SECRET_KEY",
    "ApiUrl": "https://test-payment.momo.vn/v2/gateway/api/create",
    "ReturnUrl": "YOUR_DOMAIN/Booking/MoMoCallback",
    "NotifyUrl": "YOUR_DOMAIN/Booking/MoMoCallback"
  },
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "YOUR_EMAIL@gmail.com",
    "SmtpPassword": "YOUR_APP_PASSWORD",
    "SenderEmail": "YOUR_EMAIL@gmail.com",
    "SenderName": "Booking Homestay"
  },
  "AllowedHosts": "*"
}
```

### 2. Cấu Hình Connection String

Cập nhật `ConnectionStrings:DefaultConnection` với thông tin SQL Server của bạn:

```
Server=YOUR_SERVER_NAME\\INSTANCE_NAME;Database=DoAnCs;Trusted_Connection=True;TrustServerCertificate=True
```

**Ví dụ:**
- `Server=localhost;Database=DoAnCs;Trusted_Connection=True;TrustServerCertificate=True`
- `Server=.\\SQLEXPRESS;Database=DoAnCs;Trusted_Connection=True;TrustServerCertificate=True`

### 3. Cấu Hình Google OAuth (Tùy chọn)

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Tạo project mới hoặc chọn project hiện có
3. Enable **Google+ API**
4. Tạo **OAuth 2.0 Client ID** (Web application)
5. Thêm Authorized redirect URIs: `https://localhost:7xxx/signin-google`
6. Copy Client ID và Client Secret vào `appsettings.Development.json`

### 4. Cấu Hình Google Maps API (Tùy chọn)

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Enable **Maps JavaScript API**
3. Tạo **API Key** và copy vào `appsettings.Development.json`
4. Giới hạn API key để bảo mật

### 5. Cấu Hình MoMo Payment (Tùy chọn)

1. Đăng ký tài khoản MoMo Business
2. Lấy thông tin PartnerCode, AccessKey, SecretKey từ MoMo
3. Sử dụng môi trường test: `https://test-payment.momo.vn/v2/gateway/api/create`
4. Cập nhật ReturnUrl và NotifyUrl với domain của bạn

### 6. Cấu Hình Email SMTP (Gmail)

1. Tạo [App Password](https://myaccount.google.com/apppasswords) cho Gmail
2. Cập nhật `SmtpUsername`, `SmtpPassword`, `SenderEmail` trong config

## 🚀 Chạy Dự Án

### 1. Chạy Database Migration

```bash
cd DoAnCs
dotnet ef database update
```

### 2. Build và Chạy Project

**Sử dụng Visual Studio:**
- Mở file `DoAnCs.sln`
- Nhấn `F5` hoặc click `Start Debugging`

**Sử dụng CLI:**
```bash
cd DoAnCs
dotnet run
```

### 3. Truy Cập Website

- **HTTPS**: `https://localhost:7xxx`
- **HTTP**: `http://localhost:5xxx`

(Port number được hiển thị khi chạy project)

## 📁 Cấu Trúc Dự Án

```
DoAnCs/
├── Areas/                      # Areas cho Admin và Host
│   ├── Admin/                  # Khu vực quản trị
│   └── Host/                   # Khu vực chủ homestay
├── Controllers/                # MVC Controllers
│   ├── AccountController.cs    # Xác thực người dùng
│   ├── BookingController.cs    # Đặt phòng
│   ├── HomeController.cs       # Trang chủ
│   ├── HomestayController.cs   # Quản lý homestay
│   └── UserController.cs       # Quản lý người dùng
├── Models/                     # Data Models
│   ├── ApplicationDbContext.cs # EF DbContext
│   ├── ApplicationUser.cs      # User model
│   ├── Homestay.cs            # Homestay model
│   ├── Phong.cs               # Phòng model
│   ├── PhieuDatPhong.cs       # Đặt phòng model
│   ├── Momo/                  # MoMo payment models
│   ├── ViewModels/            # View models
│   └── ...
├── Repository/                 # Repository pattern
├── Services/                   # Business logic services
├── Views/                      # Razor views
├── wwwroot/                    # Static files (CSS, JS, images)
├── Migrations/                 # EF Core migrations
├── appsettings.json           # Config template (NO SECRETS)
└── Program.cs                 # Application entry point

DoAnCs.Tests/                  # Unit tests
└── ...
```

## 🗄 Database Migration

### Tạo Migration Mới

```bash
dotnet ef migrations add TenMigration
```

### Áp Dụng Migration

```bash
dotnet ef database update
```

### Rollback Migration

```bash
dotnet ef database update TenMigrationTruocDo
```

### Xóa Migration Chưa Apply

```bash
dotnet ef migrations remove
```

## 🧪 Testing

Chạy unit tests:

```bash
cd DoAnCs.Tests
dotnet test
```

## 📝 Lưu Ý Bảo Mật

⚠️ **QUAN TRỌNG:**

1. **KHÔNG BAO GIỜ** commit file `appsettings.Development.json` lên Git
2. **KHÔNG** share các API keys, secrets trong code
3. Sử dụng **App Passwords** cho Gmail, không dùng mật khẩu chính
4. Giới hạn quyền truy cập cho API keys
5. Đổi tất cả secrets khi deploy production
6. Sử dụng **Azure Key Vault** hoặc **AWS Secrets Manager** cho production

## 🤝 Contributing

1. Fork project
2. Tạo feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Tạo Pull Request

## 📄 License

Dự án này được phát triển cho mục đích học tập.

## 📧 Liên Hệ

- Repository: [https://github.com/NoraAyato/BookingHomeStayCSharp](https://github.com/NoraAyato/BookingHomeStayCSharp)
- Issues: [https://github.com/NoraAyato/BookingHomeStayCSharp/issues](https://github.com/NoraAyato/BookingHomeStayCSharp/issues)

---

**Phát triển bởi Rose Team** 🌹
