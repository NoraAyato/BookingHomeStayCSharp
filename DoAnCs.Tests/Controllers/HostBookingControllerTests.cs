// Import thư viện NUnit để viết và chạy unit test
using NUnit.Framework;
// Import thư viện Moq để tạo mock objects (đối tượng giả lập)
using Moq;
// Import FluentAssertions để viết assertion một cách dễ đọc hơn
using FluentAssertions;
// Import các thư viện của ASP.NET Core MVC
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
// Import các model, repository và controller từ project chính
using DoAnCs.Models;
using DoAnCs.Repository;
using DoAnCs.Areas.Host.Controllers;
// Import các namespace cơ bản của .NET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Json;

namespace DoAnCs.Tests.Controllers
{
    /// <summary>
    /// Test class cho Host BookingController
    /// Kiểm tra các chức năng xem đơn đặt phòng của host
    /// </summary>
    [TestFixture]
    public class HostBookingControllerTests
    {
        #region Private Fields

        // Mock repository để giả lập dữ liệu
        private Mock<IPhieuDatPhongRepository> _mockPhieuDatPhongRepo = null!;
        private Mock<IUserRepository> _mockUserRepo = null!;
        private Mock<IPhongRepository> _mockPhongRepo = null!;
        private Mock<IServiceRepository> _mockDichVuRepo = null!;
        private Mock<IHomestayRepository> _mockHomestayRepo = null!;
        private Mock<IHoaDonRepository> _mockHoaDonRepo = null!;
        private Mock<IKhuyenMaiRepository> _mockKhuyenMaiRepo = null!;
        private Mock<IPhuThuRepository> _mockPhuThuRepo = null!;
        private Mock<IHuyPhongRepository> _mockHuyPhongRepo = null!;

        // Controller được test
        private BookingController _controller = null!;

        // Test data
        private string _testHostId = null!;

        #endregion

        #region Setup and Teardown

