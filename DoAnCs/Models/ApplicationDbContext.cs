    using DoAnCs.Models;
using Humanizer;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseLazyLoadingProxies(); // Bật Lazy Loading Proxies
        }
        // Bảng hệ thống người dùng (Identity)
        public DbSet<ThanhToan> ThanhToans { get; set; }
        public DbSet<HoaDon> HoaDons { get; set; }
        public DbSet<KhuyenMai> KhuyenMais { get; set; }
        public DbSet<PhieuSuDungDV> PhieuSuDungDVs { get; set; }
        public DbSet<DichVu> DichVus { get; set; }
        public DbSet<ChiTietPhieuDV> ChiTietPhieuDVs { get; set; }
        public DbSet<Homestay> Homestays { get; set; }
        public DbSet<Phong> Phongs { get; set; }
        public DbSet<LoaiPhong> LoaiPhongs { get; set; }
        public DbSet<ChiTietPhong> ChiTietPhongs { get; set; }
        public DbSet<PhieuDatPhong> PhieuDatPhongs { get; set; }
        public DbSet<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
        public DbSet<TinTuc> TinTucs { get; set; }
        public DbSet<ChuDe> ChuDes { get; set; }
        public DbSet<ApDungKM> ApDungKMs { get; set; }
        public DbSet<KhuVuc> KhuVucs { get; set; }
        public DbSet<PhieuPhuThu> PhieuPhuThus { get; set; }
        public DbSet<ApDungPhuThu> ApDungPhuThus { get; set; }
        public DbSet<DanhGia> DanhGias { get; set; }
        public DbSet<HinhAnhPhong> HinhAnhPhongs { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public DbSet<PhieuHuyPhong> PhieuHuyPhongs { get; set; }
        public DbSet<BinhLuan> BinhLuans { get; set; }
        public DbSet<TienNghi> TienNghis { get; set; }
        public DbSet<ChinhSach> ChinhSachs { get; set; }
       
        public DbSet<KhuyenMaiPhong> KhuyenMaiPhongs { get; set; }
        public DbSet<HopDong> HopDongs { get; set; }
        public DbSet<PhieuHuyHopDong> PhieuHuyHopDongs { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

 

        // Tắt cascade delete cho Homestay -> Phong
        modelBuilder.Entity<Phong>()
            .HasOne(p => p.Homestay)
            .WithMany(h => h.Phongs)
            .HasForeignKey(p => p.ID_Homestay)
            .OnDelete(DeleteBehavior.Restrict);

        // Tắt cascade delete cho Đánh giá 
        modelBuilder.Entity<DanhGia>()
           .HasOne(pd => pd.NguoiDung)
           .WithMany(p => p.DanhGias)
           .HasForeignKey(pd => pd.Ma_ND)
           .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DanhGia>()
            .HasOne(pd => pd.Homestay)
            .WithMany(d => d.DanhGias)
            .HasForeignKey(pd => pd.ID_Homestay)
            .OnDelete(DeleteBehavior.Restrict);
        // Tắt cascade delete cho các bảng liên quan khác
        modelBuilder.Entity<HinhAnhPhong>()
            .HasOne(h => h.Phong)
            .WithMany(p => p.HinhAnhPhongs)
            .HasForeignKey(h => h.MaPhong)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChiTietPhong>()
            .HasOne(c => c.Phong)
            .WithMany(p => p.ChiTietPhongs)
            .HasForeignKey(c => c.Ma_Phong)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChiTietDatPhong>()
            .HasOne(c => c.Phong)
            .WithMany(p => p.ChiTietDatPhongs)
            .HasForeignKey(c => c.Ma_Phong)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChiTietPhieuDV>()
            .HasOne(ct => ct.PhieuSuDungDV) // Thuộc tính điều hướng
            .WithMany(p => p.ChiTietPhieuDVs) // Tập hợp điều hướng ngược (nếu có)
            .HasForeignKey(ct => ct.Ma_Phieu)
            .OnDelete(DeleteBehavior.NoAction); // Tắt CASCADE, sử dụng NO ACTION

        modelBuilder.Entity<ChiTietHoaDon>()
            .HasOne(ct => ct.HoaDon)
            .WithMany(h => h.ChiTietHoaDons)
            .HasForeignKey(ct => ct.Ma_HD)
            .OnDelete(DeleteBehavior.Cascade); // Giữ CASCADE cho HoaDons

        // Cấu hình khóa ngoại với PhieuDatPhongs
        modelBuilder.Entity<ChiTietHoaDon>()
            .HasOne(ct => ct.PhieuDatPhong)
            .WithMany(p => p.ChiTietHoaDons)
            .HasForeignKey(ct => ct.Ma_PDPhong)
            .OnDelete(DeleteBehavior.NoAction); // Sử dụng NO ACTION để tránh chu kỳ

        modelBuilder.Entity<KhuyenMai>()
    .HasOne(km => km.NguoiTao)
    .WithMany(u => u.KhuyenMais)
    .HasForeignKey(km => km.NguoiTaoId)
    .OnDelete(DeleteBehavior.NoAction);
    }
    //protected override void OnModelCreating(ModelBuilder modelBuilder)
    //{
    //    base.OnModelCreating(modelBuilder);
    //    modelBuilder.Entity<PhieuHuyPhong>().HasKey(p => new { p.MaPHP, p.Ma_PDPhong });
    //    modelBuilder.Entity<ChiTietHoaDon>().HasKey(c => new { c.Ma_HD, c.Ma_PDPhong });

    //    modelBuilder.Entity<PhieuHuyPhong>()
    //            .HasOne(p => p.PhieuDatPhong)
    //            .WithMany(p => p.PhieuHuyPhongs)
    //            .HasForeignKey(p => p.Ma_PDPhong);
    //    modelBuilder.Entity<BinhLuan>()
    //     .HasOne(b => b.TinTuc)
    //     .WithMany(t => t.BinhLuans)
    //     .HasForeignKey(b => b.Ma_TinTuc)
    //     .OnDelete(DeleteBehavior.Cascade);


    //    modelBuilder.Entity<BinhLuan>()
    //        .HasOne(b => b.User)
    //        .WithMany()
    //        .HasForeignKey(b => b.UserId)
    //        .OnDelete(DeleteBehavior.Restrict);

    //    // Cấu hình self-referencing (Phản hồi)
    //    modelBuilder.Entity<BinhLuan>()
    //        .HasOne(b => b.BinhLuanCha)
    //        .WithMany(b => b.PhanHois)
    //        .HasForeignKey(b => b.BinhLuanChaId)
    //        .OnDelete(DeleteBehavior.Restrict);
    //    // Cấu hình các khóa chính và quan hệ
    //    modelBuilder.Entity<ApplicationRole>()
    //        .Property(r => r.Description)
    //        .HasMaxLength(256);
    //    modelBuilder.Entity<HinhAnhPhong>()
    //       .HasOne(h => h.Phong)
    //       .WithMany(p => p.HinhAnhPhongs)
    //       .HasForeignKey(h => h.MaPhong)
    //       .OnDelete(DeleteBehavior.NoAction); // Xóa ảnh khi xóa phòng
    //    // 2. ChiTietPhieuDV (ServiceUsageDetail)
    //    modelBuilder.Entity<ChiTietPhieuDV>()
    //        .HasKey(ctpdv => new { ctpdv.Ma_Phieu, ctpdv.Ma_DV, ctpdv.ID_Homestay });

    //    modelBuilder.Entity<ChiTietPhieuDV>()
    //        .HasOne(ctpdv => ctpdv.PhieuSuDungDV)
    //        .WithMany(psd => psd.ChiTietPhieuDVs)
    //        .HasForeignKey(ctpdv => ctpdv.Ma_Phieu);

    //    modelBuilder.Entity<ChiTietPhieuDV>()
    //        .HasOne(ctpdv => ctpdv.DichVu)
    //        .WithMany(dv => dv.ChiTietPhieuDVs)
    //        .HasForeignKey(ctpdv => new { ctpdv.Ma_DV, ctpdv.ID_Homestay });

    //    // 3. ChiTietDatPhong (BookingDetail)
    //    modelBuilder.Entity<ChiTietDatPhong>()
    //        .HasKey(ctdp => new { ctdp.Ma_PDPhong, ctdp.Ma_Phong });

    //    modelBuilder.Entity<ChiTietDatPhong>()
    //        .HasOne(ctdp => ctdp.PhieuDatPhong)
    //        .WithMany(pdp => pdp.ChiTietDatPhongs)
    //        .HasForeignKey(ctdp => ctdp.Ma_PDPhong);

    //    modelBuilder.Entity<ChiTietDatPhong>()
    //        .HasOne(ctdp => ctdp.Phong)
    //        .WithMany(p => p.ChiTietDatPhongs)
    //        .HasForeignKey(ctdp => ctdp.Ma_Phong)
    //        .OnDelete(DeleteBehavior.Restrict); // Vô hiệu hóa Cascade Delete

    //    // 4. ApDungKM (PromotionApplication)
    //    modelBuilder.Entity<ApDungKM>()
    //        .HasKey(adkm => new { adkm.Ma_HD, adkm.Ma_KM });

    //    modelBuilder.Entity<ApDungKM>()
    //        .HasOne(adkm => adkm.HoaDon)
    //        .WithMany(hd => hd.ApDungKMs)
    //        .HasForeignKey(adkm => adkm.Ma_HD);

    //    modelBuilder.Entity<ApDungKM>()
    //        .HasOne(adkm => adkm.KhuyenMai)
    //        .WithMany(km => km.ApDungKMs)
    //        .HasForeignKey(adkm => adkm.Ma_KM);

    //    // 5. ApDungPhuThu (SurchargeApplication)
    //    modelBuilder.Entity<ApDungPhuThu>()
    //        .HasKey(adpt => new { adpt.ID_Loai, adpt.Ma_PhieuPT });

    //    modelBuilder.Entity<ApDungPhuThu>()
    //        .HasOne(adpt => adpt.LoaiPhong)
    //        .WithMany(p => p.ApDungPhuThus)
    //        .HasForeignKey(adpt => adpt.ID_Loai);

    //    modelBuilder.Entity<ApDungPhuThu>()
    //        .HasOne(adpt => adpt.PhieuPhuThu)
    //        .WithMany(ppt => ppt.ApDungPhuThus)
    //        .HasForeignKey(adpt => adpt.Ma_PhieuPT);

    //    // 6. DanhGia (Review)
    //    modelBuilder.Entity<DanhGia>()
    //        .HasKey(dg => new { dg.Ma_ND, dg.ID_Homestay, dg.ID_DG });

    //    modelBuilder.Entity<DanhGia>()
    //    .HasOne(dg => dg.NguoiDung)
    //    .WithMany(nd => nd.DanhGias)
    //    .HasForeignKey(dg => dg.Ma_ND)
    //    .OnDelete(DeleteBehavior.Restrict); // Vô hiệu hóa Cascade Delete

    //    modelBuilder.Entity<DanhGia>()
    //    .HasOne(dg => dg.Homestay)
    //    .WithMany(hs => hs.DanhGias)
    //    .HasForeignKey(dg => dg.ID_Homestay)
    //    .OnDelete(DeleteBehavior.Restrict); // Vô hiệu hóa Cascade Delete



    //    // 8. PhieuSuDungDV (ServiceUsage)
    //    modelBuilder.Entity<PhieuSuDungDV>()
    //    .HasOne(p => p.ChiTietDatPhong)
    //    .WithMany(c => c.PhieuSuDungDVs)
    //    .HasForeignKey(p => new { p.Ma_PDPhong, p.Ma_Phong })
    //    .OnDelete(DeleteBehavior.Restrict); // Xóa phiếu khi xóa đặt phòng

    //    // 9. HoaDon (Invoice)
    //    modelBuilder.Entity<ThanhToan>()
    //        .HasOne(hd => hd.HoaDon)
    //        .WithMany(tt => tt.ThanhToans)
    //        .HasForeignKey(hd => hd.MaHD);

    //    modelBuilder.Entity<ChiTietHoaDon>()
    //           .HasOne(c => c.HoaDon)
    //           .WithMany(h => h.ChiTietHoaDons)
    //           .HasForeignKey(c => c.Ma_HD);

    //    modelBuilder.Entity<ChiTietHoaDon>()
    //        .HasOne(c => c.PhieuDatPhong)
    //        .WithMany(p => p.ChiTietHoaDons)
    //        .HasForeignKey(c => c.Ma_PDPhong).OnDelete(DeleteBehavior.NoAction);

    //    modelBuilder.Entity<HoaDon>()
    //        .HasOne(hd => hd.NguoiDung)
    //        .WithMany(nd => nd.HoaDons)
    //        .HasForeignKey(hd => hd.Ma_ND);

    //    // 10. Phong (Room)
    //    modelBuilder.Entity<Phong>()
    //        .HasOne(p => p.Homestay)
    //        .WithMany(hs => hs.Phongs)
    //        .HasForeignKey(p => p.ID_Homestay);

    //    modelBuilder.Entity<Phong>()
    //        .HasOne(p => p.LoaiPhong)
    //        .WithMany(lp => lp.Phongs)
    //        .HasForeignKey(p => p.ID_Loai);

    //    // 11. DichVu (Service)
    //    modelBuilder.Entity<DichVu>()
    //        .HasKey(dv => new { dv.Ma_DV, dv.ID_Homestay });

    //    modelBuilder.Entity<DichVu>()
    //        .HasOne(dv => dv.Homestay)
    //        .WithMany(hs => hs.DichVus)
    //        .HasForeignKey(dv => dv.ID_Homestay);

    //    // 12. Homestay
    //    modelBuilder.Entity<Homestay>()
    //        .HasOne(hs => hs.KhuVuc)
    //        .WithMany(kv => kv.Homestays)
    //        .HasForeignKey(hs => hs.Ma_KV);

    //    modelBuilder.Entity<Homestay>()
    //        .HasOne(hs => hs.NguoiDung)
    //        .WithMany(nd => nd.Homestays)
    //        .HasForeignKey(hs => hs.Ma_ND);

    //    // 13. TinTuc (News)
    //    modelBuilder.Entity<TinTuc>()
    //        .HasOne(tt => tt.ChuDe)
    //        .WithMany(cd => cd.TinTucs)
    //        .HasForeignKey(tt => tt.ID_ChuDe);

    //    // 14. ChiTietPhong (RoomDetail)
    //    modelBuilder.Entity<ChiTietPhong>()
    //        .HasOne(ctp => ctp.Phong)
    //        .WithMany(p => p.ChiTietPhongs)
    //        .HasForeignKey(ctp => ctp.Ma_Phong);

    //    // 15. PhieuDatPhong (Booking)
    //    modelBuilder.Entity<PhieuDatPhong>()
    //        .HasOne(pdp => pdp.NguoiDung)
    //        .WithMany(nd => nd.PhieuDatPhongs)
    //        .HasForeignKey(pdp => pdp.Ma_ND);
    //}
}
