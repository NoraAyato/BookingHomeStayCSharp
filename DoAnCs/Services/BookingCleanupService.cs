using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using DoAnCs.Models;
using DoAnCs.Repository;

namespace DoAnCs.Services
{
    public class BookingCleanupService : BackgroundService
    {
        private readonly ILogger<BookingCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public BookingCleanupService(ILogger<BookingCleanupService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("BookingCleanupService is starting. Check interval: {Interval} minutes.", _checkInterval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Starting a new cleanup cycle at {Time}.", DateTime.Now);

                try
                {
                    await CleanupUnpaidBookings(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up unpaid bookings. Details: {ErrorMessage}", ex.Message);
                }

                _logger.LogDebug("Finished cleanup cycle. Waiting for next check in {Interval} minutes.", _checkInterval.TotalMinutes);
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("BookingCleanupService is stopping.");
        }

        private async Task CleanupUnpaidBookings(CancellationToken stoppingToken)
        {
            _logger.LogDebug("Entering CleanupUnpaidBookings method.");

            using (var scope = _serviceProvider.CreateScope())
            {
                var hoaDonRepository = scope.ServiceProvider.GetRequiredService<IHoaDonRepository>();
                var phieuDatPhongRepository = scope.ServiceProvider.GetRequiredService<IPhieuDatPhongRepository>();
                var khuyenMaiRepository = scope.ServiceProvider.GetRequiredService<IKhuyenMaiRepository>();
                var thanhToanRepository = scope.ServiceProvider.GetRequiredService<IThanhToanRepository>();
                _logger.LogDebug("Successfully retrieved repositories.");

                var now = DateTime.Now;
                _logger.LogInformation("Current time: {Now}. Will check for invoices older than 30 minutes.", now);

                var allInvoices = await hoaDonRepository.GetAllAsync();
                _logger.LogInformation("Retrieved {Count} invoices from database.", allInvoices.Count());

                foreach (var invoice in allInvoices)
                {
                    _logger.LogDebug("Invoice details: MaHD={MaHD}, TrangThai={TrangThai}, NgayLap={NgayLap}, MinutesElapsed={MinutesElapsed}",
                        invoice.Ma_HD,
                        invoice.TrangThai,
                        invoice.NgayLap,
                        invoice.NgayLap != null ? (now - invoice.NgayLap).TotalMinutes : "NgayLap is null");
                }

                var unpaidInvoices = allInvoices
                    .Where(hd =>
                        hd.TrangThai?.Trim() == "Chưa thanh toán" &&
                        hd.NgayLap != null &&
                        (now - hd.NgayLap).TotalMinutes > 15)//set up thời gian
                    .ToList();

                _logger.LogInformation("Found {Count} unpaid invoices to clean up.", unpaidInvoices.Count);

                if (!unpaidInvoices.Any())
                {
                    _logger.LogDebug("No unpaid invoices found that are older than 30 minutes.");
                    return;
                }

                foreach (var hoaDon in unpaidInvoices)
                {
                    _logger.LogInformation("Processing cleanup for invoice: MaHD={MaHD}, CreatedAt={NgayLap}", hoaDon.Ma_HD, hoaDon.NgayLap);

                    // Lấy danh sách ChiTietHoaDon để lưu Ma_PDPhong trước khi xóa
                    var chiTietHoaDons = await hoaDonRepository.GetByIdAsync(hoaDon.Ma_HD)
                        .ContinueWith(t => t.Result?.ChiTietHoaDons ?? new List<ChiTietHoaDon>(), stoppingToken);

                    _logger.LogDebug("Found {Count} ChiTietHoaDons for invoice MaHD={MaHD}.", chiTietHoaDons.Count, hoaDon.Ma_HD);

                    // Lưu danh sách Ma_PDPhong để xóa sau
                    var maPDPhongsToDelete = chiTietHoaDons
                        .Select(ct => ct.Ma_PDPhong)
                        .Distinct()
                        .ToList();

                    _logger.LogDebug("Found {Count} PhieuDatPhong IDs to delete: {MaPDPhongs}", maPDPhongsToDelete.Count, string.Join(", ", maPDPhongsToDelete));
                    await hoaDonRepository.DeleteAsync(hoaDon.Ma_HD);
                    _logger.LogInformation("Successfully deleted HoaDon: MaHD={MaHD}.", hoaDon.Ma_HD);

                    // Sau đó xóa tất cả PhieuDatPhong liên quan
                    foreach (var maPDPhong in maPDPhongsToDelete)
                    {
                        var phieuDatPhong = await phieuDatPhongRepository.GetByIdAsync(maPDPhong);
                        if (phieuDatPhong != null)
                        {
                            await phieuDatPhongRepository.DeleteAsync(maPDPhong);
                            _logger.LogInformation("Deleted PhieuDatPhong: MaPDPhong={MaPDPhong}.", maPDPhong);
                        }
                        else
                        {
                            _logger.LogWarning("PhieuDatPhong not found for MaPDPhong={MaPDPhong}.", maPDPhong);
                        }
                    }
                }
            }

            _logger.LogDebug("Exiting CleanupUnpaidBookings method.");
        }
    }
}