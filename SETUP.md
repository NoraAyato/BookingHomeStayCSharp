# 🔧 Hướng Dẫn Cài Đặt Chi Tiết - Booking Homestay

Tài liệu này cung cấp hướng dẫn chi tiết từng bước để setup và chạy dự án Booking Homestay trên môi trường local.

## 📋 Mục Lục

- [Chuẩn Bị Môi Trường](#-chuẩn-bị-môi-trường)
- [Cài Đặt Dependencies](#-cài-đặt-dependencies)
- [Cấu Hình Database](#-cấu-hình-database)
- [Cấu Hình Third-party Services](#-cấu-hình-third-party-services)
- [Khởi Chạy Dự Án](#-khởi-chạy-dự-án)
- [Troubleshooting](#-troubleshooting)

## 🔨 Chuẩn Bị Môi Trường

### 1. Cài Đặt .NET 9.0 SDK

**Windows:**
1. Truy cập [https://dotnet.microsoft.com/download/dotnet/9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Download **.NET 9.0 SDK** (x64)
3. Chạy installer và làm theo hướng dẫn
4. Kiểm tra cài đặt:
```bash
dotnet --version
# Kết quả: 9.0.x
```

**macOS/Linux:**
```bash
# Sử dụng script từ Microsoft
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0
```

### 2. Cài Đặt SQL Server

**Windows - SQL Server Express (Khuyến nghị cho Dev):**
1. Download [SQL Server 2022 Express](https://www.microsoft.com/sql-server/sql-server-downloads)
2. Chọn **Basic installation**
3. Ghi nhớ instance name (mặc định: `SQLEXPRESS`)
4. Download và cài đặt [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/sql/ssms/download-sql-server-management-studio-ssms)

**Connection String Examples:**
```
Server=localhost\SQLEXPRESS;Database=DoAnCs;Trusted_Connection=True;TrustServerCertificate=True
Server=(localdb)\MSSQLLocalDB;Database=DoAnCs;Trusted_Connection=True;TrustServerCertificate=True
Server=.;Database=DoAnCs;Trusted_Connection=True;TrustServerCertificate=True
```

**macOS/Linux - SQL Server Docker:**
```bash
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
   -p 1433:1433 --name sqlserver \
   -d mcr.microsoft.com/mssql/server:2022-latest
```

Connection String:
```
Server=localhost,1433;Database=DoAnCs;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
```

### 3. Cài Đặt IDE

**Option 1: Visual Studio 2022 (Khuyến nghị cho Windows)**
1. Download [Visual Studio 2022 Community](https://visualstudio.microsoft.com/downloads/)
2. Trong installer, chọn workload:
   - **ASP.NET and web development**
   - **.NET desktop development**
3. Individual components:
   - **.NET 9.0 Runtime**
   - **Entity Framework 9 tools**

**Option 2: Visual Studio Code (Cross-platform)**
1. Download [VS Code](https://code.visualstudio.com/)
2. Cài đặt extensions:
   - **C# Dev Kit** (ms-dotnettools.csdevkit)
   - **C#** (ms-dotnettools.csharp)
   - **NuGet Package Manager**
   - **SQL Server (mssql)**

### 4. Cài Đặt Git

```bash
# Windows - Sử dụng installer
https://git-scm.com/download/win

# macOS
brew install git

# Linux (Ubuntu/Debian)
sudo apt-get install git
```

## 📦 Cài Đặt Dependencies

### 1. Clone Repository

```bash
# HTTPS
git clone https://github.com/NoraAyato/BookingHomeStayCSharp.git

# SSH (nếu đã setup SSH key)
git clone git@github.com:NoraAyato/BookingHomeStayCSharp.git

cd BookingHomeStayCSharp
```

### 2. Restore NuGet Packages

```bash
cd DoAnCs
dotnet restore
```

Nếu gặp lỗi restore, thử:
```bash
dotnet nuget locals all --clear
dotnet restore --force
```

### 3. Verify Dependencies

```bash
dotnet list package
```

Expected packages:
- Microsoft.EntityFrameworkCore.SqlServer (9.0.3)
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (9.0.3)
- Microsoft.AspNetCore.Authentication.Google (9.0.3)
- NEST (7.17.5)
- ClosedXML, EPPlus...

## 🗄 Cấu Hình Database

### 1. Tạo Database

**Option A: Sử dụng SSMS (SQL Server Management Studio)**
1. Mở SSMS
2. Connect to server
3. Right-click **Databases** → **New Database**
4. Đặt tên: `DoAnCs`
5. Click **OK**

**Option B: Sử dụng T-SQL**
```sql
CREATE DATABASE DoAnCs;
GO
```

**Option C: Để EF Core tự tạo (khuyến nghị)**
- EF Core sẽ tự tạo database khi chạy migration

### 2. Cấu Hình Connection String

Tạo file `appsettings.Development.json` trong thư mục `DoAnCs/`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=DoAnCs;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

**Thay thế `YOUR_SERVER` bằng:**
- `localhost\SQLEXPRESS` (SQL Server Express)
- `(localdb)\MSSQLLocalDB` (LocalDB)
- `.` hoặc `localhost` (Default instance)
- `localhost,1433` (Docker SQL Server)

### 3. Test Connection

**Sử dụng VS Code với SQL Server extension:**
```
Ctrl+Shift+P → "MS SQL: Connect"
Enter server name, authentication type
```

**Sử dụng dotnet CLI:**
```bash
# Install EF Core CLI tools nếu chưa có
dotnet tool install --global dotnet-ef

# Verify version
dotnet ef --version
```

### 4. Chạy Migrations

```bash
# Di chuyển đến thư mục project
cd DoAnCs

# Tạo database và apply migrations
dotnet ef database update
```

**Expected Output:**
```
Build started...
Build succeeded.
Applying migration '20250417083909_initial'.
Applying migration '20250419130700_changeforeignkey'.
...
Done.
```

### 5. Verify Database Schema

Kiểm tra database đã được tạo với các bảng:
- AspNetUsers
- AspNetRoles
- Homestay
- Phong
- PhieuDatPhong
- HoaDon
- DanhGia
- ...

## 🔑 Cấu Hình Third-party Services

### 1. Google OAuth 2.0 (Đăng nhập Google)

#### Bước 1: Tạo Google Cloud Project

1. Truy cập [Google Cloud Console](https://console.cloud.google.com/)
2. Đăng nhập với Google account
3. Click **Select a project** → **New Project**
4. Nhập tên project: `Booking-Homestay-Dev`
5. Click **Create**

#### Bước 2: Enable Google+ API

1. Trong project vừa tạo, vào **APIs & Services** → **Library**
2. Tìm **Google+ API**
3. Click **Enable**

#### Bước 3: Tạo OAuth 2.0 Credentials

1. Vào **APIs & Services** → **Credentials**
2. Click **Create Credentials** → **OAuth client ID**
3. Chọn **Application type**: **Web application**
4. Nhập **Name**: `Homestay Web Client`
5. **Authorized JavaScript origins**: 
   - `https://localhost:7xxx` (thay xxx bằng port của bạn)
6. **Authorized redirect URIs**:
   - `https://localhost:7xxx/signin-google`
7. Click **Create**
8. **Copy Client ID và Client Secret**

#### Bước 4: Cập nhật Configuration

Thêm vào `appsettings.Development.json`:
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
      "ClientSecret": "GOCSPX-xxxxxxxxxxxxxxxxxxxxx"
    }
  }
}
```

### 2. Google Maps API (Hiển thị bản đồ)

#### Bước 1: Enable Maps JavaScript API

1. Trong Google Cloud Console
2. **APIs & Services** → **Library**
3. Tìm **Maps JavaScript API**
4. Click **Enable**

#### Bước 2: Tạo API Key

1. **APIs & Services** → **Credentials**
2. Click **Create Credentials** → **API key**
3. Copy API key
4. Click **Restrict Key**:
   - **Application restrictions**: HTTP referrers
   - **Website restrictions**: `localhost:*/*`
   - **API restrictions**: Maps JavaScript API
5. Click **Save**

#### Bước 3: Cập nhật Configuration

```json
{
  "GoogleMaps": {
    "ApiKey": "AIzaSyXXXXXXXXXXXXXXXXXXXXXXXXX"
  }
}
```

### 3. MoMo Payment Gateway

#### Bước 1: Đăng ký MoMo Business (môi trường Test)

1. Liên hệ MoMo để đăng ký tài khoản test
2. Hoặc sử dụng thông tin test có sẵn (nếu có)

#### Bước 2: Lấy Credentials

Bạn sẽ nhận được:
- **PartnerCode**: MOMO
- **AccessKey**: F8BBA842ECF85
- **SecretKey**: K951B6PE1waDMi640xX08PD3vg6EkVlz

#### Bước 3: Cấu hình Local Development

```json
{
  "MoMoSettings": {
    "PartnerCode": "MOMO",
    "AccessKey": "YOUR_ACCESS_KEY",
    "SecretKey": "YOUR_SECRET_KEY",
    "ApiUrl": "https://test-payment.momo.vn/v2/gateway/api/create",
    "ReturnUrl": "https://localhost:7xxx/Booking/MoMoCallback",
    "NotifyUrl": "https://localhost:7xxx/Booking/MoMoCallback"
  }
}
```

**Lưu ý:** Thay `7xxx` bằng port HTTPS của bạn

#### Bước 4: Setup Ngrok (để test callback từ MoMo)

```bash
# Download ngrok từ https://ngrok.com/download
# Chạy ngrok
ngrok http https://localhost:7xxx

# Copy HTTPS URL (ví dụ: https://abc123.ngrok.io)
# Cập nhật ReturnUrl và NotifyUrl:
"ReturnUrl": "https://abc123.ngrok.io/Booking/MoMoCallback",
"NotifyUrl": "https://abc123.ngrok.io/Booking/MoMoCallback"
```

### 4. Email Service (Gmail SMTP)

#### Bước 1: Tạo App Password

1. Truy cập [Google Account Security](https://myaccount.google.com/security)
2. Enable **2-Step Verification** (nếu chưa bật)
3. Vào **App passwords** ([direct link](https://myaccount.google.com/apppasswords))
4. Chọn app: **Mail**, device: **Other (Custom name)**
5. Nhập tên: `Homestay Booking`
6. Click **Generate**
7. **Copy 16-digit password** (ví dụ: `abcd efgh ijkl mnop`)

#### Bước 2: Cấu hình SMTP

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "youremail@gmail.com",
    "SmtpPassword": "abcdefghijklmnop",
    "SenderEmail": "youremail@gmail.com",
    "SenderName": "Booking Homestay"
  }
}
```

**Lưu ý:** 
- Không dùng mật khẩu Gmail chính
- App password không có khoảng trắng
- Enable "Less secure app access" có thể cần thiết

### 5. Elasticsearch (Tùy chọn - Tìm kiếm nâng cao)

#### Option A: Docker (Khuyến nghị)

```bash
# Pull Elasticsearch 7.17
docker pull docker.elastic.co/elasticsearch/elasticsearch:7.17.24

# Chạy Elasticsearch
docker run -d \
  --name elasticsearch \
  -p 9200:9200 \
  -p 9300:9300 \
  -e "discovery.type=single-node" \
  -e "xpack.security.enabled=true" \
  -e "ELASTIC_PASSWORD=YourPassword123" \
  docker.elastic.co/elasticsearch/elasticsearch:7.17.24

# Kiểm tra
curl -u elastic:YourPassword123 http://localhost:9200
```

#### Option B: Download và cài đặt

1. Download [Elasticsearch 7.17](https://www.elastic.co/downloads/past-releases/elasticsearch-7-17-0)
2. Giải nén
3. Chỉnh sửa `config/elasticsearch.yml`:
```yaml
xpack.security.enabled: true
```
4. Khởi động:
```bash
# Windows
bin\elasticsearch.bat

# Linux/macOS
bin/elasticsearch
```
5. Đặt password:
```bash
bin\elasticsearch-setup-passwords interactive
```

#### Bước 3: Cấu hình

```json
{
  "Elasticsearch": {
    "Url": "https://localhost:9200",
    "IndexName": "homestay-index",
    "Username": "elastic",
    "Password": "YourPassword123"
  }
}
```

## 🚀 Khởi Chạy Dự Án

### 1. Build Project

```bash
cd DoAnCs
dotnet build
```

Expected output:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### 2. Chạy Project

**Option A: Visual Studio**
1. Mở `DoAnCs.sln`
2. Nhấn `F5` hoặc click **Start Debugging**
3. Hoặc `Ctrl+F5` để chạy không debug

**Option B: CLI**
```bash
dotnet run
```

**Option C: Watch mode (auto-reload khi có thay đổi)**
```bash
dotnet watch run
```

### 3. Truy Cập Website

Mở browser và truy cập:
- **HTTPS**: `https://localhost:7xxx`
- **HTTP**: `http://localhost:5xxx`

(Port được hiển thị khi chạy project)

### 4. Tạo Admin Account (Lần đầu)

**Option 1: Sử dụng Seeding Data**
- Nếu có file seeding, data sẽ tự động được tạo

**Option 2: Tạo thủ công qua Database**
```sql
-- Tạo admin role nếu chưa có
INSERT INTO AspNetRoles (Id, Name, NormalizedName) 
VALUES (NEWID(), 'Admin', 'ADMIN');

-- Đăng ký user qua website, sau đó add role
INSERT INTO AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id 
FROM AspNetUsers u, AspNetRoles r
WHERE u.Email = 'youradmin@email.com' AND r.Name = 'Admin';
```

## 🐛 Troubleshooting

### Lỗi: "Cannot connect to SQL Server"

**Giải pháp:**
```bash
# Kiểm tra SQL Server đang chạy
# Windows: Services → SQL Server (SQLEXPRESS)

# Test connection string
dotnet ef database update --verbose
```

### Lỗi: "Login failed for user"

**Giải pháp:**
- Kiểm tra Windows Authentication: `Trusted_Connection=True`
- Hoặc dùng SQL Authentication: `User Id=sa;Password=xxx`
- Add `TrustServerCertificate=True` vào connection string

### Lỗi: "The certificate chain was issued by an authority that is not trusted"

**Giải pháp:**
Thêm vào connection string:
```
TrustServerCertificate=True
```

### Lỗi: "A network-related or instance-specific error"

**Giải pháp:**
```bash
# Enable TCP/IP cho SQL Server
# SQL Server Configuration Manager → SQL Server Network Configuration → Protocols for SQLEXPRESS → TCP/IP → Enable
# Restart SQL Server service
```

### Lỗi: "Entity Framework Core tools version mismatch"

**Giải pháp:**
```bash
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef --version 9.0.3
```

### Lỗi: "Unable to resolve service for type ApplicationDbContext"

**Giải pháp:**
- Kiểm tra `Program.cs` đã register DbContext chưa
- Kiểm tra `appsettings.Development.json` có ConnectionString chưa

### Elasticsearch không kết nối được

**Giải pháp:**
```bash
# Kiểm tra Elasticsearch đang chạy
curl http://localhost:9200

# Nếu enable security, add credentials
curl -u elastic:password http://localhost:9200
```

### Google OAuth redirect URI mismatch

**Giải pháp:**
- Kiểm tra port trong Google Cloud Console khớp với port local
- Phải dùng HTTPS
- Redirect URI format: `https://localhost:PORT/signin-google`

### MoMo callback không hoạt động ở local

**Giải pháp:**
- Sử dụng ngrok để expose local server
- Cập nhật ReturnUrl/NotifyUrl với ngrok URL
- Log để debug callback data

## 📚 Tài Liệu Tham Khảo

- [ASP.NET Core Documentation](https://learn.microsoft.com/aspnet/core/)
- [Entity Framework Core](https://learn.microsoft.com/ef/core/)
- [Google OAuth 2.0](https://developers.google.com/identity/protocols/oauth2)
- [MoMo Payment API](https://developers.momo.vn/)
- [Elasticsearch .NET Client](https://www.elastic.co/guide/en/elasticsearch/client/net-api/current/index.html)

---

Nếu gặp vấn đề khác không nằm trong tài liệu này, vui lòng tạo [Issue trên GitHub](https://github.com/NoraAyato/BookingHomeStayCSharp/issues).
