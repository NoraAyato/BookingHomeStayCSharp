// Import thư viện NUnit để viết và chạy unit test
using NUnit.Framework;
// Import thư viện Moq để tạo mock objects (đối tượng giả lập)
using Moq;
// Import FluentAssertions để viết assertion một cách dễ đọc hơn
using FluentAssertions;
// Import các thư viện của ASP.NET Core MVC
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
// Import các model, repository và controller từ project chính
using DoAnCs.Models;
using DoAnCs.Repository;
using DoAnCs.Areas.Admin.Controllers;
// Import các namespace cơ bản của .NET
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace Demo.Tests.Controllers
{
    /// <summary>
    /// Test class cho PromotionController
    /// Kiểm tra các chức năng tạo và quản lý khuyến mãi
    /// </summary>
    [TestFixture]
    public class PromotionControllerTests
    {
        #region Private Fields

        // Mock objects để giả lập dependencies
        private Mock<IKhuyenMaiRepository> _mockKhuyenMaiRepo = null!;
        private Mock<ILogger<PromotionController>> _mockLogger = null!;

        // Instance của controller cần test
        private PromotionController _controller = null!;

        // Test user ID
        private string _testUserId = "USER001";

        #endregion

        #region Setup and Teardown

        /// <summary>
        /// Setup method chạy trước mỗi test case
        /// Khởi tạo tất cả mock objects và dependencies
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            // Khởi tạo tất cả mock objects
            _mockKhuyenMaiRepo = new Mock<IKhuyenMaiRepository>();
            _mockLogger = new Mock<ILogger<PromotionController>>();

            // Tạo instance của PromotionController với các mock dependencies
            _controller = new PromotionController(
                _mockKhuyenMaiRepo.Object,
                _mockLogger.Object
            );

            // Setup ControllerContext để mock User
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUserId),
                new Claim(ClaimTypes.Name, "testuser@test.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        /// <summary>
        /// TearDown method chạy sau mỗi test case
        /// Giải phóng resources
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }

        #endregion

        #region Test Cases - Create Promotion

        /// <summary>
        /// Test case: Tạo khuyến mãi thành công với chiết khấu phần trăm
        /// Scenario: Admin tạo khuyến mãi giảm 10% không có hình ảnh
        /// Expected: Khuyến mãi được tạo thành công với các thông tin đúng
        /// </summary>
        [Test]
        public async Task Create_WithPercentageDiscount_ShouldCreatePromotionSuccessfully()
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "SALE10",
                NoiDung = "Giảm 10% cho đơn hàng đầu tiên",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(30),
                ChietKhau = 10,
                LoaiChietKhau = "Percentage",
                SoDemToiThieu = 2,
                SoNgayDatTruoc = 1,
                ChiApDungChoKhachMoi = true,
                TrangThai = "Đang áp dụng",
                SoLuong = 100
            };

            // Setup mock repository
            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("SALE10"))
                .ReturnsAsync((KhuyenMai)null!); // Ma_KM chưa tồn tại

            _mockKhuyenMaiRepo.Setup(x => x.AddAsync(It.IsAny<KhuyenMai>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            var result = await _controller.Create(promotion, null);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<JsonResult>();

            var jsonResult = result as JsonResult;
            var jsonValue = JsonSerializer.Serialize(jsonResult!.Value);
            jsonValue.Should().Contain("\"success\":true");
            jsonValue.Should().Contain("Thêm khuyến mãi thành công");

            // Verify AddAsync được gọi với các giá trị đúng
            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.Is<KhuyenMai>(km =>
                km.Ma_KM == "SALE10" &&
                km.NoiDung == "Giảm 10% cho đơn hàng đầu tiên" &&
                km.ChietKhau == 10 &&
                km.LoaiChietKhau == "Percentage" &&
                km.SoDemToiThieu == 2 &&
                km.SoNgayDatTruoc == 1 &&
                km.ChiApDungChoKhachMoi == true &&
                km.TrangThai == "Đang áp dụng" &&
                km.SoLuong == 100 &&
                km.ApDungChoTatCaPhong == true &&
                km.NguoiTaoId == _testUserId &&
                km.HinhAnh == null
            )), Times.Once);
        }

        /// <summary>
        /// Test case: Tạo khuyến mãi thành công với chiết khấu cố định
        /// Scenario: Admin tạo khuyến mãi giảm 100,000 VND
        /// Expected: Khuyến mãi với Fixed discount được tạo thành công
        /// </summary>
        [Test]
        public async Task Create_WithFixedDiscount_ShouldCreatePromotionSuccessfully()
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "FIXED100K",
                NoiDung = "Giảm 100,000 VND",
                NgayBatDau = DateTime.Today.AddDays(1),
                HSD = DateTime.Today.AddDays(15),
                ChietKhau = 100000,
                LoaiChietKhau = "Fixed",
                SoDemToiThieu = 1,
                SoNgayDatTruoc = 0,
                ChiApDungChoKhachMoi = false,
                TrangThai = "Đang áp dụng",
                SoLuong = 50
            };

            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("FIXED100K"))
                .ReturnsAsync((KhuyenMai)null!);

            _mockKhuyenMaiRepo.Setup(x => x.AddAsync(It.IsAny<KhuyenMai>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            var result = await _controller.Create(promotion, null);

            // Assert
            var jsonResult = result as JsonResult;
            var jsonValue = JsonSerializer.Serialize(jsonResult!.Value);
            jsonValue.Should().Contain("\"success\":true");

            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.Is<KhuyenMai>(km =>
                km.Ma_KM == "FIXED100K" &&
                km.ChietKhau == 100000 &&
                km.LoaiChietKhau == "Fixed" &&
                km.ChiApDungChoKhachMoi == false
            )), Times.Once);
        }

        /// <summary>
        /// Test case: Tạo khuyến mãi thành công với hình ảnh
        /// Scenario: Admin tạo khuyến mãi và upload hình ảnh
        /// Expected: Khuyến mãi được tạo với đường dẫn hình ảnh được lưu
        /// </summary>
        [Test]
        public async Task Create_WithImage_ShouldCreatePromotionAndSaveImage()
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "NEWYEAR2025",
                NoiDung = "Khuyến mãi năm mới 2025",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(7),
                ChietKhau = 15,
                LoaiChietKhau = "Percentage",
                SoDemToiThieu = 3,
                SoNgayDatTruoc = 2,
                ChiApDungChoKhachMoi = false,
                TrangThai = "Đang áp dụng",
                SoLuong = 200
            };

            // Tạo mock IFormFile
            var mockFile = CreateMockFormFile("newyear.jpg", "image/jpeg");

            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("NEWYEAR2025"))
                .ReturnsAsync((KhuyenMai)null!);

            _mockKhuyenMaiRepo.Setup(x => x.AddAsync(It.IsAny<KhuyenMai>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            var result = await _controller.Create(promotion, mockFile.Object);

            // Assert
            var jsonResult = result as JsonResult;
            var jsonValue = JsonSerializer.Serialize(jsonResult!.Value);
            jsonValue.Should().Contain("\"success\":true");

            // Verify rằng khuyến mãi có đường dẫn hình ảnh
            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.Is<KhuyenMai>(km =>
                km.HinhAnh != null &&
                km.HinhAnh.StartsWith("/img/promotions/") &&
                km.HinhAnh.EndsWith(".jpg")
            )), Times.Once);
        }

        /// <summary>
        /// Test case: Tạo khuyến mãi với các định dạng hình ảnh khác nhau
        /// Scenario: Admin upload hình ảnh với các extension khác nhau
        /// Expected: Các định dạng được phép (jpg, png, gif) được chấp nhận
        /// </summary>
        [Test]
        [TestCase("promo.jpg", "image/jpeg", ".jpg")]
        [TestCase("promo.png", "image/png", ".png")]
        [TestCase("promo.gif", "image/gif", ".gif")]
        [TestCase("promo.jpeg", "image/jpeg", ".jpeg")]
        public async Task Create_WithDifferentImageFormats_ShouldAcceptValidFormats(
            string fileName, string contentType, string expectedExtension)
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "IMG-TEST",
                NoiDung = "Test Image Format",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(10),
                ChietKhau = 5,
                LoaiChietKhau = "Percentage",
                TrangThai = "Đang áp dụng",
                SoLuong = 50
            };

            var mockFile = CreateMockFormFile(fileName, contentType);

            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("IMG-TEST"))
                .ReturnsAsync((KhuyenMai)null!);

            _mockKhuyenMaiRepo.Setup(x => x.AddAsync(It.IsAny<KhuyenMai>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(promotion, mockFile.Object);

            // Assert
            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.Is<KhuyenMai>(km =>
                km.HinhAnh != null &&
                km.HinhAnh.EndsWith(expectedExtension)
            )), Times.Once);
        }

        /// <summary>
        /// Test case: Tạo khuyến mãi thất bại khi Ma_KM trống
        /// Scenario: Admin không nhập mã khuyến mãi
        /// Expected: Trả về lỗi "Mã khuyến mãi không được để trống"
        /// </summary>
        [Test]
        public async Task Create_WithEmptyMaKM_ShouldReturnError()
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "",
                NoiDung = "Test Promotion",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(10),
                ChietKhau = 10,
                LoaiChietKhau = "Percentage",
                TrangThai = "Đang áp dụng",
                SoLuong = 100
            };

            // Act
            var result = await _controller.Create(promotion, null);

            // Assert
            var jsonResult = result as JsonResult;
            var jsonValue = JsonSerializer.Serialize(jsonResult!.Value);
            jsonValue.Should().Contain("\"success\":false");
            jsonValue.Should().Contain("Mã khuyến mãi không được để trống");

            // Verify AddAsync không được gọi
            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.IsAny<KhuyenMai>()), Times.Never);
        }

        /// <summary>
        /// Test case: Tạo khuyến mãi thất bại khi Ma_KM vượt quá 20 ký tự
        /// Scenario: Admin nhập mã khuyến mãi quá dài
        /// Expected: Trả về lỗi "Mã khuyến mãi không được dài quá 20 ký tự"
        /// </summary>
        [Test]
        public async Task Create_WithTooLongMaKM_ShouldReturnError()
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "VERYLONGPROMOTIONCODE123456789", // > 20 chars
                NoiDung = "Test Promotion",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(10),
                ChietKhau = 10,
                LoaiChietKhau = "Percentage",
                TrangThai = "Đang áp dụng",
                SoLuong = 100
            };

            // Act
            var result = await _controller.Create(promotion, null);

            // Assert
            var jsonResult = result as JsonResult;
            var jsonValue = JsonSerializer.Serialize(jsonResult!.Value);
            jsonValue.Should().Contain("\"success\":false");
            jsonValue.Should().Contain("không được dài quá 20 ký tự");

            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.IsAny<KhuyenMai>()), Times.Never);
        }

        /// <summary>
        /// Test case: Tạo khuyến mãi thất bại khi Ma_KM có ký tự không hợp lệ
        /// Scenario: Admin nhập mã khuyến mãi có ký tự đặc biệt không được phép
        /// Expected: Trả về lỗi "Mã khuyến mãi chỉ được chứa chữ cái, số và dấu gạch ngang"
        /// </summary>
        [Test]
        [TestCase("SALE@10%")]
        [TestCase("PROMO#2025")]
        [TestCase("KM_NEWYEAR")]
        [TestCase("SALE 10")]
        public async Task Create_WithInvalidCharactersInMaKM_ShouldReturnError(string invalidMaKM)
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = invalidMaKM,
                NoiDung = "Test Promotion",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(10),
                ChietKhau = 10,
                LoaiChietKhau = "Percentage",
                TrangThai = "Đang áp dụng",
                SoLuong = 100
            };

            // Act
            var result = await _controller.Create(promotion, null);

            // Assert
            var jsonResult = result as JsonResult;
            var jsonValue = JsonSerializer.Serialize(jsonResult!.Value);
            jsonValue.Should().Contain("\"success\":false");
            jsonValue.Should().Contain("chỉ được chứa chữ cái, số và dấu gạch ngang");

            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.IsAny<KhuyenMai>()), Times.Never);
        }

        /// <summary>
        /// Test case: Tạo khuyến mãi thất bại khi Ma_KM đã tồn tại
        /// Scenario: Admin tạo khuyến mãi với mã đã có trong hệ thống
        /// Expected: Trả về lỗi "Mã khuyến mãi đã tồn tại"
        /// </summary>
        [Test]
        public async Task Create_WithExistingMaKM_ShouldReturnError()
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "EXISTING",
                NoiDung = "Test Promotion",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(10),
                ChietKhau = 10,
                LoaiChietKhau = "Percentage",
                TrangThai = "Đang áp dụng",
                SoLuong = 100
            };

            // Setup mock để trả về khuyến mãi đã tồn tại
            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("EXISTING"))
                .ReturnsAsync(new KhuyenMai { Ma_KM = "EXISTING" });

            // Act
            var result = await _controller.Create(promotion, null);

            // Assert
            var jsonResult = result as JsonResult;
            var jsonValue = JsonSerializer.Serialize(jsonResult!.Value);
            jsonValue.Should().Contain("\"success\":false");
            jsonValue.Should().Contain("Mã khuyến mãi đã tồn tại");

            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.IsAny<KhuyenMai>()), Times.Never);
        }

        /// <summary>
        /// Test case: Tạo khuyến mãi thất bại khi chiết khấu phần trăm không hợp lệ
        /// Scenario: Admin nhập chiết khấu phần trăm < 1 hoặc > 100
        /// Expected: Trả về lỗi về chiết khấu không hợp lệ
        /// </summary>
        [Test]
        [TestCase(0)]
        [TestCase(-5)]
        [TestCase(101)]
        [TestCase(150)]
        public async Task Create_WithInvalidPercentageDiscount_ShouldReturnError(decimal invalidDiscount)
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "INVALID-PCT",
                NoiDung = "Test Promotion",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(10),
                ChietKhau = invalidDiscount,
                LoaiChietKhau = "Percentage",
                TrangThai = "Đang áp dụng",
                SoLuong = 100
            };

            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("INVALID-PCT"))
                .ReturnsAsync((KhuyenMai)null!);

            // Act
            var result = await _controller.Create(promotion, null);

            // Assert
            var jsonResult = result as JsonResult;
            var jsonValue = JsonSerializer.Serialize(jsonResult!.Value);
            jsonValue.Should().Contain("\"success\":false");
            jsonValue.Should().Contain("Chiết khấu phần trăm phải từ 1% đến 100%");

            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.IsAny<KhuyenMai>()), Times.Never);
        }

        /// <summary>
        /// Test case: Tạo khuyến mãi thất bại khi chiết khấu cố định không hợp lệ
        /// Scenario: Admin nhập chiết khấu cố định <= 0
        /// Expected: Trả về lỗi "Chiết khấu cố định phải lớn hơn 0"
        /// </summary>
        [Test]
        [TestCase(0)]
        [TestCase(-1000)]
        public async Task Create_WithInvalidFixedDiscount_ShouldReturnError(decimal invalidDiscount)
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "INVALID-FIXED",
                NoiDung = "Test Promotion",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(10),
                ChietKhau = invalidDiscount,
                LoaiChietKhau = "Fixed",
                TrangThai = "Đang áp dụng",
                SoLuong = 100
            };

            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("INVALID-FIXED"))
                .ReturnsAsync((KhuyenMai)null!);

            // Act
            var result = await _controller.Create(promotion, null);

            // Assert
            var jsonResult = result as JsonResult;
            var jsonValue = JsonSerializer.Serialize(jsonResult!.Value);
            jsonValue.Should().Contain("\"success\":false");
            jsonValue.Should().Contain("Chiết khấu cố định phải lớn hơn 0");

            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.IsAny<KhuyenMai>()), Times.Never);
        }

        /// <summary>
        /// Test case: Tạo khuyến mãi thất bại khi số lượng âm
        /// Scenario: Admin nhập số lượng khuyến mãi < 0
        /// Expected: Trả về lỗi "Số lượng phải lớn hơn hoặc bằng 0"
        /// </summary>
        [Test]
        public async Task Create_WithNegativeQuantity_ShouldReturnError()
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "NEG-QTY",
                NoiDung = "Test Promotion",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(10),
                ChietKhau = 10,
                LoaiChietKhau = "Percentage",
                TrangThai = "Đang áp dụng",
                SoLuong = -10
            };

            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("NEG-QTY"))
                .ReturnsAsync((KhuyenMai)null!);

            // Act
            var result = await _controller.Create(promotion, null);

            // Assert
            var jsonResult = result as JsonResult;
            var jsonValue = JsonSerializer.Serialize(jsonResult!.Value);
            jsonValue.Should().Contain("\"success\":false");
            jsonValue.Should().Contain("Số lượng phải lớn hơn hoặc bằng 0");

            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.IsAny<KhuyenMai>()), Times.Never);
        }

        /// <summary>
        /// Test case: Tạo khuyến mãi thất bại khi ngày bắt đầu < ngày hiện tại
        /// Scenario: Admin tạo khuyến mãi với ngày bắt đầu trong quá khứ
        /// Expected: Trả về lỗi "Ngày bắt đầu không được nhỏ hơn ngày hiện tại"
        /// </summary>
        [Test]
        public async Task Create_WithPastStartDate_ShouldReturnError()
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "PAST-DATE",
                NoiDung = "Test Promotion",
                NgayBatDau = DateTime.Today.AddDays(-1), // Past date
                HSD = DateTime.Today.AddDays(10),
                ChietKhau = 10,
                LoaiChietKhau = "Percentage",
                TrangThai = "Đang áp dụng",
                SoLuong = 100
            };

            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("PAST-DATE"))
                .ReturnsAsync((KhuyenMai)null!);

            // Act
            var result = await _controller.Create(promotion, null);

            // Assert
            var jsonResult = result as JsonResult;
            var jsonValue = JsonSerializer.Serialize(jsonResult!.Value);
            jsonValue.Should().Contain("\"success\":false");
            jsonValue.Should().Contain("Ngày bắt đầu không được nhỏ hơn ngày hiện tại");

            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.IsAny<KhuyenMai>()), Times.Never);
        }

        /// <summary>
        /// Test case: Tạo khuyến mãi thất bại khi HSD < NgayBatDau
        /// Scenario: Admin tạo khuyến mãi với hạn sử dụng trước ngày bắt đầu
        /// Expected: Trả về lỗi "HSD không được nhỏ hơn ngày bắt đầu"
        /// </summary>
        [Test]
        public async Task Create_WithHSDBeforeStartDate_ShouldReturnError()
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "INVALID-HSD",
                NoiDung = "Test Promotion",
                NgayBatDau = DateTime.Today.AddDays(10),
                HSD = DateTime.Today.AddDays(5), // HSD < NgayBatDau
                ChietKhau = 10,
                LoaiChietKhau = "Percentage",
                TrangThai = "Đang áp dụng",
                SoLuong = 100
            };

            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("INVALID-HSD"))
                .ReturnsAsync((KhuyenMai)null!);

            // Act
            var result = await _controller.Create(promotion, null);

            // Assert
            var jsonResult = result as JsonResult;
            var jsonValue = JsonSerializer.Serialize(jsonResult!.Value);
            jsonValue.Should().Contain("\"success\":false");
            jsonValue.Should().Contain("HSD không được nhỏ hơn ngày bắt đầu");

            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.IsAny<KhuyenMai>()), Times.Never);
        }

        /// <summary>
        /// Test case: Tạo khuyến mãi thất bại khi upload file không phải hình ảnh
        /// Scenario: Admin upload file .pdf, .txt hoặc các file không phải ảnh
        /// Expected: Trả về lỗi về định dạng file không được hỗ trợ
        /// </summary>
        [Test]
        [TestCase("document.pdf", "application/pdf")]
        [TestCase("file.txt", "text/plain")]
        [TestCase("data.xlsx", "application/vnd.ms-excel")]
        public async Task Create_WithInvalidFileFormat_ShouldReturnError(string fileName, string contentType)
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "INVALID-FILE",
                NoiDung = "Test Promotion",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(10),
                ChietKhau = 10,
                LoaiChietKhau = "Percentage",
                TrangThai = "Đang áp dụng",
                SoLuong = 100
            };

            var mockFile = CreateMockFormFile(fileName, contentType);

            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("INVALID-FILE"))
                .ReturnsAsync((KhuyenMai)null!);

            // Act
            var result = await _controller.Create(promotion, mockFile.Object);

            // Assert
            var jsonResult = result as JsonResult;
            var jsonValue = JsonSerializer.Serialize(jsonResult!.Value);
            jsonValue.Should().Contain("\"success\":false");
            jsonValue.Should().Contain("Chỉ hỗ trợ các định dạng hình ảnh");

            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.IsAny<KhuyenMai>()), Times.Never);
        }

        /// <summary>
        /// Test case: Kiểm tra khuyến mãi được gán NguoiTaoId đúng
        /// Scenario: Admin tạo khuyến mãi
        /// Expected: NguoiTaoId được gán là ID của user đang đăng nhập
        /// </summary>
        [Test]
        public async Task Create_ShouldAssignCorrectNguoiTaoId()
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "CHECK-USER",
                NoiDung = "Check User ID",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(10),
                ChietKhau = 10,
                LoaiChietKhau = "Percentage",
                TrangThai = "Đang áp dụng",
                SoLuong = 100
            };

            string capturedNguoiTaoId = null!;

            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("CHECK-USER"))
                .ReturnsAsync((KhuyenMai)null!);

            _mockKhuyenMaiRepo.Setup(x => x.AddAsync(It.IsAny<KhuyenMai>()))
                .Callback<KhuyenMai>(km => capturedNguoiTaoId = km.NguoiTaoId)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Create(promotion, null);

            // Assert
            capturedNguoiTaoId.Should().NotBeNull();
            capturedNguoiTaoId.Should().Be(_testUserId);
        }

        /// <summary>
        /// Test case: Kiểm tra NgayTao được set tự động
        /// Scenario: Admin tạo khuyến mãi
        /// Expected: NgayTao được set là thời điểm hiện tại
        /// </summary>
        [Test]
        public async Task Create_ShouldSetNgayTaoAutomatically()
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "CHECK-DATE",
                NoiDung = "Check NgayTao",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(10),
                ChietKhau = 10,
                LoaiChietKhau = "Percentage",
                TrangThai = "Đang áp dụng",
                SoLuong = 100
            };

            DateTime capturedNgayTao = DateTime.MinValue;
            var beforeCreate = DateTime.Now;

            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("CHECK-DATE"))
                .ReturnsAsync((KhuyenMai)null!);

            _mockKhuyenMaiRepo.Setup(x => x.AddAsync(It.IsAny<KhuyenMai>()))
                .Callback<KhuyenMai>(km => capturedNgayTao = km.NgayTao)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Create(promotion, null);
            var afterCreate = DateTime.Now;

            // Assert
            capturedNgayTao.Should().BeOnOrAfter(beforeCreate);
            capturedNgayTao.Should().BeOnOrBefore(afterCreate);
        }

        /// <summary>
        /// Test case: Kiểm tra ApDungChoTatCaPhong luôn được set true
        /// Scenario: Admin tạo khuyến mãi
        /// Expected: ApDungChoTatCaPhong = true và KhuyenMaiPhongs = null
        /// </summary>
        [Test]
        public async Task Create_ShouldSetApDungChoTatCaPhongToTrue()
        {
            // Arrange
            var promotion = new KhuyenMai
            {
                Ma_KM = "CHECK-ROOMS",
                NoiDung = "Check All Rooms Flag",
                NgayBatDau = DateTime.Today,
                HSD = DateTime.Today.AddDays(10),
                ChietKhau = 10,
                LoaiChietKhau = "Percentage",
                TrangThai = "Đang áp dụng",
                SoLuong = 100
            };

            _mockKhuyenMaiRepo.Setup(x => x.GetByIdAsync("CHECK-ROOMS"))
                .ReturnsAsync((KhuyenMai)null!);

            _mockKhuyenMaiRepo.Setup(x => x.AddAsync(It.IsAny<KhuyenMai>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _controller.Create(promotion, null);

            // Assert
            _mockKhuyenMaiRepo.Verify(x => x.AddAsync(It.Is<KhuyenMai>(km =>
                km.ApDungChoTatCaPhong == true &&
                km.KhuyenMaiPhongs == null
            )), Times.Once);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Tạo mock IFormFile để test upload file
        /// </summary>
        private Mock<IFormFile> CreateMockFormFile(string fileName, string contentType)
        {
            var mockFile = new Mock<IFormFile>();
            var content = "fake image content";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.ContentType).Returns(contentType);
            mockFile.Setup(f => f.Length).Returns(ms.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream stream, CancellationToken token) =>
                {
                    ms.CopyToAsync(stream);
                    return Task.CompletedTask;
                });

            return mockFile;
        }

        #endregion
    }
}
