// Import thư viện NUnit để viết và chạy unit test
using NUnit.Framework;
// Import thư viện Moq để tạo mock objects (đối tượng giả lập)
using Moq;
// Import FluentAssertions để viết assertion một cách dễ đọc hơn
using FluentAssertions;
// Import Microsoft.Extensions.Logging để làm việc với logging
using Microsoft.Extensions.Logging;
// Import Microsoft.Extensions.DependencyInjection để làm việc với dependency injection
using Microsoft.Extensions.DependencyInjection;
// Import các service và repository từ project chính
using DoAnCs.Services;
using DoAnCs.Repository;
using DoAnCs.Models;
// Import các namespace cơ bản của .NET
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Demo.Tests
{
    /// <summary>
    /// Test class cho BookingCleanupService
    /// Kiểm tra các chức năng cleanup hóa đơn chưa thanh toán hết hạn
    /// </summary>
    [TestFixture]
    public class BookingCleanupServiceTests
    {
        #region Private Fields

        // Mock objects để giả lập dependencies
        private Mock<ILogger<BookingCleanupService>> _mockLogger = null!;
        private Mock<IServiceProvider> _mockServiceProvider = null!;
        private Mock<IServiceScope> _mockServiceScope = null!;
        private Mock<IServiceScopeFactory> _mockServiceScopeFactory = null!;
        private Mock<IHoaDonRepository> _mockHoaDonRepository = null!;
        private Mock<IPhieuDatPhongRepository> _mockPhieuDatPhongRepository = null!;
        private Mock<IKhuyenMaiRepository> _mockKhuyenMaiRepository = null!;
        private Mock<IThanhToanRepository> _mockThanhToanRepository = null!;
        
        // Instance của service cần test
        private BookingCleanupService _service = null!;

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
            _mockLogger = new Mock<ILogger<BookingCleanupService>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockServiceScope = new Mock<IServiceScope>();
            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            _mockHoaDonRepository = new Mock<IHoaDonRepository>();
            _mockPhieuDatPhongRepository = new Mock<IPhieuDatPhongRepository>();
            _mockKhuyenMaiRepository = new Mock<IKhuyenMaiRepository>();
            _mockThanhToanRepository = new Mock<IThanhToanRepository>();

            // Cấu hình dependency injection chain
            SetupDependencyInjection();

            // Tạo instance của BookingCleanupService với các mock dependencies
            _service = new BookingCleanupService(_mockLogger.Object, _mockServiceProvider.Object);
        }

        /// <summary>
        /// TearDown method chạy sau mỗi test case
        /// Giải phóng resources
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        #endregion

        #region Test Cases

        /// <summary>
        /// Test case: Kiểm tra service nhận diện đúng hóa đơn hết hạn
        /// Scenario: Có hóa đơn hết hạn (>15 phút), hợp lệ (<15 phút), và đã thanh toán
        /// Expected: Chỉ hóa đơn hết hạn và chưa thanh toán bị xóa
        /// </summary>
        [Test]
        public async Task CleanupUnpaidBookings_ShouldIdentifyAndDeleteExpiredInvoices()
        {
            // Arrange
            var now = DateTime.Now;
            var invoices = CreateTestInvoices(now);

            _mockHoaDonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(invoices);
            _mockHoaDonRepository.Setup(x => x.DeleteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            await InvokeCleanupMethod();

            // Assert
            _mockHoaDonRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync("HD001"), Times.Once); // Hóa đơn hết hạn
            _mockHoaDonRepository.Verify(x => x.DeleteAsync("HD002"), Times.Never); // Hóa đơn còn hợp lệ
            _mockHoaDonRepository.Verify(x => x.DeleteAsync("HD003"), Times.Never); // Hóa đơn đã thanh toán
            _mockHoaDonRepository.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test case: Kiểm tra xử lý danh sách hóa đơn rỗng
        /// Scenario: Repository trả về danh sách rỗng
        /// Expected: Không có delete operation nào được thực hiện
        /// </summary>
        [Test]
        public async Task CleanupUnpaidBookings_ShouldHandleEmptyInvoiceList()
        {
            // Arrange
            var emptyInvoices = new List<HoaDon>();
            _mockHoaDonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(emptyInvoices);
            _mockHoaDonRepository.Setup(x => x.DeleteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            await InvokeCleanupMethod();

            // Assert
            _mockHoaDonRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Never);
        }

        /// <summary>
        /// Test case: Kiểm tra filter theo trạng thái hóa đơn
        /// Scenario: Có hóa đơn đã thanh toán, đã hủy, và chưa thanh toán (tất cả đều hết hạn)
        /// Expected: Chỉ hóa đơn "Chưa thanh toán" bị xóa
        /// </summary>
        [Test]
        public async Task CleanupUnpaidBookings_ShouldFilterByStatus()
        {
            // Arrange
            var now = DateTime.Now;
            var invoices = new List<HoaDon>
            {
                new HoaDon { Ma_HD = "HD001", TrangThai = "Đã thanh toán", NgayLap = now.AddMinutes(-30), TongTien = 1000000 },
                new HoaDon { Ma_HD = "HD002", TrangThai = "Đã hủy", NgayLap = now.AddMinutes(-30), TongTien = 500000 },
                new HoaDon { Ma_HD = "HD003", TrangThai = "Chưa thanh toán", NgayLap = now.AddMinutes(-30), TongTien = 750000 }
            };

            _mockHoaDonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(invoices);
            _mockHoaDonRepository.Setup(x => x.DeleteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            await InvokeCleanupMethod();

            // Assert
            _mockHoaDonRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync("HD003"), Times.Once);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync("HD001"), Times.Never);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync("HD002"), Times.Never);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test case: Kiểm tra filter theo ngưỡng thời gian
        /// Scenario: Có hóa đơn gần đây (10 phút) và cũ (20 phút), cả hai đều chưa thanh toán
        /// Expected: Chỉ hóa đơn cũ (>15 phút) bị xóa
        /// </summary>
        [Test]
        public async Task CleanupUnpaidBookings_ShouldFilterByTimeThreshold()
        {
            // Arrange
            var now = DateTime.Now;
            var invoices = new List<HoaDon>
            {
                new HoaDon { Ma_HD = "HD001", TrangThai = "Chưa thanh toán", NgayLap = now.AddMinutes(-10), TongTien = 1000000 }, // Còn hợp lệ
                new HoaDon { Ma_HD = "HD002", TrangThai = "Chưa thanh toán", NgayLap = now.AddMinutes(-20), TongTien = 500000 }  // Hết hạn
            };

            _mockHoaDonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(invoices);
            _mockHoaDonRepository.Setup(x => x.DeleteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            await InvokeCleanupMethod();

            // Assert
            _mockHoaDonRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync("HD002"), Times.Once);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync("HD001"), Times.Never);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Once);
        }

        /// <summary>
        /// Test case: Kiểm tra xử lý nhiều hóa đơn hết hạn cùng lúc
        /// Scenario: Có nhiều hóa đơn chưa thanh toán và hết hạn
        /// Expected: Tất cả hóa đơn hết hạn đều bị xóa
        /// </summary>
        [Test]
        public async Task CleanupUnpaidBookings_ShouldDeleteMultipleExpiredInvoices()
        {
            // Arrange
            var now = DateTime.Now;
            var invoices = new List<HoaDon>
            {
                new HoaDon { Ma_HD = "HD001", TrangThai = "Chưa thanh toán", NgayLap = now.AddMinutes(-20), TongTien = 1000000 },
                new HoaDon { Ma_HD = "HD002", TrangThai = "Chưa thanh toán", NgayLap = now.AddMinutes(-25), TongTien = 500000 },
                new HoaDon { Ma_HD = "HD003", TrangThai = "Chưa thanh toán", NgayLap = now.AddMinutes(-30), TongTien = 750000 },
                new HoaDon { Ma_HD = "HD004", TrangThai = "Chưa thanh toán", NgayLap = now.AddMinutes(-5), TongTien = 300000 }  // Còn hợp lệ
            };

            _mockHoaDonRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(invoices);
            _mockHoaDonRepository.Setup(x => x.DeleteAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            // Act
            await InvokeCleanupMethod();

            // Assert
            _mockHoaDonRepository.Verify(x => x.GetAllAsync(), Times.Once);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync("HD001"), Times.Once);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync("HD002"), Times.Once);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync("HD003"), Times.Once);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync("HD004"), Times.Never);
            _mockHoaDonRepository.Verify(x => x.DeleteAsync(It.IsAny<string>()), Times.Exactly(3));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Cấu hình dependency injection cho testing
        /// </summary>
        private void SetupDependencyInjection()
        {
            var mockScopeServiceProvider = new Mock<IServiceProvider>();
            
            mockScopeServiceProvider.Setup(x => x.GetService(typeof(IHoaDonRepository)))
                                   .Returns(_mockHoaDonRepository.Object);
            mockScopeServiceProvider.Setup(x => x.GetService(typeof(IPhieuDatPhongRepository)))
                                   .Returns(_mockPhieuDatPhongRepository.Object);
            mockScopeServiceProvider.Setup(x => x.GetService(typeof(IKhuyenMaiRepository)))
                                   .Returns(_mockKhuyenMaiRepository.Object);
            mockScopeServiceProvider.Setup(x => x.GetService(typeof(IThanhToanRepository)))
                                   .Returns(_mockThanhToanRepository.Object);

            _mockServiceScope.Setup(x => x.ServiceProvider).Returns(mockScopeServiceProvider.Object);
            _mockServiceScopeFactory.Setup(x => x.CreateScope()).Returns(_mockServiceScope.Object);
            _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                               .Returns(_mockServiceScopeFactory.Object);
        }

        /// <summary>
        /// Tạo dữ liệu test cho các hóa đơn
        /// </summary>
        /// <param name="now">Thời gian hiện tại</param>
        /// <returns>Danh sách hóa đơn test</returns>
        private List<HoaDon> CreateTestInvoices(DateTime now)
        {
            return new List<HoaDon>
            {
                new HoaDon { Ma_HD = "HD001", TrangThai = "Chưa thanh toán", NgayLap = now.AddMinutes(-20), TongTien = 1000000 }, // Hết hạn
                new HoaDon { Ma_HD = "HD002", TrangThai = "Chưa thanh toán", NgayLap = now.AddMinutes(-5), TongTien = 500000 },   // Còn hợp lệ
                new HoaDon { Ma_HD = "HD003", TrangThai = "Đã thanh toán", NgayLap = now.AddMinutes(-30), TongTien = 750000 }    // Đã thanh toán
            };
        }

        /// <summary>
        /// Gọi private method CleanupUnpaidBookings bằng reflection
        /// </summary>
        private async Task InvokeCleanupMethod()
        {
            var method = typeof(BookingCleanupService).GetMethod("CleanupUnpaidBookings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var cancellationToken = new CancellationToken();
            await (Task)method!.Invoke(_service, new object[] { cancellationToken });
        }

        #endregion
    }
}