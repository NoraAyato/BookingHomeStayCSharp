using NUnit.Framework;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DoAnCs.Models;
using DoAnCs.Repository;
using DoAnCs.Areas.Host.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DoAnCs.Tests.Controllers
{
    /// <summary>
    /// Unit tests cho BookingController.Details() trong Host Area
    /// Test chức năng xem chi tiết phiếu đặt phòng của Host
    /// Coverage: Authorization, Data validation, Business logic
    /// </summary>
    [TestFixture]
    public class HostBookingDetailsControllerTests
    {
        #region Private Fields

        private Mock<IPhieuDatPhongRepository> _mockPhieuDatPhongRepo;
        private Mock<IUserRepository> _mockUserRepo;
        private Mock<IPhongRepository> _mockPhongRepo;
        private Mock<IServiceRepository> _mockDichVuRepo;
        private Mock<IHomestayRepository> _mockHomestayRepo;
        private Mock<ApplicationDbContext> _mockContext;
        private Mock<IHoaDonRepository> _mockHoaDonRepo;
        private Mock<IKhuyenMaiRepository> _mockKhuyenMaiRepo;
        private Mock<IPhuThuRepository> _mockPhuThuRepo;
        private Mock<IHuyPhongRepository> _mockHuyPhongRepo;
        
        private BookingController _controller;

        private const string TEST_HOST_ID = "host-123";
        private const string TEST_BOOKING_ID = "PDP-001";
        private const string OTHER_HOST_ID = "host-999";
        private const string TEST_CUSTOMER_ID = "customer-456";

        #endregion

        #region Setup and Teardown

        [SetUp]
        public void SetUp()
        {
            _mockPhieuDatPhongRepo = new Mock<IPhieuDatPhongRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockPhongRepo = new Mock<IPhongRepository>();
            _mockDichVuRepo = new Mock<IServiceRepository>();
            _mockHomestayRepo = new Mock<IHomestayRepository>();
            _mockContext = new Mock<ApplicationDbContext>();
            _mockHoaDonRepo = new Mock<IHoaDonRepository>();
            _mockKhuyenMaiRepo = new Mock<IKhuyenMaiRepository>();
            _mockPhuThuRepo = new Mock<IPhuThuRepository>();
            _mockHuyPhongRepo = new Mock<IHuyPhongRepository>();

            _controller = new BookingController(
                _mockPhieuDatPhongRepo.Object,
                _mockUserRepo.Object,
                _mockPhongRepo.Object,
                _mockDichVuRepo.Object,
                _mockHomestayRepo.Object,
                _mockContext.Object,
                _mockHoaDonRepo.Object,
                _mockKhuyenMaiRepo.Object,
                _mockPhuThuRepo.Object,
                _mockHuyPhongRepo.Object
            );

            // Setup default authenticated user
            SetupAuthenticatedUser(TEST_HOST_ID);
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }

        #endregion

        #region Happy Path Tests

        /// <summary>
        /// Test: Host xem chi tiết booking hợp lệ của mình
        /// Verify: Trả về đầy đủ thông tin booking với success = true
        /// </summary>
        [Test]
        public async Task Details_ValidBookingBelongsToHost_ReturnsSuccessWithFullDetails()
        {
            // Arrange
            var booking = CreateValidBooking(TEST_HOST_ID);
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(TEST_BOOKING_ID))
                .ReturnsAsync(booking);

            _mockPhuThuRepo.Setup(x => x.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(true);
            jsonValue.data.Should().NotBeNull();
            
            // Verify customer info
            ((string)jsonValue.data.CustomerName).Should().Be("John Doe");
            ((string)jsonValue.data.CustomerEmail).Should().Be("john@example.com");
            
            // Verify booking info
            ((string)jsonValue.data.Ma_PDPhong).Should().Be(TEST_BOOKING_ID);
            ((string)jsonValue.data.TrangThai).Should().NotBeNullOrEmpty();

            _mockPhieuDatPhongRepo.Verify(x => x.GetByIdAsync(TEST_BOOKING_ID), Times.Once);
        }

        /// <summary>
        /// Test: Xem chi tiết booking có nhiều phòng
        /// Verify: Trả về tất cả thông tin phòng trong booking
        /// </summary>
        [Test]
        public async Task Details_BookingWithMultipleRooms_ReturnsAllRoomDetails()
        {
            // Arrange
            var booking = CreateBookingWithMultipleRooms(TEST_HOST_ID);
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(TEST_BOOKING_ID))
                .ReturnsAsync(booking);

            _mockPhuThuRepo.Setup(x => x.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(true);
            
            var rooms = ((IEnumerable<object>)jsonValue.data.Rooms).ToList();
            rooms.Should().HaveCount(2);
        }

        /// <summary>
        /// Test: Xem chi tiết booking có dịch vụ
        /// Verify: Services được list đầy đủ trong mỗi phòng
        /// </summary>
        [Test]
        public async Task Details_BookingWithServices_ReturnsServiceDetails()
        {
            // Arrange
            var booking = CreateBookingWithServices(TEST_HOST_ID);
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(TEST_BOOKING_ID))
                .ReturnsAsync(booking);

            _mockPhuThuRepo.Setup(x => x.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(true);
            
            var rooms = ((IEnumerable<object>)jsonValue.data.Rooms).ToList();
            var firstRoom = (dynamic)rooms[0];
            var services = ((IEnumerable<object>)firstRoom.Services).ToList();
            
            services.Should().NotBeEmpty();
        }

        /// <summary>
        /// Test: Xem chi tiết booking có phụ thu
        /// Verify: Surcharges được tính toán và hiển thị đúng
        /// </summary>
        [Test]
        public async Task Details_BookingWithSurcharges_ReturnsSurchargeDetails()
        {
            // Arrange
            var booking = CreateValidBooking(TEST_HOST_ID);
            var surcharges = CreateTestSurcharges();
            
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(TEST_BOOKING_ID))
                .ReturnsAsync(booking);

            _mockPhuThuRepo.Setup(x => x.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(surcharges);

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(true);
            
            var rooms = ((IEnumerable<object>)jsonValue.data.Rooms).ToList();
            var firstRoom = (dynamic)rooms[0];
            var surchargesList = ((IEnumerable<object>)firstRoom.Surcharges).ToList();
            
            surchargesList.Should().NotBeEmpty();
        }

        /// <summary>
        /// Test: Booking với trạng thái "Đã xác nhận" có HaveToPay
        /// Verify: TotalPrice và HaveToPay được tính toán đúng
        /// </summary>
        [Test]
        public async Task Details_ConfirmedBooking_ReturnsHaveToPayAmount()
        {
            // Arrange
            var booking = CreateValidBooking(TEST_HOST_ID);
            booking.TrangThai = "Đã xác nhận";
            
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(TEST_BOOKING_ID))
                .ReturnsAsync(booking);

            _mockPhuThuRepo.Setup(x => x.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(true);
            
            // HaveToPay phải có giá trị cho booking đã xác nhận
            ((decimal?)jsonValue.data.HaveToPay).Should().NotBeNull();
            ((decimal?)jsonValue.data.HaveToPay).Should().BeGreaterThan(0);
        }

        /// <summary>
        /// Test: Booking với trạng thái không phải "Đã xác nhận"
        /// Verify: HaveToPay = null
        /// </summary>
        [Test]
        public async Task Details_PendingBooking_HaveToPayIsNull()
        {
            // Arrange
            var booking = CreateValidBooking(TEST_HOST_ID);
            booking.TrangThai = "Chờ xác nhận";
            
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(TEST_BOOKING_ID))
                .ReturnsAsync(booking);

            _mockPhuThuRepo.Setup(x => x.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(true);
            
            // HaveToPay phải null cho booking chưa xác nhận
            ((decimal?)jsonValue.data.HaveToPay).Should().BeNull();
        }

        #endregion

        #region Authorization Tests

        /// <summary>
        /// Test: User không được authenticate
        /// Verify: Trả về error về việc không xác định được host
        /// </summary>
        [Test]
        public async Task Details_UnauthenticatedUser_ReturnsUnauthorizedError()
        {
            // Arrange
            SetupAuthenticatedUser(null); // No user ID

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(false);
            ((string)jsonValue.message).Should().Contain("Không xác định được host");

            _mockPhieuDatPhongRepo.Verify(x => x.GetByIdAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Test: Host cố gắng xem booking không thuộc về mình
        /// Verify: Trả về error về quyền truy cập
        /// </summary>
        [Test]
        public async Task Details_BookingNotBelongsToHost_ReturnsForbiddenError()
        {
            // Arrange
            var booking = CreateValidBooking(OTHER_HOST_ID); // Different host
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(TEST_BOOKING_ID))
                .ReturnsAsync(booking);

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(false);
            ((string)jsonValue.message).Should().Contain("không có quyền xem");

            _mockPhieuDatPhongRepo.Verify(x => x.GetByIdAsync(TEST_BOOKING_ID), Times.Once);
        }

        #endregion

        #region Error Handling Tests

        /// <summary>
        /// Test: Booking ID không tồn tại trong database
        /// Verify: Trả về error message phù hợp
        /// </summary>
        [Test]
        public async Task Details_BookingNotFound_ReturnsNotFoundError()
        {
            // Arrange
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(TEST_BOOKING_ID))
                .ReturnsAsync((PhieuDatPhong)null);

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(false);
            ((string)jsonValue.message).Should().Contain("Không tìm thấy phiếu đặt phòng");

            _mockPhieuDatPhongRepo.Verify(x => x.GetByIdAsync(TEST_BOOKING_ID), Times.Once);
        }

        /// <summary>
        /// Test: Repository throw exception
        /// Verify: Exception được catch và trả về error message
        /// </summary>
        [Test]
        public async Task Details_RepositoryThrowsException_ReturnsErrorMessage()
        {
            // Arrange
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(TEST_BOOKING_ID))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(false);
            ((string)jsonValue.message).Should().Contain("Không thể tải chi tiết đặt phòng");

            _mockPhieuDatPhongRepo.Verify(x => x.GetByIdAsync(TEST_BOOKING_ID), Times.Once);
        }

        /// <summary>
        /// Test: Null booking ID
        /// Verify: Xử lý gracefully
        /// </summary>
        [Test]
        public async Task Details_NullBookingId_ReturnsNotFoundError()
        {
            // Arrange
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(null))
                .ReturnsAsync((PhieuDatPhong)null);

            // Act
            var result = await _controller.Details(null);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(false);
        }

        /// <summary>
        /// Test: Empty string booking ID
        /// Verify: Xử lý như không tìm thấy
        /// </summary>
        [Test]
        public async Task Details_EmptyBookingId_ReturnsNotFoundError()
        {
            // Arrange
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(""))
                .ReturnsAsync((PhieuDatPhong)null);

            // Act
            var result = await _controller.Details("");

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(false);
        }

        #endregion

        #region Business Logic Tests

        /// <summary>
        /// Test: Booking không có phòng nào (edge case)
        /// Verify: Vẫn trả về success nhưng Rooms rỗng
        /// </summary>
        [Test]
        public async Task Details_BookingWithNoRooms_ReturnsEmptyRoomsList()
        {
            // Arrange
            var booking = CreateBookingWithNoRooms(TEST_HOST_ID);
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(TEST_BOOKING_ID))
                .ReturnsAsync(booking);

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(true);
            
            var rooms = ((IEnumerable<object>)jsonValue.data.Rooms).ToList();
            rooms.Should().BeEmpty();
        }

        /// <summary>
        /// Test: Customer info không có trong booking
        /// Verify: Các trường customer trả về null hoặc empty
        /// </summary>
        [Test]
        public async Task Details_BookingWithoutCustomerInfo_ReturnsNullCustomerFields()
        {
            // Arrange
            var booking = CreateBookingWithoutCustomer(TEST_HOST_ID);
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(TEST_BOOKING_ID))
                .ReturnsAsync(booking);

            _mockPhuThuRepo.Setup(x => x.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(true);
            
            // Customer fields should be null
            ((string)jsonValue.data.CustomerName).Should().BeNull();
            ((string)jsonValue.data.CustomerEmail).Should().BeNull();
        }

        /// <summary>
        /// Test: Phòng không có loại phòng (ID_Loai = null)
        /// Verify: Không có surcharges, không throw exception
        /// </summary>
        [Test]
        public async Task Details_RoomWithoutType_ReturnsEmptySurcharges()
        {
            // Arrange
            var booking = CreateBookingWithRoomWithoutType(TEST_HOST_ID);
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(TEST_BOOKING_ID))
                .ReturnsAsync(booking);

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            jsonValue.success.Should().Be(true);
            
            var rooms = ((IEnumerable<object>)jsonValue.data.Rooms).ToList();
            var firstRoom = (dynamic)rooms[0];
            var surcharges = ((IEnumerable<object>)firstRoom.Surcharges).ToList();
            
            surcharges.Should().BeEmpty();
        }

        /// <summary>
        /// Test: Check-in và check-out date format
        /// Verify: Dates được format đúng theo yyyy-MM-dd
        /// </summary>
        [Test]
        public async Task Details_ValidBooking_ReturnsCorrectDateFormat()
        {
            // Arrange
            var booking = CreateValidBooking(TEST_HOST_ID);
            _mockPhieuDatPhongRepo.Setup(x => x.GetByIdAsync(TEST_BOOKING_ID))
                .ReturnsAsync(booking);

            _mockPhuThuRepo.Setup(x => x.GetApDungPhuThuByLoaiPhongAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<ApDungPhuThu>());

            // Act
            var result = await _controller.Details(TEST_BOOKING_ID);

            // Assert
            var jsonValue = GetJsonResultValue(result);
            var rooms = ((IEnumerable<object>)jsonValue.data.Rooms).ToList();
            var firstRoom = (dynamic)rooms[0];
            
            string checkinDate = firstRoom.CheckinDate;
            string checkoutDate = firstRoom.CheckoutDate;
            
            // Verify format yyyy-MM-dd
            checkinDate.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}$");
            checkoutDate.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}$");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Setup user authentication với ClaimsPrincipal
        /// </summary>
        private void SetupAuthenticatedUser(string userId)
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        /// <summary>
        /// Tạo một PhieuDatPhong hợp lệ cho testing
        /// </summary>
        private PhieuDatPhong CreateValidBooking(string hostId)
        {
            return new PhieuDatPhong
            {
                Ma_PDPhong = TEST_BOOKING_ID,
                Ma_ND = TEST_CUSTOMER_ID,
                NgayLap = DateTime.Today,
                TrangThai = "Chờ xác nhận",
                NguoiDung = new ApplicationUser
                {
                    Id = TEST_CUSTOMER_ID,
                    FullName = "John Doe",
                    Email = "john@example.com",
                    PhoneNumber = "0123456789",
                    ProfilePicture = "/img/default-avatar.jpg"
                },
                ChiTietDatPhongs = new List<ChiTietDatPhong>
                {
                    new ChiTietDatPhong
                    {
                        Ma_PDPhong = TEST_BOOKING_ID,
                        Ma_Phong = "ROOM-001",
                        NgayDen = DateTime.Today.AddDays(1),
                        NgayDi = DateTime.Today.AddDays(3),
                        Phong = new Phong
                        {
                            Ma_Phong = "ROOM-001",
                            TenPhong = "Deluxe Room",
                            DonGia = 500000,
                            ID_Homestay = "HS-001",
                            ID_Loai = "LP-001",
                            Homestay = new Homestay
                            {
                                ID_Homestay = "HS-001",
                                Ten_Homestay = "Luxury Villa",
                                Ma_ND = hostId
                            }
                        },
                        PhieuSuDungDVs = new List<PhieuSuDungDV>()
                    }
                }
            };
        }

        /// <summary>
        /// Tạo booking với nhiều phòng
        /// </summary>
        private PhieuDatPhong CreateBookingWithMultipleRooms(string hostId)
        {
            var booking = CreateValidBooking(hostId);
            booking.ChiTietDatPhongs.Add(new ChiTietDatPhong
            {
                Ma_PDPhong = TEST_BOOKING_ID,
                Ma_Phong = "ROOM-002",
                NgayDen = DateTime.Today.AddDays(1),
                NgayDi = DateTime.Today.AddDays(3),
                Phong = new Phong
                {
                    Ma_Phong = "ROOM-002",
                    TenPhong = "Standard Room",
                    DonGia = 300000,
                    ID_Homestay = "HS-001",
                    ID_Loai = "LP-002",
                    Homestay = new Homestay
                    {
                        ID_Homestay = "HS-001",
                        Ten_Homestay = "Luxury Villa",
                        Ma_ND = hostId
                    }
                },
                PhieuSuDungDVs = new List<PhieuSuDungDV>()
            });
            return booking;
        }

        /// <summary>
        /// Tạo booking có dịch vụ
        /// </summary>
        private PhieuDatPhong CreateBookingWithServices(string hostId)
        {
            var booking = CreateValidBooking(hostId);
            var room = booking.ChiTietDatPhongs.First();
            room.PhieuSuDungDVs = new List<PhieuSuDungDV>
            {
                new PhieuSuDungDV
                {
                    Ma_PhieuSDDV = "PSDV-001",
                    ChiTietPhieuDVs = new List<ChiTietPhieuDV>
                    {
                        new ChiTietPhieuDV
                        {
                            Ma_DV = "DV-001",
                            SoLuong = 2,
                            DichVu = new DichVu
                            {
                                Ma_DV = "DV-001",
                                Ten_DV = "Bữa sáng",
                                DonGia = 50000
                            }
                        }
                    }
                }
            };
            return booking;
        }

        /// <summary>
        /// Tạo test surcharges
        /// </summary>
        private List<ApDungPhuThu> CreateTestSurcharges()
        {
            return new List<ApDungPhuThu>
            {
                new ApDungPhuThu
                {
                    Ma_PhieuPT = "PT-001",
                    NgayApDung = DateTime.Today.AddDays(1),
                    PhieuPhuThu = new PhieuPhuThu
                    {
                        Ma_PhieuPT = "PT-001",
                        NoiDung = "Phụ thu cuối tuần",
                        PhiPhuThu = 0.2m // 20%
                    }
                }
            };
        }

        /// <summary>
        /// Tạo booking không có phòng
        /// </summary>
        private PhieuDatPhong CreateBookingWithNoRooms(string hostId)
        {
            var booking = CreateValidBooking(hostId);
            booking.ChiTietDatPhongs = new List<ChiTietDatPhong>();
            return booking;
        }

        /// <summary>
        /// Tạo booking không có customer info
        /// </summary>
        private PhieuDatPhong CreateBookingWithoutCustomer(string hostId)
        {
            var booking = CreateValidBooking(hostId);
            booking.NguoiDung = null;
            return booking;
        }

        /// <summary>
        /// Tạo booking với phòng không có loại
        /// </summary>
        private PhieuDatPhong CreateBookingWithRoomWithoutType(string hostId)
        {
            var booking = CreateValidBooking(hostId);
            var room = booking.ChiTietDatPhongs.First();
            room.Phong.ID_Loai = null;
            return booking;
        }

        /// <summary>
        /// Helper để extract value từ JsonResult
        /// </summary>
        private dynamic GetJsonResultValue(JsonResult result)
        {
            return result.Value;
        }

        #endregion
    }
}
