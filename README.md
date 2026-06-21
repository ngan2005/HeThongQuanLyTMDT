# 🛒 Hệ Thống Quản Lý Thương Mại Điện Tử

![Build Status](https://img.shields.io/badge/build-passing-brightgreen)
![.NET Core](https://img.shields.io/badge/.NET%208.0-Purple?logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-Desktop%20App-blue)
![EF Core](https://img.shields.io/badge/Entity%20Framework-Core-green)
![Architecture](https://img.shields.io/badge/Architecture-MVVM-orange)
![License](https://img.shields.io/badge/License-MIT-yellow)

Một hệ thống Quản lý Thương Mại Điện Tử toàn diện, chuẩn doanh nghiệp được phát triển trên nền tảng Windows Presentation Foundation (WPF). Dự án tuân thủ nghiêm ngặt kiến trúc **MVVM (Model-View-ViewModel)**, đảm bảo phân tách rõ ràng giữa giao diện và logic nghiệp vụ, tính dễ bảo trì và hiệu suất hoạt động cao.

## 📋 Mục lục
- [Kiến trúc hệ thống](#-kiến-trúc-hệ-thống)
- [Tính năng cốt lõi](#-tính-năng-cốt-lõi)
- [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
- [Hướng dẫn triển khai](#-hướng-dẫn-triển-khai)
- [Đóng góp](#-đóng-góp)

---

## 🏛 Kiến trúc hệ thống

Ứng dụng được thiết kế dựa trên các nguyên lý phát triển phần mềm hiện đại (SOLID) và áp dụng các Design Pattern tiêu chuẩn:

- **MVVM Pattern**: Tách biệt hoàn toàn giao diện (XAML) khỏi logic nghiệp vụ và trạng thái dữ liệu.
- **Event-Driven Communication**: Sử dụng `MessageBus` trung tâm để điều phối tín hiệu giữa các thành phần một cách lỏng lẻo (ví dụ: kích hoạt sự kiện giữa các ViewModels mà không cần tham chiếu trực tiếp).
- **Service Locator / Dependency Injection**: Tập trung hóa các service xử lý nghiệp vụ (ví dụ: `CartService`, phiên đăng nhập).
- **Code-First Database Design**: Tận dụng tính năng Migration của Entity Framework Core để đồng bộ hóa các model thực thể với lược đồ cơ sở dữ liệu SQL Server.

---

## 🚀 Tính năng cốt lõi

Hệ thống triển khai cơ chế kiểm soát truy cập dựa trên vai trò (Role-Based Access Control - RBAC) với 3 phân hệ hoạt động riêng biệt:

### 👤 1. Phân hệ Khách hàng (Buyer)
- **Danh mục sản phẩm**: Tìm kiếm nâng cao, lọc và xem thông tin chi tiết sản phẩm.
- **Giỏ hàng & Yêu thích**: Quản lý trạng thái giỏ hàng và danh sách sản phẩm yêu thích.
- **Trải nghiệm người dùng (UX)**: Tích hợp các hiệu ứng chuyển đổi mượt mà, hiệu ứng hover tùy chỉnh, animation bay vào giỏ hàng sinh động và các thông báo nổi (Toast Notifications) không gây gián đoạn.

### 🏪 2. Phân hệ Người bán (Seller)
- **Báo cáo & Thống kê**: Trực quan hóa số liệu doanh thu và lưu lượng đơn hàng theo thời gian thực.
- **Quản lý Kho hàng**: Thực hiện đầy đủ các thao tác thêm, sửa, xóa (CRUD) cho danh mục sản phẩm, giá cả và số lượng tồn kho.
- **Quy trình Xử lý Đơn hàng**: Quản lý quy trình từ lúc tiếp nhận, phê duyệt đến khi giao hàng và xử lý yêu cầu hoàn trả.

### 👑 3. Phân hệ Quản trị viên (Admin)
- **Quản lý Định danh & Truy cập**: Kiểm soát người dùng tập trung, phân quyền và khóa tài khoản khi cần thiết.
- **Xét duyệt Đối tác**: Quy trình xác minh và phê duyệt cho các tài khoản nhà bán hàng mới.
- **Marketing & Khuyến mãi**: Quản lý các chiến dịch tiếp thị toàn sàn, định tuyến danh mục và phát hành voucher giảm giá.

---

## 🛠 Công nghệ sử dụng

- **Môi trường & Ngôn ngữ**: .NET 8.0, C# 12
- **Tầng giao diện (Presentation Layer)**: WPF (Windows Presentation Foundation)
- **Thư viện UI**: `MaterialDesignInXamlToolkit`
- **Framework MVVM**: `CommunityToolkit.Mvvm`
- **Tầng truy cập dữ liệu (ORM)**: Entity Framework Core 8.0
- **Hệ quản trị CSDL**: Microsoft SQL Server / LocalDB

---

## ⚙️ Hướng dẫn triển khai

### Yêu cầu tiên quyết
- Visual Studio 2022 (v17.8+ với Workload `.NET Desktop Development`)
- .NET 8.0 SDK
- SQL Server 2019+ hoặc LocalDB

### Cài đặt & Khởi chạy

1. **Clone repository về máy**:
   ```bash
   git clone https://github.com/ngan2005/HeThongQuanLyTMDT.git
   cd TMDT
   ```

2. **Mở Solution**:
   Mở tệp `TMDT.sln` bằng Visual Studio 2022. IDE sẽ tự động khôi phục các gói NuGet cần thiết.

3. **Khởi tạo Cơ sở dữ liệu**:
   Điều hướng đến `Tools` > `NuGet Package Manager` > `Package Manager Console` và thực thi:
   ```powershell
   Update-Database
   ```
   *Lưu ý: Lệnh này sẽ thực thi EF Core migrations, tự động tạo cấu trúc bảng và chèn dữ liệu mẫu (Seeding).*

4. **Khởi động ứng dụng**:
   Đặt `TMDT` làm project khởi động (Startup Project) và nhấn `F5` để biên dịch và chạy.

---

## 🤝 Đóng góp
Chúng tôi tuân theo quy trình Git Flow tiêu chuẩn. Vui lòng tạo branch feature, commit các thay đổi theo định dạng thông điệp commit chuẩn, và mở một Pull Request để review code.

## 📄 Giấy phép
Dự án này được cấp phép theo Giấy phép MIT - xem chi tiết tại tệp [LICENSE](LICENSE).