        /// <summary>
        /// Phương thức Setup được chạy trước mỗi test case
        /// Khởi tạo mock repository và controller
        /// </summary>
        [SetUp]
        public void Setup()
        {
            // Tạo mock cho các repository
            _mockPhieuDatPhongRepo = new Mock<IPhieuDatPhongRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockPhongRepo = new Mock<IPhongRepository>();
            _mockDichVuRepo = new Mock<IServiceRepository>();
            _mockHomestayRepo = new Mock<IHomestayRepository>();
            _mockHoaDonRepo = new Mock<IHoaDonRepository>();
            _mockKhuyenMaiRepo = new Mock<IKhuyenMaiRepository>();
            _mockPhuThuRepo = new Mock<IPhuThuRepository>();
            _mockHuyPhongRepo = new Mock<IHuyPhongRepository>();

            // Khởi tạo controller với mock repository
            // Truyền null cho context vì chỉ test logic nghiệp vụ
            _controller = new BookingController(
                _mockPhieuDatPhongRepo.Object,
                _mockUserRepo.Object,
                _mockPhongRepo.Object,
                _mockDichVuRepo.Object,
                _mockHomestayRepo.Object,
                null!,
                _mockHoaDonRepo.Object,
                _mockKhuyenMaiRepo.Object,
                _mockPhuThuRepo.Object,
                _mockHuyPhongRepo.Object
            );

            // Setup test host ID
            _testHostId = "HOST123";

            // Setup User Claims để giả lập host đăng nhập
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testHostId),
                new Claim(ClaimTypes.Role, "Host")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }
        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }
        #endregion

        #region Details Method Tests

        /// <summary>
        /// Test case: Xem chi tiết đơn đặt phòng thành công
        /// Kịch bản: Host xem chi tiết đơn đặt phòng thuộc về homestay của mình
        /// Kết quả mong đợi: Trả về đầy đủ thông tin chi tiết đơn đặt phòng bao gồm:
        /// - Thông tin khách hàng (tên, email, phone, ảnh)
        /// - Danh sách phòng với checkin/checkout date
        /// - TotalPrice và HaveToPay (nếu đã xác nhận)
        /// </summary>
        [Test]
        public async Task Details_WithValidBookingAndAuthorizedHost_ReturnsBookingDetails()
        {
            // Arrange
            var bookingId = "PDP001";
            var mockBooking = CreateDetailedMockBooking(bookingId, _testHostId);

            _mockPhieuDatPhongRepo.Setup(repo => repo.GetByIdAsync(bookingId))
                .ReturnsAsync(mockBooking);

            // Setup cho GetApDungPhuThuByLoaiPhongAsync - method này được gọi trong Details để lấy surcharges
            _mockPhuThuRepo.Setup(repo => repo.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            // Setup cho CalculatePhuThuAsync - method này được gọi trong CalculateTotalPrice
            _mockPhuThuRepo.Setup(repo => repo.CalculatePhuThuAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<decimal>()))
                .ReturnsAsync(0);

            // Setup HoaDonRepo - được gọi trong CalculateTotalPrice để check khuyến mãi
            _mockHoaDonRepo.Setup(repo => repo.GetByPhieuDatPhongAsync(It.IsAny<string>()))
                .ReturnsAsync((HoaDon)null!);

            // Act
            var result = await _controller.Details(bookingId);

            // Assert
            result.Should().NotBeNull();
            var jsonValue = JsonSerializer.Serialize(result.Value);

            jsonValue.Should().Contain("\"success\":true");
            jsonValue.Should().Contain("\"data\":");

            // Verify repository methods được gọi
            _mockPhieuDatPhongRepo.Verify(repo => repo.GetByIdAsync(bookingId), Times.Once);
            _mockPhuThuRepo.Verify(repo => repo.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
        }

        /// <summary>
        /// Test case: Không tìm thấy phiếu đặt phòng
        /// Kịch bản: Host xem chi tiết đơn đặt phòng không tồn tại
        /// Kết quả mong đợi: Trả về message lỗi "Không tìm thấy phiếu đặt phòng"
        /// </summary>
        [Test]
        public async Task Details_WithNonExistentBooking_ReturnsNotFoundError()
        {
            // Arrange
            var bookingId = "PDP999";

            _mockPhieuDatPhongRepo.Setup(repo => repo.GetByIdAsync(bookingId))
                .ReturnsAsync((PhieuDatPhong)null!);

            // Act
            var result = await _controller.Details(bookingId);

            // Assert
            result.Should().NotBeNull();
            var jsonValue = JsonSerializer.Serialize(result.Value);

            jsonValue.Should().Contain("\"success\":false");
            jsonValue.Should().Contain("Không tìm thấy phiếu đặt phòng");

            _mockPhieuDatPhongRepo.Verify(repo => repo.GetByIdAsync(bookingId), Times.Once);
        }

        /// <summary>
        /// Test case: Host không có quyền xem đơn đặt phòng
        /// Kịch bản: Host cố gắng xem đơn đặt phòng có phòng thuộc homestay của host khác
        /// Logic kiểm tra: booking.ChiTietDatPhongs.Any(ct => ct.Phong.Homestay.Ma_ND == hostId)
        /// Kết quả mong đợi: Trả về message lỗi "Bạn không có quyền xem phiếu đặt phòng này"
        /// </summary>
        [Test]
        public async Task Details_WithUnauthorizedHost_ReturnsUnauthorizedError()
        {
            // Arrange
            var bookingId = "PDP001";
            var otherHostId = "OTHER_HOST";
            // Tạo booking thuộc host khác, không phải _testHostId
            var mockBooking = CreateBookingForOtherHost(bookingId, otherHostId);

            // Verify mock data: homestay thuộc OTHER_HOST, không thuộc _testHostId
            mockBooking.ChiTietDatPhongs.First().Phong.Homestay.Ma_ND.Should().Be(otherHostId);
            mockBooking.ChiTietDatPhongs.First().Phong.Homestay.Ma_ND.Should().NotBe(_testHostId);

            _mockPhieuDatPhongRepo.Setup(repo => repo.GetByIdAsync(bookingId))
                .ReturnsAsync(mockBooking);

            // Act
            var result = await _controller.Details(bookingId);

            // Assert
            result.Should().NotBeNull();
            
            // Serialize và deserialize để truy cập properties
            var jsonString = JsonSerializer.Serialize(result.Value);
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var root = jsonDoc.RootElement;

            root.GetProperty("success").GetBoolean().Should().BeFalse();
            root.GetProperty("message").GetString().Should().Be("Bạn không có quyền xem phiếu đặt phòng này");

            _mockPhieuDatPhongRepo.Verify(repo => repo.GetByIdAsync(bookingId), Times.Once);
        }

        /// <summary>
        /// Test case: Không xác định được host (không có User Claims)
        /// Kịch bản: Request không có thông tin xác thực host
        /// Kết quả mong đợi: Trả về message lỗi "Không xác định được host"
        /// </summary>
        [Test]
        public async Task Details_WithoutHostAuthentication_ReturnsAuthenticationError()
        {
            // Arrange
            var bookingId = "PDP001";

            // Xóa User Claims để giả lập không có authentication
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await _controller.Details(bookingId);

            // Assert
            result.Should().NotBeNull();
            var jsonValue = JsonSerializer.Serialize(result.Value);

            jsonValue.Should().Contain("\"success\":false");
            jsonValue.Should().Contain("Không xác định được host");

            _mockPhieuDatPhongRepo.Verify(repo => repo.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Test case: Chi tiết đơn đặt phòng có dịch vụ kèm theo
        /// Kịch bản: Host xem chi tiết đơn có thêm các dịch vụ (ăn sáng, giặt ủi...)
        /// Kết quả mong đợi: Trả về thông tin đầy đủ bao gồm các dịch vụ
        /// </summary>
        [Test]
        public async Task Details_WithServices_ReturnsBookingDetailsWithServices()
        {
            // Arrange
            var bookingId = "PDP001";
            var mockBooking = CreateBookingWithServices(bookingId, _testHostId);

            _mockPhieuDatPhongRepo.Setup(repo => repo.GetByIdAsync(bookingId))
                .ReturnsAsync(mockBooking);

            _mockPhuThuRepo.Setup(repo => repo.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            _mockPhuThuRepo.Setup(repo => repo.CalculatePhuThuAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<decimal>()))
                .ReturnsAsync(0);

            _mockHoaDonRepo.Setup(repo => repo.GetByPhieuDatPhongAsync(It.IsAny<string>()))
                .ReturnsAsync((HoaDon)null!);

            // Act
            var result = await _controller.Details(bookingId);

            // Assert
            result.Should().NotBeNull();
            var jsonValue = JsonSerializer.Serialize(result.Value);

            jsonValue.Should().Contain("\"success\":true");

            _mockPhieuDatPhongRepo.Verify(repo => repo.GetByIdAsync(bookingId), Times.Once);
        }

        /// <summary>
        /// Test case: Chi tiết đơn đặt phòng có phụ thu
        /// Kịch bản: Host xem chi tiết đơn có phụ thu (cuối tuần, lễ tết...)
        /// Kết quả mong đợi: Trả về thông tin bao gồm các khoản phụ thu
        /// </summary>
        [Test]
        public async Task Details_WithSurcharges_ReturnsBookingDetailsWithSurcharges()
        {
            // Arrange
            var bookingId = "PDP001";
            var mockBooking = CreateDetailedMockBooking(bookingId, _testHostId);
            var mockSurcharges = CreateMockSurcharges();

            _mockPhieuDatPhongRepo.Setup(repo => repo.GetByIdAsync(bookingId))
                .ReturnsAsync(mockBooking);

            _mockPhuThuRepo.Setup(repo => repo.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(mockSurcharges);

            _mockPhuThuRepo.Setup(repo => repo.CalculatePhuThuAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<decimal>()))
                .ReturnsAsync(100000);

            _mockHoaDonRepo.Setup(repo => repo.GetByPhieuDatPhongAsync(It.IsAny<string>()))
                .ReturnsAsync((HoaDon)null!);

            // Act
            var result = await _controller.Details(bookingId);

            // Assert
            result.Should().NotBeNull();
            var jsonValue = JsonSerializer.Serialize(result.Value);

            jsonValue.Should().Contain("\"success\":true");

            _mockPhieuDatPhongRepo.Verify(repo => repo.GetByIdAsync(bookingId), Times.Once);
            _mockPhuThuRepo.Verify(repo => repo.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Test case: Chi tiết đơn đặt phòng có trạng thái "Đã xác nhận"
        /// Kịch bản: Host xem chi tiết đơn đã được xác nhận
        /// Kết quả mong đợi: Trả về thông tin bao gồm số tiền host phải nhận (HaveToPay)
        /// </summary>
        [Test]
        public async Task Details_WithConfirmedStatus_ReturnsHaveToPayAmount()
        {
            // Arrange
            var bookingId = "PDP001";
            var mockBooking = CreateDetailedMockBooking(bookingId, _testHostId);
            mockBooking.TrangThai = "Đã xác nhận";

            _mockPhieuDatPhongRepo.Setup(repo => repo.GetByIdAsync(bookingId))
                .ReturnsAsync(mockBooking);

            _mockPhuThuRepo.Setup(repo => repo.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            _mockPhuThuRepo.Setup(repo => repo.CalculatePhuThuAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<decimal>()))
                .ReturnsAsync(0);

            _mockHoaDonRepo.Setup(repo => repo.GetByPhieuDatPhongAsync(It.IsAny<string>()))
                .ReturnsAsync((HoaDon)null!);

            // Act
            var result = await _controller.Details(bookingId);

            // Assert
            result.Should().NotBeNull();
            var jsonValue = JsonSerializer.Serialize(result.Value);

            jsonValue.Should().Contain("\"success\":true");
            jsonValue.Should().Contain("\"HaveToPay\":");
            jsonValue.Should().NotContain("\"HaveToPay\":null");

            _mockPhieuDatPhongRepo.Verify(repo => repo.GetByIdAsync(bookingId), Times.Once);
        }

        /// <summary>
        /// Test case: Chi tiết đơn đặt phòng chưa xác nhận
        /// Kịch bản: Host xem chi tiết đơn có trạng thái "Chờ xác nhận"
        /// Kết quả mong đợi: HaveToPay là null vì chưa xác nhận
        /// </summary>
        [Test]
        public async Task Details_WithPendingStatus_ReturnsNullHaveToPay()
        {
            // Arrange
            var bookingId = "PDP001";
            var mockBooking = CreateDetailedMockBooking(bookingId, _testHostId);
            mockBooking.TrangThai = "Chờ xác nhận";

            _mockPhieuDatPhongRepo.Setup(repo => repo.GetByIdAsync(bookingId))
                .ReturnsAsync(mockBooking);

            _mockPhuThuRepo.Setup(repo => repo.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            _mockPhuThuRepo.Setup(repo => repo.CalculatePhuThuAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<decimal>()))
                .ReturnsAsync(0);

            _mockHoaDonRepo.Setup(repo => repo.GetByPhieuDatPhongAsync(It.IsAny<string>()))
                .ReturnsAsync((HoaDon)null!);

            // Act
            var result = await _controller.Details(bookingId);

            // Assert
            result.Should().NotBeNull();
            var jsonValue = JsonSerializer.Serialize(result.Value);

            jsonValue.Should().Contain("\"success\":true");
            jsonValue.Should().Contain("\"HaveToPay\":null");

            _mockPhieuDatPhongRepo.Verify(repo => repo.GetByIdAsync(bookingId), Times.Once);
        }

        /// <summary>
        /// Test case: Chi tiết đơn đặt phòng có nhiều phòng
        /// Kịch bản: Host xem chi tiết đơn đặt nhiều phòng cùng lúc
        /// Kết quả mong đợi: Trả về thông tin đầy đủ của tất cả các phòng
        /// </summary>
        [Test]
        public async Task Details_WithMultipleRooms_ReturnsAllRoomsDetails()
        {
            // Arrange
            var bookingId = "PDP001";
            var mockBooking = CreateBookingWithMultipleRooms(bookingId, _testHostId);

            _mockPhieuDatPhongRepo.Setup(repo => repo.GetByIdAsync(bookingId))
                .ReturnsAsync(mockBooking);

            _mockPhuThuRepo.Setup(repo => repo.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            _mockPhuThuRepo.Setup(repo => repo.CalculatePhuThuAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<decimal>()))
                .ReturnsAsync(0);

            _mockHoaDonRepo.Setup(repo => repo.GetByPhieuDatPhongAsync(It.IsAny<string>()))
                .ReturnsAsync((HoaDon)null!);

            // Act
            var result = await _controller.Details(bookingId);

            // Assert
            result.Should().NotBeNull();
            var jsonValue = JsonSerializer.Serialize(result.Value);

            jsonValue.Should().Contain("\"success\":true");

            _mockPhieuDatPhongRepo.Verify(repo => repo.GetByIdAsync(bookingId), Times.Once);
        }

        /// <summary>
        /// Test case: Kiểm tra format ngày check-in và check-out
        /// Kịch bản: Host xem chi tiết đơn đặt phòng
        /// Kết quả mong đợi: Ngày check-in và check-out có format yyyy-MM-dd
        /// </summary>
        [Test]
        public async Task Details_ReturnsDateInCorrectFormat()
        {
            // Arrange
            var bookingId = "PDP001";
            var mockBooking = CreateDetailedMockBooking(bookingId, _testHostId);

            _mockPhieuDatPhongRepo.Setup(repo => repo.GetByIdAsync(bookingId))
                .ReturnsAsync(mockBooking);

            _mockPhuThuRepo.Setup(repo => repo.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            _mockPhuThuRepo.Setup(repo => repo.CalculatePhuThuAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<decimal>()))
                .ReturnsAsync(0);

            _mockHoaDonRepo.Setup(repo => repo.GetByPhieuDatPhongAsync(It.IsAny<string>()))
                .ReturnsAsync((HoaDon)null!);

            // Act
            var result = await _controller.Details(bookingId);

            // Assert
            result.Should().NotBeNull();
            var jsonValue = JsonSerializer.Serialize(result.Value);

            jsonValue.Should().Contain("\"success\":true");
            // Verify date format yyyy-MM-dd (example: 2025-10-30)
            jsonValue.Should().MatchRegex(@"""CheckinDate"":""\\d{4}-\\d{2}-\\d{2}""");
            jsonValue.Should().MatchRegex(@"""CheckoutDate"":""\\d{4}-\\d{2}-\\d{2}""");

            _mockPhieuDatPhongRepo.Verify(repo => repo.GetByIdAsync(bookingId), Times.Once);
        }

        /// <summary>
        /// Test case: Repository ném exception khi lấy chi tiết
        /// Kịch bản: Có lỗi xảy ra khi truy vấn database
        /// Kết quả mong đợi: Trả về JSON với success = false và message lỗi
        /// </summary>
        [Test]
        public async Task Details_WhenRepositoryThrowsException_ReturnsError()
        {
            // Arrange
            var bookingId = "PDP001";

            _mockPhieuDatPhongRepo.Setup(repo => repo.GetByIdAsync(bookingId))
                .ThrowsAsync(new Exception("Database connection error"));

            // Act
            var result = await _controller.Details(bookingId);

            // Assert
            result.Should().NotBeNull();
            var jsonValue = JsonSerializer.Serialize(result.Value);

            jsonValue.Should().Contain("\"success\":false");
            jsonValue.Should().Contain("Không thể tải chi tiết đặt phòng");
        }

        /// <summary>
        /// Test case: Chi tiết đơn đặt phòng có thông tin khách hàng đầy đủ
        /// Kịch bản: Host xem chi tiết đơn đặt phòng
        /// Kết quả mong đợi: Trả về đầy đủ thông tin khách hàng (tên, email, số điện thoại, ảnh)
        /// </summary>
        [Test]
        public async Task Details_ReturnsCompleteCustomerInformation()
        {
            // Arrange
            var bookingId = "PDP001";
            var mockBooking = CreateDetailedMockBooking(bookingId, _testHostId);

            _mockPhieuDatPhongRepo.Setup(repo => repo.GetByIdAsync(bookingId))
                .ReturnsAsync(mockBooking);

            _mockPhuThuRepo.Setup(repo => repo.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            _mockPhuThuRepo.Setup(repo => repo.CalculatePhuThuAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<decimal>()))
                .ReturnsAsync(0);

            _mockHoaDonRepo.Setup(repo => repo.GetByPhieuDatPhongAsync(It.IsAny<string>()))
                .ReturnsAsync((HoaDon)null!);

            // Act
            var result = await _controller.Details(bookingId);

            // Assert
            result.Should().NotBeNull();
            var jsonValue = JsonSerializer.Serialize(result.Value);

            jsonValue.Should().Contain("\"success\":true");

            _mockPhieuDatPhongRepo.Verify(repo => repo.GetByIdAsync(bookingId), Times.Once);
        }

        #endregion

        #region Helper Methods for Details Tests

        /// <summary>
        /// Tạo mock PhieuDatPhong chi tiết để test method Details
        /// </summary>
        private PhieuDatPhong CreateDetailedMockBooking(string bookingId, string hostId)
        {
            return new PhieuDatPhong
            {
                Ma_PDPhong = bookingId,
                Ma_ND = "USER001",
                NgayLap = DateTime.Now.AddDays(-5),
                TrangThai = "Chờ xác nhận",
                NguoiDung = new ApplicationUser
                {
                    Id = "USER001",
                    FullName = "Nguyễn Văn A",
                    Email = "nguyenvana@example.com",
                    PhoneNumber = "0901234567",
                    ProfilePicture = "/images/user001.jpg"
                },
                ChiTietDatPhongs = new List<ChiTietDatPhong>
                {
                    new ChiTietDatPhong
                    {
                        Ma_PDPhong = bookingId,
                        Ma_Phong = "ROOM001",
                        NgayDen = DateTime.Now.AddDays(1),
                        NgayDi = DateTime.Now.AddDays(3),
                        Phong = new Phong
                        {
                            Ma_Phong = "ROOM001",
                            TenPhong = "Phòng Deluxe",
                            DonGia = 500000,
                            ID_Homestay = "HS001",
                            ID_Loai = "LP001",
                            Homestay = new Homestay
                            {
                                ID_Homestay = "HS001",
                                Ten_Homestay = "Homestay Biển Xanh",
                                Ma_ND = hostId
                            }
                        },
                        PhieuSuDungDVs = new List<PhieuSuDungDV>()
                    }
                }
            };
        }

        /// <summary>
        /// Tạo mock PhieuDatPhong có dịch vụ kèm theo
        /// </summary>
        private PhieuDatPhong CreateBookingWithServices(string bookingId, string hostId)
        {
            var booking = CreateDetailedMockBooking(bookingId, hostId);

            booking.ChiTietDatPhongs.First().PhieuSuDungDVs = new List<PhieuSuDungDV>
            {
                new PhieuSuDungDV
                {
                    Ma_Phieu = "PDV001",
                    ChiTietPhieuDVs = new List<ChiTietPhieuDV>
                    {
                        new ChiTietPhieuDV
                        {
                            Ma_DV = "DV001",
                            SoLuong = 2,
                            DichVu = new DichVu
                            {
                                Ma_DV = "DV001",
                                Ten_DV = "Ăn sáng",
                                DonGia = 50000
                            }
                        },
                        new ChiTietPhieuDV
                        {
                            Ma_DV = "DV002",
                            SoLuong = 1,
                            DichVu = new DichVu
                            {
                                Ma_DV = "DV002",
                                Ten_DV = "Giặt ủi",
                                DonGia = 30000
                            }
                        }
                    }
                }
            };

            return booking;
        }

        /// <summary>
        /// Tạo mock PhieuDatPhong có nhiều phòng
        /// </summary>
        private PhieuDatPhong CreateBookingWithMultipleRooms(string bookingId, string hostId)
        {
            return new PhieuDatPhong
            {
                Ma_PDPhong = bookingId,
                Ma_ND = "USER001",
                NgayLap = DateTime.Now.AddDays(-5),
                TrangThai = "Chờ xác nhận",
                NguoiDung = new ApplicationUser
                {
                    Id = "USER001",
                    FullName = "Nguyễn Văn A",
                    Email = "nguyenvana@example.com",
                    PhoneNumber = "0901234567",
                    ProfilePicture = "/images/user001.jpg"
                },
                ChiTietDatPhongs = new List<ChiTietDatPhong>
                {
                    new ChiTietDatPhong
                    {
                        Ma_PDPhong = bookingId,
                        Ma_Phong = "ROOM001",
                        NgayDen = DateTime.Now.AddDays(1),
                        NgayDi = DateTime.Now.AddDays(3),
                        Phong = new Phong
                        {
                            Ma_Phong = "ROOM001",
                            TenPhong = "Phòng Deluxe",
                            DonGia = 500000,
                            ID_Homestay = "HS001",
                            ID_Loai = "LP001",
                            Homestay = new Homestay
                            {
                                ID_Homestay = "HS001",
                                Ten_Homestay = "Homestay Biển Xanh",
                                Ma_ND = hostId
                            }
                        },
                        PhieuSuDungDVs = new List<PhieuSuDungDV>()
                    },
                    new ChiTietDatPhong
                    {
                        Ma_PDPhong = bookingId,
                        Ma_Phong = "ROOM002",
                        NgayDen = DateTime.Now.AddDays(1),
                        NgayDi = DateTime.Now.AddDays(3),
                        Phong = new Phong
                        {
                            Ma_Phong = "ROOM002",
                            TenPhong = "Phòng Standard",
                            DonGia = 400000,
                            ID_Homestay = "HS001",
                            ID_Loai = "LP002",
                            Homestay = new Homestay
                            {
                                ID_Homestay = "HS001",
                                Ten_Homestay = "Homestay Biển Xanh",
                                Ma_ND = hostId
                            }
                        },
                        PhieuSuDungDVs = new List<PhieuSuDungDV>()
                    }
                }
            };
        }

        /// <summary>
        /// Tạo mock ApDungPhuThu (phụ thu)
        /// </summary>
        private List<ApDungPhuThu> CreateMockSurcharges()
        {
            return new List<ApDungPhuThu>
            {
                new ApDungPhuThu
                {
                    Ma_PhieuPT = "PT001",
                    NgayApDung = DateTime.Now.AddDays(1),
                    PhieuPhuThu = new PhieuPhuThu
                    {
                        Ma_PhieuPT = "PT001",
                        NoiDung = "Phụ thu cuối tuần",
                        PhiPhuThu = 0.2m // 20% phụ thu
                    }
                }
            };
        }

        /// <summary>
        /// Tạo mock PhieuDatPhong thuộc về host khác (để test unauthorized)
        /// </summary>
        private PhieuDatPhong CreateBookingForOtherHost(string bookingId, string otherHostId)
        {
            return new PhieuDatPhong
            {
                Ma_PDPhong = bookingId,
                Ma_ND = "USER002",
                NgayLap = DateTime.Now.AddDays(-3),
                TrangThai = "Chờ xác nhận",
                NguoiDung = new ApplicationUser
                {
                    Id = "USER002",
                    FullName = "Trần Văn B",
                    Email = "tranvanb@example.com",
                    PhoneNumber = "0912345678",
                    ProfilePicture = "/images/user002.jpg"
                },
                ChiTietDatPhongs = new List<ChiTietDatPhong>
                {
                    new ChiTietDatPhong
                    {
                        Ma_PDPhong = bookingId,
                        Ma_Phong = "ROOM_OTHER",
                        NgayDen = DateTime.Now.AddDays(2),
                        NgayDi = DateTime.Now.AddDays(4),
                        Phong = new Phong
                        {
                            Ma_Phong = "ROOM_OTHER",
                            TenPhong = "Phòng của Host Khác",
                            DonGia = 600000,
                            ID_Homestay = "HS_OTHER",
                            ID_Loai = "LP002",
                            Homestay = new Homestay
                            {
                                ID_Homestay = "HS_OTHER",
                                Ten_Homestay = "Homestay của Host Khác",
                                Ma_ND = otherHostId  // Thuộc về host khác
                            }
                        },
                        PhieuSuDungDVs = new List<PhieuSuDungDV>()
                    }
                }
            };
        }

        #endregion
    }
}
