using NUnit.Framework;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using DoAnCs.Models;
using DoAnCs.Repository;
using DoAnCs.Areas.Admin.Controllers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DoAnCs.Tests.Controllers
{
    /// <summary>
    /// Unit tests cho chức năng tạo mới Homestay trong HomestayController.
    /// Tập trung test business logic, không test database hay file system.
    /// </summary>
    [TestFixture]
    public class HomestayControllerTests
    {
        private Mock<IHomestayRepository> _mockHomestayRepo;
        private Mock<IChinhSachRepository> _mockChinhSachRepo;
        private HomestayController _controller;

        [SetUp]
        public void SetUp()
        {
            _mockHomestayRepo = new Mock<IHomestayRepository>();
            _mockChinhSachRepo = new Mock<IChinhSachRepository>();

            _controller = new HomestayController(
                _mockHomestayRepo.Object,
                _mockChinhSachRepo.Object,
                null!  // Context không cần thiết cho unit test
            );
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }

        #region Happy Path Tests

        /// <summary>
        /// Test: Tạo homestay thành công không có hình ảnh
        /// Verify: Homestay và ChinhSach được tạo với ID đúng format và giá trị mặc định
        /// </summary>
        [Test]
        public async Task Create_ValidModelWithoutImage_ReturnsSuccessAndCallsRepositories()
        {
            // Arrange
            var homestay = CreateValidHomestay();

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(homestay, null);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = (JsonResult)result;

            var resultValue = jsonResult.Value;
            resultValue.Should().NotBeNull();
            resultValue.GetType().GetProperty("success")?.GetValue(resultValue).Should().Be(true);

            // Verify homestay được lưu với ID được generate
            _mockHomestayRepo.Verify(x => x.AddAsync(It.Is<Homestay>(h =>
                h.ID_Homestay.StartsWith("HS-") &&
                h.Ten_Homestay == homestay.Ten_Homestay &&
                h.Ma_KV == homestay.Ma_KV &&
                h.Ma_ND == homestay.Ma_ND &&
                h.HinhAnh == null
            )), Times.Once);

            // Verify ChinhSach được tạo với giá trị mặc định
            _mockChinhSachRepo.Verify(x => x.AddAsync(It.Is<ChinhSach>(cs =>
                cs.Ma_CS.StartsWith("CS-") &&
                cs.ID_Homestay.StartsWith("HS-") &&
                cs.NhanPhong == "14:00" &&
                cs.TraPhong == "12:00" &&
                cs.HuyPhong == "Hủy trước 48 giờ: hoàn tiền 100%. Sau đó: không hoàn tiền." &&
                cs.BuaAn == "Không bao gồm bữa ăn."
            )), Times.Once);
        }

        /// <summary>
        /// Test: Tạo homestay thành công có file hình ảnh
        /// Verify: File được lưu và đường dẫn được set vào model
        /// </summary>
        [Test]
        public async Task Create_ValidModelWithImage_SavesImageAndReturnsSuccess()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            var mockFile = CreateMockImageFile("test.jpg", "image/jpeg");

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(homestay, mockFile.Object);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = (JsonResult)result;
            jsonResult.Value.GetType().GetProperty("success")?.GetValue(jsonResult.Value).Should().Be(true);

            // Verify file path được set
            _mockHomestayRepo.Verify(x => x.AddAsync(It.Is<Homestay>(h =>
                h.HinhAnh != null &&
                h.HinhAnh.StartsWith("/img/Homestays/") &&
                h.HinhAnh.EndsWith(".jpg")
            )), Times.Once);

            _mockChinhSachRepo.Verify(x => x.AddAsync(It.IsAny<ChinhSach>()), Times.Once);
        }

        /// <summary>
        /// Test: File upload với các extension khác nhau được xử lý đúng
        /// TestCase: PNG, JPEG, GIF - verify extension được giữ nguyên
        /// </summary>
        [Test]
        [TestCase("image.png", "image/png", ".png")]
        [TestCase("image.jpeg", "image/jpeg", ".jpeg")]
        [TestCase("image.gif", "image/gif", ".gif")]
        public async Task Create_WithDifferentImageExtensions_PreservesCorrectExtension(
            string fileName, string contentType, string expectedExtension)
        {
            // Arrange
            var homestay = CreateValidHomestay();
            var mockFile = CreateMockImageFile(fileName, contentType);

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(homestay, mockFile.Object);

            // Assert
            _mockHomestayRepo.Verify(x => x.AddAsync(It.Is<Homestay>(h =>
                h.HinhAnh != null && h.HinhAnh.EndsWith(expectedExtension)
            )), Times.Once);
        }

        #endregion

        #region Edge Cases Tests

        /// <summary>
        /// Test: File empty (Length = 0) không được lưu
        /// Verify: HinhAnh = null, không tạo file
        /// </summary>
        [Test]
        public async Task Create_WithEmptyFile_DoesNotSaveImage()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);
            mockFile.Setup(f => f.FileName).Returns("empty.jpg");

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Create(homestay, mockFile.Object);

            // Assert
            _mockHomestayRepo.Verify(x => x.AddAsync(It.Is<Homestay>(h =>
                h.HinhAnh == null
            )), Times.Once);
        }

        /// <summary>
        /// Test: Null file không gây lỗi
        /// Verify: Homestay được tạo thành công mà không có hình ảnh
        /// </summary>
        [Test]
        public async Task Create_WithNullFile_SucceedsWithoutImage()
        {
            // Arrange
            var homestay = CreateValidHomestay();

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(homestay, null);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = (JsonResult)result;
            jsonResult.Value.GetType().GetProperty("success")?.GetValue(jsonResult.Value).Should().Be(true);

            _mockHomestayRepo.Verify(x => x.AddAsync(It.Is<Homestay>(h =>
                h.HinhAnh == null
            )), Times.Once);
        }

        #endregion

        #region Error Handling Tests

        /// <summary>
        /// Test: Repository throw exception - trả về failure
        /// Verify: ChinhSach không được tạo khi Homestay fail
        /// </summary>
        [Test]
        public async Task Create_WhenHomestayRepositoryFails_ReturnsFailureResponse()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            var expectedException = new Exception("Database connection failed");

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .ThrowsAsync(expectedException);

            // Act
            var result = await _controller.Create(homestay, null);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = (JsonResult)result;

            var success = jsonResult.Value.GetType().GetProperty("success")?.GetValue(jsonResult.Value);
            var message = jsonResult.Value.GetType().GetProperty("message")?.GetValue(jsonResult.Value);

            success.Should().Be(false);
            message.Should().Be("Database connection failed");

            // ChinhSach không được tạo khi Homestay fail
            _mockChinhSachRepo.Verify(x => x.AddAsync(It.IsAny<ChinhSach>()), Times.Never);
        }

        /// <summary>
        /// Test: ChinhSach repository throw exception - trả về failure
        /// Verify: Homestay đã được gọi trước khi ChinhSach fail
        /// </summary>
        [Test]
        public async Task Create_WhenChinhSachRepositoryFails_ReturnsFailureResponse()
        {
            // Arrange
            var homestay = CreateValidHomestay();

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .ThrowsAsync(new Exception("Failed to create policy"));

            // Act
            var result = await _controller.Create(homestay, null);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = (JsonResult)result;
            jsonResult.Value.GetType().GetProperty("success")?.GetValue(jsonResult.Value).Should().Be(false);

            // Homestay đã được gọi trước khi ChinhSach fail
            _mockHomestayRepo.Verify(x => x.AddAsync(It.IsAny<Homestay>()), Times.Once);
        }

        /// <summary>
        /// Test: File upload throw IOException - trả về failure
        /// Verify: Repository không được gọi khi file upload fail
        /// </summary>
        [Test]
        public async Task Create_WhenFileUploadFails_ReturnsFailureResponse()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(1000);
            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("Disk full"));

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(homestay, mockFile.Object);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = (JsonResult)result;
            jsonResult.Value.GetType().GetProperty("success")?.GetValue(jsonResult.Value).Should().Be(false);

            // Repository không được gọi khi file upload fail
            _mockHomestayRepo.Verify(x => x.AddAsync(It.IsAny<Homestay>()), Times.Never);
        }

        /// <summary>
        /// Test: Null model throw exception - trả về failure
        /// Verify: Không có repository nào được gọi
        /// </summary>
        [Test]
        public async Task Create_WithNullModel_ReturnsFailureResponse()
        {
            // Arrange
            Homestay homestay = null!;

            // Act
            var result = await _controller.Create(homestay, null);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = (JsonResult)result;
            jsonResult.Value.GetType().GetProperty("success")?.GetValue(jsonResult.Value).Should().Be(false);

            _mockHomestayRepo.Verify(x => x.AddAsync(It.IsAny<Homestay>()), Times.Never);
            _mockChinhSachRepo.Verify(x => x.AddAsync(It.IsAny<ChinhSach>()), Times.Never);
        }

        #endregion

        #region Business Logic Tests

        /// <summary>
        /// Test: ID_Homestay được generate với format đúng (HS-GUID)
        /// Verify: Format matches regex pattern
        /// </summary>
        [Test]
        public async Task Create_GeneratesCorrectHomestayIdFormat()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            string capturedId = null!;

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Callback<Homestay>(h => capturedId = h.ID_Homestay)
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Create(homestay, null);

            // Assert
            capturedId.Should().NotBeNullOrEmpty();
            capturedId.Should().StartWith("HS-");
            capturedId.Should().MatchRegex(@"^HS-[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$");
        }

        /// <summary>
        /// Test: ChinhSach được tạo với ID_Homestay trùng với Homestay
        /// Verify: Foreign key relationship chính xác
        /// </summary>
        [Test]
        public async Task Create_CreatesChinhSachWithMatchingHomestayId()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            string homestayId = null!;
            string chinhSachHomestayId = null!;

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Callback<Homestay>(h => homestayId = h.ID_Homestay)
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Callback<ChinhSach>(cs => chinhSachHomestayId = cs.ID_Homestay)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Create(homestay, null);

            // Assert
            homestayId.Should().NotBeNullOrEmpty();
            chinhSachHomestayId.Should().Be(homestayId);
        }

        /// <summary>
        /// Test: ChinhSach được tạo với các giá trị mặc định đúng
        /// Verify: NhanPhong, TraPhong, HuyPhong, BuaAn có giá trị default
        /// </summary>
        [Test]
        public async Task Create_CreatesChinhSachWithCorrectDefaultValues()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            ChinhSach capturedChinhSach = null!;

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Callback<ChinhSach>(cs => capturedChinhSach = cs)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Create(homestay, null);

            // Assert
            capturedChinhSach.Should().NotBeNull();
            capturedChinhSach.Ma_CS.Should().StartWith("CS-");
            capturedChinhSach.NhanPhong.Should().Be("14:00");
            capturedChinhSach.TraPhong.Should().Be("12:00");
            capturedChinhSach.HuyPhong.Should().Be("Hủy trước 48 giờ: hoàn tiền 100%. Sau đó: không hoàn tiền.");
            capturedChinhSach.BuaAn.Should().Be("Không bao gồm bữa ăn.");
        }

        /// <summary>
        /// Test: Tất cả thuộc tính của model được truyền chính xác vào repository
        /// Verify: Không có thuộc tính nào bị thay đổi hoặc mất
        /// </summary>
        [Test]
        public async Task Create_AllModelPropertiesArePreserved()
        {
            // Arrange
            var homestay = new Homestay
            {
                Ten_Homestay = "Luxury Villa Dalat",
                Ma_KV = "KV999",
                Ma_ND = "ND999",
                DiaChi = "456 Mountain View",
                PricePerNight = 1500000,
                TrangThai = "Hoạt động",
                Hang = 4.8m
            };

            Homestay capturedHomestay = null!;

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Callback<Homestay>(h => capturedHomestay = h)
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Create(homestay, null);

            // Assert
            capturedHomestay.Should().NotBeNull();
            capturedHomestay.Ten_Homestay.Should().Be("Luxury Villa Dalat");
            capturedHomestay.Ma_KV.Should().Be("KV999");
            capturedHomestay.Ma_ND.Should().Be("ND999");
            capturedHomestay.DiaChi.Should().Be("456 Mountain View");
            capturedHomestay.PricePerNight.Should().Be(1500000);
            capturedHomestay.TrangThai.Should().Be("Hoạt động");
            capturedHomestay.Hang.Should().Be(4.8m);
        }

        #endregion

        #region Validation Tests

        /// <summary>
        /// Test: Model với PricePerNight = 0 vẫn được xử lý (không có validation)
        /// Note: Controller hiện không validate giá trị
        /// </summary>
        [Test]
        public async Task Create_WithZeroPrice_StillSucceeds()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            homestay.PricePerNight = 0;

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(homestay, null);

            // Assert - Controller hiện không validate, nên vẫn success
            result.Should().BeOfType<JsonResult>();
            var jsonResult = (JsonResult)result;
            jsonResult.Value.GetType().GetProperty("success")?.GetValue(jsonResult.Value).Should().Be(true);

            _mockHomestayRepo.Verify(x => x.AddAsync(It.Is<Homestay>(h => 
                h.PricePerNight == 0
            )), Times.Once);
        }

        /// <summary>
        /// Test: Model với Hang = 0 vẫn được xử lý
        /// Note: Không có validation cho rating range
        /// </summary>
        [Test]
        public async Task Create_WithZeroHang_StillSucceeds()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            homestay.Hang = 0;

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(homestay, null);

            // Assert
            result.Should().BeOfType<JsonResult>();
            _mockHomestayRepo.Verify(x => x.AddAsync(It.Is<Homestay>(h => 
                h.Hang == 0
            )), Times.Once);
        }

        /// <summary>
        /// Test: Model với Hang > 5 vẫn được xử lý (không có validation)
        /// Note: Nên thêm validation để Hang trong khoảng 0-5
        /// </summary>
        [Test]
        public async Task Create_WithHangGreaterThanFive_StillSucceeds()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            homestay.Hang = 6.5m;

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(homestay, null);

            // Assert
            result.Should().BeOfType<JsonResult>();
            _mockHomestayRepo.Verify(x => x.AddAsync(It.Is<Homestay>(h => 
                h.Hang == 6.5m
            )), Times.Once);
        }

        /// <summary>
        /// Test: Model với empty strings vẫn được xử lý
        /// Note: Không có validation cho required fields
        /// </summary>
        [Test]
        public async Task Create_WithEmptyStrings_StillSucceeds()
        {
            // Arrange
            var homestay = new Homestay
            {
                Ten_Homestay = "",
                Ma_KV = "",
                Ma_ND = "",
                DiaChi = "",
                PricePerNight = 100000,
                TrangThai = "",
                Hang = 3.0m
            };

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(homestay, null);

            // Assert - Không có validation nên vẫn success
            result.Should().BeOfType<JsonResult>();
            _mockHomestayRepo.Verify(x => x.AddAsync(It.IsAny<Homestay>()), Times.Once);
        }

        #endregion

        #region File Type Tests

        /// <summary>
        /// Test: File PDF/TXT/EXE không có validation - vẫn được upload
        /// TestCase: Nhiều loại file khác nhau
        /// Note: Nên thêm validation để chỉ chấp nhận image files
        /// </summary>
        [Test]
        [TestCase("document.pdf", "application/pdf", ".pdf")]
        [TestCase("text.txt", "text/plain", ".txt")]
        [TestCase("script.exe", "application/x-msdownload", ".exe")]
        public async Task Create_WithNonImageFileTypes_StillUploadsFile(
            string fileName, string contentType, string extension)
        {
            // Arrange
            var homestay = CreateValidHomestay();
            var mockFile = CreateMockImageFile(fileName, contentType);

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(homestay, mockFile.Object);

            // Assert - Controller không validate file type
            result.Should().BeOfType<JsonResult>();
            _mockHomestayRepo.Verify(x => x.AddAsync(It.Is<Homestay>(h =>
                h.HinhAnh != null && h.HinhAnh.EndsWith(extension)
            )), Times.Once);
        }

        /// <summary>
        /// Test: File không có extension - vẫn được xử lý
        /// Verify: File được lưu mà không có extension trong path
        /// </summary>
        [Test]
        public async Task Create_WithNoFileExtension_CreatesFileWithoutExtension()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            var mockFile = CreateMockImageFile("imagewithoutextension", "image/jpeg");

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(homestay, mockFile.Object);

            // Assert
            result.Should().BeOfType<JsonResult>();
            _mockHomestayRepo.Verify(x => x.AddAsync(It.Is<Homestay>(h =>
                h.HinhAnh != null && 
                h.HinhAnh.StartsWith("/img/Homestays/") &&
                !h.HinhAnh.Contains(".")  // No extension in saved path
            )), Times.Once);
        }

        /// <summary>
        /// Test: File với uppercase extension được xử lý đúng
        /// Verify: Extension case được preserve
        /// </summary>
        [Test]
        public async Task Create_WithUppercaseExtension_PreservesCase()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            var mockFile = CreateMockImageFile("IMAGE.JPG", "image/jpeg");

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(homestay, mockFile.Object);

            // Assert
            result.Should().BeOfType<JsonResult>();
            _mockHomestayRepo.Verify(x => x.AddAsync(It.Is<Homestay>(h =>
                h.HinhAnh != null && h.HinhAnh.EndsWith(".JPG")
            )), Times.Once);
        }

        /// <summary>
        /// Test: File với tên đặc biệt (có khoảng trắng, ký tự đặc biệt)
        /// Verify: Guid được dùng nên không bị ảnh hưởng bởi tên file gốc
        /// </summary>
        [Test]
        public async Task Create_WithSpecialCharactersInFileName_HandlesCorrectly()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            var mockFile = CreateMockImageFile("my image (1) test.jpg", "image/jpeg");

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(homestay, mockFile.Object);

            // Assert - Guid được dùng nên không bị ảnh hưởng
            result.Should().BeOfType<JsonResult>();
            _mockHomestayRepo.Verify(x => x.AddAsync(It.Is<Homestay>(h =>
                h.HinhAnh != null && h.HinhAnh.EndsWith(".jpg")
            )), Times.Once);
        }

        #endregion

        #region Concurrency and Uniqueness Tests

        /// <summary>
        /// Test: Mỗi lần tạo homestay sinh ID khác nhau
        /// Verify: GUID uniqueness
        /// </summary>
        [Test]
        public async Task Create_MultipleCalls_GeneratesUniqueIds()
        {
            // Arrange
            var homestay1 = CreateValidHomestay();
            var homestay2 = CreateValidHomestay();
            
            string capturedId1 = null!;
            string capturedId2 = null!;

            _mockHomestayRepo.SetupSequence(x => x.AddAsync(It.IsAny<Homestay>()))
                .Callback<Homestay>(h => capturedId1 = h.ID_Homestay)
                .Returns(Task.CompletedTask)
                .Callback<Homestay>(h => capturedId2 = h.ID_Homestay)
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Create(homestay1, null);
            await _controller.Create(homestay2, null);

            // Assert
            capturedId1.Should().NotBeNullOrEmpty();
            capturedId2.Should().NotBeNullOrEmpty();
            capturedId1.Should().NotBe(capturedId2);
        }

        /// <summary>
        /// Test: Mỗi lần tạo ChinhSach sinh Ma_CS khác nhau
        /// Verify: Cả hai Ma_CS đều bắt đầu bằng "CS-" và khác nhau
        /// </summary>
        [Test]
        public async Task Create_MultipleCalls_GeneratesUniqueChinhSachIds()
        {
            // Arrange
            var homestay1 = CreateValidHomestay();
            var homestay2 = CreateValidHomestay();
            
            string capturedMaCS1 = null!;
            string capturedMaCS2 = null!;

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.SetupSequence(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Callback<ChinhSach>(cs => capturedMaCS1 = cs.Ma_CS)
                .Returns(Task.CompletedTask)
                .Callback<ChinhSach>(cs => capturedMaCS2 = cs.Ma_CS)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Create(homestay1, null);
            await _controller.Create(homestay2, null);

            // Assert
            capturedMaCS1.Should().NotBe(capturedMaCS2);
            capturedMaCS1.Should().StartWith("CS-");
            capturedMaCS2.Should().StartWith("CS-");
        }

        #endregion

        #region Integration Behavior Tests

        /// <summary>
        /// Test: Homestay được thêm trước ChinhSach (thứ tự quan trọng)
        /// Verify: Call order phải đúng vì ChinhSach có foreign key đến Homestay
        /// </summary>
        [Test]
        public async Task Create_AddsHomestayBeforeChinhSach()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            var callOrder = new List<string>();

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Callback(() => callOrder.Add("Homestay"))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Callback(() => callOrder.Add("ChinhSach"))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Create(homestay, null);

            // Assert
            callOrder.Should().HaveCount(2);
            callOrder[0].Should().Be("Homestay");
            callOrder[1].Should().Be("ChinhSach");
        }

        /// <summary>
        /// Test: Khi Homestay fail, ChinhSach không được tạo
        /// Verify: Transaction-like behavior
        /// </summary>
        [Test]
        public async Task Create_WhenHomestayFails_ChinhSachIsNotCreated()
        {
            // Arrange
            var homestay = CreateValidHomestay();

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .ThrowsAsync(new Exception("Homestay creation failed"));

            // Act
            await _controller.Create(homestay, null);

            // Assert
            _mockHomestayRepo.Verify(x => x.AddAsync(It.IsAny<Homestay>()), Times.Once);
            _mockChinhSachRepo.Verify(x => x.AddAsync(It.IsAny<ChinhSach>()), Times.Never);
        }

        /// <summary>
        /// Test: File upload xảy ra trước khi lưu vào database
        /// Verify: Thứ tự: FileUpload -> Homestay -> ChinhSach
        /// </summary>
        [Test]
        public async Task Create_UploadsFileBeforeDatabaseSave()
        {
            // Arrange
            var homestay = CreateValidHomestay();
            var mockFile = CreateMockImageFile("test.jpg", "image/jpeg");
            var callOrder = new List<string>();

            mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Callback(() => callOrder.Add("FileUpload"))
                .Returns((Stream stream, CancellationToken token) =>
                {
                    var ms = new MemoryStream();
                    return ms.CopyToAsync(stream, token);
                });

            _mockHomestayRepo.Setup(x => x.AddAsync(It.IsAny<Homestay>()))
                .Callback(() => callOrder.Add("Homestay"))
                .Returns(Task.CompletedTask);

            _mockChinhSachRepo.Setup(x => x.AddAsync(It.IsAny<ChinhSach>()))
                .Callback(() => callOrder.Add("ChinhSach"))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Create(homestay, mockFile.Object);

            // Assert
            callOrder.Should().HaveCount(3);
            callOrder[0].Should().Be("FileUpload");
            callOrder[1].Should().Be("Homestay");
            callOrder[2].Should().Be("ChinhSach");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Tạo một Homestay hợp lệ cho testing
        /// </summary>
        private Homestay CreateValidHomestay()
        {
            return new Homestay
            {
                Ten_Homestay = "Test Homestay",
                Ma_KV = "KV001",
                Ma_ND = "ND001",
                DiaChi = "123 Test Street",
                PricePerNight = 500000,
                TrangThai = "Hoạt động",
                Hang = 4.5m
            };
        }

        /// <summary>
        /// Tạo mock IFormFile cho testing file upload
        /// </summary>
        private Mock<IFormFile> CreateMockImageFile(string fileName, string contentType)
        {
            var mockFile = new Mock<IFormFile>();
            var content = "fake image content for testing";
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
                    ms.Position = 0;
                    return ms.CopyToAsync(stream, token);
                });

            return mockFile;
        }

        #endregion
    }
}
