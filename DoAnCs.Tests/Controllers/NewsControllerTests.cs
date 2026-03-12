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
using DoAnCs.Areas.Admin.Controllers;
// Import các namespace cơ bản của .NET
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DoAnCs.Tests.Controllers
{
    /// <summary>
    /// Test class cho NewsController
    /// Kiểm tra các chức năng quản lý tin tức
    /// </summary>
    [TestFixture]
    public class NewsControllerTests
    {
        #region Private Fields

        // Mock repository để giả lập dữ liệu
        private Mock<INewsRepository> _mockNewsRepo = null!;

        // Controller được test
        private NewsController _controller = null!;

        #endregion

        #region Setup and Teardown

        /// <summary>
        /// Phương thức Setup được chạy trước mỗi test case
        /// Khởi tạo mock repository và controller
        /// </summary>
        [SetUp]
        public void Setup()
        {
            // Tạo mock cho INewsRepository để giả lập dữ liệu
            _mockNewsRepo = new Mock<INewsRepository>();

            // Khởi tạo controller với mock repository
            // Truyền null cho context và logger vì chỉ test logic nghiệp vụ
            _controller = new NewsController(_mockNewsRepo.Object, null!, null!);
        }
        [TearDown]
        public void TearDown()
        {
            _controller.Dispose();
        }
        #endregion

        #region Create Tests

        /// <summary>
        /// Test case: Thêm tin tức thành công không có hình ảnh
        /// Kịch bản: Tạo tin tức mới với đầy đủ thông tin nhưng không có file hình ảnh
        /// Kết quả mong đợi: Tin tức được thêm thành công, trả về JSON success = true
        /// </summary>
        [Test]
        public async Task Create_ValidModelWithoutImage_ReturnsSuccessJson()
        {
            // Arrange - Chuẩn bị dữ liệu test
            var model = new TinTuc
            {
                ID_ChuDe = "CD001",
                TieuDe = "Tin tức mới về du lịch",
                NoiDung = "Nội dung tin tức về các địa điểm du lịch hấp dẫn",
                TacGia = "Admin",
                TrangThai = "active"
            };

            // Setup mock để AddTinTucAsync không ném exception
            _mockNewsRepo.Setup(repo => repo.AddTinTucAsync(It.IsAny<TinTuc>()))
                .Returns(Task.CompletedTask);

            // Act - Thực hiện hành động cần test
            var result = await _controller.Create(model, null);

            // Assert - Kiểm tra kết quả
            // Kiểm tra result là JsonResult
            result.Should().BeOfType<JsonResult>();

            var jsonResult = result as JsonResult;
            jsonResult.Should().NotBeNull();

            // Kiểm tra giá trị JSON trả về
            var value = jsonResult!.Value;
            var successProperty = value!.GetType().GetProperty("success");
            var successValue = successProperty!.GetValue(value);
            successValue.Should().Be(true);

            // Verify AddTinTucAsync được gọi đúng 1 lần
            _mockNewsRepo.Verify(repo => repo.AddTinTucAsync(It.Is<TinTuc>(t =>
                t.TieuDe == model.TieuDe &&
                t.NoiDung == model.NoiDung &&
                t.TacGia == model.TacGia &&
                t.ID_ChuDe == model.ID_ChuDe &&
                t.Ma_TinTuc.StartsWith("N") &&
                t.NgayDang != default(DateTime)
            )), Times.Once);
        }

        /// <summary>
        /// Test case: Thêm tin tức với hình ảnh hợp lệ (PNG)
        /// Kịch bản: Tạo tin tức mới với file hình ảnh PNG hợp lệ
        /// Kết quả mong đợi: Tin tức được thêm thành công, file được lưu, trả về success = true
        /// </summary>
        [Test]
        public async Task Create_ValidModelWithValidPngImage_ReturnsSuccessJson()
        {
            // Arrange
            var model = new TinTuc
            {
                ID_ChuDe = "CD001",
                TieuDe = "Tin tức có hình ảnh",
                NoiDung = "Nội dung tin tức có hình ảnh đính kèm",
                TacGia = "Admin",
                TrangThai = "active"
            };

            // Tạo mock IFormFile cho hình ảnh PNG
            var fileMock = new Mock<IFormFile>();
            var content = "fake image content";
            var fileName = "test.png";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            fileMock.Setup(_ => _.ContentType).Returns("image/png");
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream stream, CancellationToken token) => ms.CopyToAsync(stream, token));

            _mockNewsRepo.Setup(repo => repo.AddTinTucAsync(It.IsAny<TinTuc>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(model, fileMock.Object);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var value = jsonResult!.Value;
            var successProperty = value!.GetType().GetProperty("success");
            var successValue = successProperty!.GetValue(value);
            successValue.Should().Be(true);

            // Verify AddTinTucAsync được gọi với model có HinhAnh không null
            _mockNewsRepo.Verify(repo => repo.AddTinTucAsync(It.Is<TinTuc>(t =>
                !string.IsNullOrEmpty(t.HinhAnh) &&
                t.HinhAnh.StartsWith("/img/news/")
            )), Times.Once);
        }

        /// <summary>
        /// Test case: Thêm tin tức với hình ảnh hợp lệ (JPG)
        /// Kịch bản: Tạo tin tức mới với file hình ảnh JPG hợp lệ
        /// Kết quả mong đợi: Tin tức được thêm thành công, file được lưu, trả về success = true
        /// </summary>
        [Test]
        public async Task Create_ValidModelWithValidJpgImage_ReturnsSuccessJson()
        {
            // Arrange
            var model = new TinTuc
            {
                ID_ChuDe = "CD001",
                TieuDe = "Tin tức có hình JPG",
                NoiDung = "Nội dung tin tức có hình ảnh JPG",
                TacGia = "Admin",
                TrangThai = "active"
            };

            // Tạo mock IFormFile cho hình ảnh JPEG
            var fileMock = new Mock<IFormFile>();
            var content = "fake jpg image content";
            var fileName = "test.jpg";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream stream, CancellationToken token) => ms.CopyToAsync(stream, token));

            _mockNewsRepo.Setup(repo => repo.AddTinTucAsync(It.IsAny<TinTuc>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(model, fileMock.Object);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var value = jsonResult!.Value;
            var successProperty = value!.GetType().GetProperty("success");
            var successValue = successProperty!.GetValue(value);
            successValue.Should().Be(true);

            _mockNewsRepo.Verify(repo => repo.AddTinTucAsync(It.IsAny<TinTuc>()), Times.Once);
        }

        /// <summary>
        /// Test case: Thêm tin tức với file không hợp lệ (PDF)
        /// Kịch bản: Tạo tin tức với file PDF (không được phép)
        /// Kết quả mong đợi: Trả về JSON với success = false và message về loại file không hợp lệ
        /// </summary>
        [Test]
        public async Task Create_WithInvalidFileType_ReturnsErrorJson()
        {
            // Arrange
            var model = new TinTuc
            {
                ID_ChuDe = "CD001",
                TieuDe = "Tin tức với file không hợp lệ",
                NoiDung = "Nội dung tin tức",
                TacGia = "Admin",
                TrangThai = "active"
            };

            // Tạo mock IFormFile cho file PDF (không hợp lệ)
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(_ => _.FileName).Returns("document.pdf");
            fileMock.Setup(_ => _.Length).Returns(1024);
            fileMock.Setup(_ => _.ContentType).Returns("application/pdf");

            // Act
            var result = await _controller.Create(model, fileMock.Object);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var value = jsonResult!.Value;

            var successProperty = value!.GetType().GetProperty("success");
            var successValue = successProperty!.GetValue(value);
            successValue.Should().Be(false);

            var messageProperty = value.GetType().GetProperty("message");
            var messageValue = messageProperty!.GetValue(value) as string;
            messageValue.Should().Contain("Chỉ hỗ trợ file PNG hoặc JPG");

            // Verify AddTinTucAsync không được gọi
            _mockNewsRepo.Verify(repo => repo.AddTinTucAsync(It.IsAny<TinTuc>()), Times.Never);
        }

        /// <summary>
        /// Test case: Thêm tin tức với file quá lớn (>5MB)
        /// Kịch bản: Tạo tin tức với file hình ảnh vượt quá 5MB
        /// Kết quả mong đợi: Trả về JSON với success = false và message về kích thước file
        /// </summary>
        [Test]
        public async Task Create_WithOversizedFile_ReturnsErrorJson()
        {
            // Arrange
            var model = new TinTuc
            {
                ID_ChuDe = "CD001",
                TieuDe = "Tin tức với file quá lớn",
                NoiDung = "Nội dung tin tức",
                TacGia = "Admin",
                TrangThai = "active"
            };

            // Tạo mock IFormFile với kích thước > 5MB
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(_ => _.FileName).Returns("large-image.png");
            fileMock.Setup(_ => _.Length).Returns(6 * 1024 * 1024); // 6MB
            fileMock.Setup(_ => _.ContentType).Returns("image/png");

            // Act
            var result = await _controller.Create(model, fileMock.Object);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var value = jsonResult!.Value;

            var successProperty = value!.GetType().GetProperty("success");
            var successValue = successProperty!.GetValue(value);
            successValue.Should().Be(false);

            var messageProperty = value.GetType().GetProperty("message");
            var messageValue = messageProperty!.GetValue(value) as string;
            messageValue.Should().Contain("File quá lớn");

            // Verify AddTinTucAsync không được gọi
            _mockNewsRepo.Verify(repo => repo.AddTinTucAsync(It.IsAny<TinTuc>()), Times.Never);
        }

        /// <summary>
        /// Test case: Kiểm tra mã tin tức được tạo đúng format
        /// Kịch bản: Tạo tin tức mới và kiểm tra Ma_TinTuc có bắt đầu bằng "N"
        /// Kết quả mong đợi: Ma_TinTuc bắt đầu bằng "N" và có độ dài 19 ký tự
        /// </summary>
        [Test]
        public async Task Create_GeneratesCorrectNewsMaFormat()
        {
            // Arrange
            var model = new TinTuc
            {
                ID_ChuDe = "CD001",
                TieuDe = "Test mã tin tức",
                NoiDung = "Nội dung test",
                TacGia = "Admin",
                TrangThai = "active"
            };

            TinTuc? capturedTinTuc = null;
            _mockNewsRepo.Setup(repo => repo.AddTinTucAsync(It.IsAny<TinTuc>()))
                .Callback<TinTuc>(t => capturedTinTuc = t)
                .Returns(Task.CompletedTask);

            // Act
            await _controller.Create(model, null);

            // Assert
            capturedTinTuc.Should().NotBeNull();
            capturedTinTuc!.Ma_TinTuc.Should().StartWith("N");
            capturedTinTuc.Ma_TinTuc.Should().HaveLength(19); // "N" + 18 ký tự
        }

        /// <summary>
        /// Test case: Thêm tin tức với file có length = 0 (empty file)
        /// Kịch bản: File được upload nhưng không có nội dung (0 bytes)
        /// Kết quả mong đợi: Vẫn tạo tin tức thành công vì controller chỉ check > 0 trong điều kiện if
        /// </summary>
        [Test]
        public async Task Create_WithEmptyFile_SkipsFileUpload()
        {
            // Arrange
            var model = new TinTuc
            {
                ID_ChuDe = "CD001",
                TieuDe = "Tin tức với file rỗng",
                NoiDung = "Nội dung tin tức",
                TacGia = "Admin",
                TrangThai = "active"
            };

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(_ => _.FileName).Returns("empty.png");
            fileMock.Setup(_ => _.Length).Returns(0); // Empty file
            fileMock.Setup(_ => _.ContentType).Returns("image/png");

            _mockNewsRepo.Setup(repo => repo.AddTinTucAsync(It.IsAny<TinTuc>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(model, fileMock.Object);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var value = jsonResult!.Value;
            var successProperty = value!.GetType().GetProperty("success");
            successProperty!.GetValue(value).Should().Be(true);

            // Verify tin tức được thêm nhưng không có HinhAnh
            _mockNewsRepo.Verify(repo => repo.AddTinTucAsync(It.Is<TinTuc>(t =>
                string.IsNullOrEmpty(t.HinhAnh)
            )), Times.Once);
        }

        /// <summary>
        /// Test case: Repository throw exception khi thêm tin tức
        /// Kịch bản: Database connection lỗi hoặc constraint violation
        /// Kết quả mong đợi: Trả về JSON với success = false và message lỗi
        /// </summary>
        [Test]
        public async Task Create_WhenRepositoryThrowsException_ReturnsErrorJson()
        {
            // Arrange
            var model = new TinTuc
            {
                ID_ChuDe = "CD001",
                TieuDe = "Test exception handling",
                NoiDung = "Nội dung test",
                TacGia = "Admin",
                TrangThai = "active"
            };

            // Setup repository để throw exception
            _mockNewsRepo.Setup(repo => repo.AddTinTucAsync(It.IsAny<TinTuc>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _controller.Create(model, null);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var value = jsonResult!.Value;

            var successProperty = value!.GetType().GetProperty("success");
            var successValue = successProperty!.GetValue(value);
            successValue.Should().Be(false);

            var messageProperty = value.GetType().GetProperty("message");
            var messageValue = messageProperty!.GetValue(value) as string;
            messageValue.Should().Be("Có lỗi xảy ra khi thêm bài viết");

            // Verify repository được gọi
            _mockNewsRepo.Verify(repo => repo.AddTinTucAsync(It.IsAny<TinTuc>()), Times.Once);
        }

        /// <summary>
        /// Test case: Kiểm tra NgayDang được set tự động
        /// Kịch bản: Tạo tin tức mới mà không set NgayDang
        /// Kết quả mong đợi: NgayDang được tự động set là DateTime.UtcNow
        /// </summary>
        [Test]
        public async Task Create_AutomaticallySetNgayDang()
        {
            // Arrange
            var model = new TinTuc
            {
                ID_ChuDe = "CD001",
                TieuDe = "Test NgayDang",
                NoiDung = "Nội dung test",
                TacGia = "Admin",
                TrangThai = "active"
            };

            TinTuc? capturedTinTuc = null;
            _mockNewsRepo.Setup(repo => repo.AddTinTucAsync(It.IsAny<TinTuc>()))
                .Callback<TinTuc>(t => capturedTinTuc = t)
                .Returns(Task.CompletedTask);

            var beforeCreate = DateTime.UtcNow;

            // Act
            await _controller.Create(model, null);

            var afterCreate = DateTime.UtcNow;

            // Assert
            capturedTinTuc.Should().NotBeNull();
            capturedTinTuc!.NgayDang.Should().BeOnOrAfter(beforeCreate);
            capturedTinTuc.NgayDang.Should().BeOnOrBefore(afterCreate);
        }

        #endregion
    }
}
