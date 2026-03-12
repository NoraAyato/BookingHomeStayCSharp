using DoAnCs.Models;
using DoAnCs.Repository;
using DoAnCs.Serivces;
using DoAnCs.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nest;
using Elasticsearch.Net;
using DoAnCs.Controllers;
using DoAnCs.Models.Momo;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình DbContext sử dụng SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
// Configure MoMoSettings
builder.Services.Configure<MoMoSettings>(builder.Configuration.GetSection("MoMoSettings"));

// Cấu hình Cookie Authentication 
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Home/Index";
    options.AccessDeniedPath = "/Home/Index";
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Redirect("/Home/Index?showLoginModal=true&requiresAuth=true");
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.Redirect("/Home/Index?showLoginModal=true&requiresAuth=true");
        return Task.CompletedTask;
    };
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});
// Thêm IHttpClientFactory
builder.Services.AddHttpClient();
// Cấu hình Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(googleOptions =>
{
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    googleOptions.CallbackPath = "/signin-google";
    googleOptions.SaveTokens = true;
});

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký các repository và service
builder.Services.AddScoped<IHomestayRepository, HomestayRepository>();
builder.Services.AddScoped<IKhuVucRepository, KhuVucRepository>();
builder.Services.AddScoped<IPhongRepository, PhongRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPhieuDatPhongRepository, PhieuDatPhongRepository>();
builder.Services.AddScoped<IThanhToanRepository, ThanhToanRepository>();
builder.Services.AddScoped<IHoaDonRepository, HoaDonRepository>();
builder.Services.AddScoped<IKhuyenMaiRepository, KhuyenMaiRepository>();
builder.Services.AddScoped<INewsRepository, NewsRepository>();
builder.Services.AddScoped<ITienNghiRepository, TienNghiRepository>();
builder.Services.AddScoped<IPhuThuRepository, PhuThuRepository>();
builder.Services.AddScoped<IDanhGiaRepository, DanhGiaRepository>();
builder.Services.AddScoped<ISelectedRoomsService, SelectedRoomsService>();
builder.Services.AddScoped<IChinhSachRepository, ChinhSachRepository>();
builder.Services.AddScoped<IHopDongRepository, HopDongRepository>();
builder.Services.AddScoped<IHuyPhongRepository, HuyPhongRepository>();
builder.Services.AddHostedService<BookingCleanupService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IMoMoPaymentService, MoMoPaymentService>();

// Đọc cấu hình Elasticsearch từ appsettings.json
var elasticsearchSettings = builder.Configuration.GetSection("Elasticsearch");
var url = elasticsearchSettings["Url"];
var defaultIndex = elasticsearchSettings["IndexName"];
var username = elasticsearchSettings["Username"];
var password = elasticsearchSettings["Password"];

// Cấu hình ElasticClient với xác thực
var settings = new ConnectionSettings(new Uri(url))
    .BasicAuthentication(username, password)
    .DefaultIndex(defaultIndex)
    .ServerCertificateValidationCallback((o, certificate, chain, errors) => true);

// Tạo ElasticClient
var client = new ElasticClient(settings);

//// Kiểm tra kết nối với Elasticsearch
//var healthResponse = client.Cluster.Health();
//if (!healthResponse.IsValid)
//{
//    Console.WriteLine("Cannot connect to Elasticsearch: " + healthResponse.OriginalException?.Message);
//    throw new Exception("Failed to connect to Elasticsearch: " + healthResponse.OriginalException?.Message);
//}

// Đăng ký ElasticClient vào DI
builder.Services.AddSingleton<IElasticClient>(client);
builder.Services.AddScoped<IElasticsearchService, ElasticsearchService>();

var app = builder.Build();

//// Seed dữ liệu khi khởi động (tùy chọn)
//using (var scope = app.Services.CreateScope())
//{
//    var elasticsearchService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();
//    await elasticsearchService.SeedDataAsync();
//}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=DashBoard}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();