/*========================================================= 

    SCRIPT SQL SERVER - WEBSITE BAN NOI THAT B2C 

    Project: DoAnNoiThatB2CEntities - ASP.NET MVC 5 

    ERD: TaiKhoan, KhachHang, DanhMuc, SanPham, Gia, 

         GioHang, DonHang, ThanhToan, VanChuyen, DanhGia, Tin_Tuc 

=========================================================*/ 

 

-- Có thể đổi tên database theo project của bạn 

IF DB_ID(N'TMDT_CK_DoAn') IS NULL 

BEGIN 

    CREATE DATABASE DoAnNoiThatB2CEntities; 

END 

GO 

 

USE DoAnNoiThatB2CEntities; 

GO 

 

/*========================================================= 

    DROP TABLES - Chạy lại script từ đầu 

    Lưu ý: Phần này sẽ xóa dữ liệu cũ nếu bảng đã tồn tại. 

=========================================================*/ 

 

IF OBJECT_ID(N'dbo.DANHGIA', N'U') IS NOT NULL DROP TABLE dbo.DANHGIA; 

IF OBJECT_ID(N'dbo.VANCHUYEN', N'U') IS NOT NULL DROP TABLE dbo.VANCHUYEN; 

IF OBJECT_ID(N'dbo.THANHTOAN', N'U') IS NOT NULL DROP TABLE dbo.THANHTOAN; 

IF OBJECT_ID(N'dbo.CHITIET_DONHANG', N'U') IS NOT NULL DROP TABLE dbo.CHITIET_DONHANG; 

IF OBJECT_ID(N'dbo.DONHANG', N'U') IS NOT NULL DROP TABLE dbo.DONHANG; 

IF OBJECT_ID(N'dbo.CHITIET_GIOHANG', N'U') IS NOT NULL DROP TABLE dbo.CHITIET_GIOHANG; 

IF OBJECT_ID(N'dbo.GIOHANG', N'U') IS NOT NULL DROP TABLE dbo.GIOHANG; 

IF OBJECT_ID(N'dbo.GIA', N'U') IS NOT NULL DROP TABLE dbo.GIA; 

IF OBJECT_ID(N'dbo.SANPHAM', N'U') IS NOT NULL DROP TABLE dbo.SANPHAM; 

IF OBJECT_ID(N'dbo.TIN_TUC', N'U') IS NOT NULL DROP TABLE dbo.TIN_TUC; 

IF OBJECT_ID(N'dbo.DANHMUC', N'U') IS NOT NULL DROP TABLE dbo.DANHMUC; 

IF OBJECT_ID(N'dbo.KHACHHANG', N'U') IS NOT NULL DROP TABLE dbo.KHACHHANG; 

IF OBJECT_ID(N'dbo.TAIKHOAN', N'U') IS NOT NULL DROP TABLE dbo.TAIKHOAN; 

GO 

 

/*========================================================= 

    1. TAIKHOAN 

=========================================================*/ 

 

CREATE TABLE dbo.TAIKHOAN 

( 

    MaTK INT IDENTITY(1,1) NOT NULL, 

    HoTen NVARCHAR(100) NOT NULL, 

    Email VARCHAR(150) NOT NULL, 

    MatKhau NVARCHAR(255) NOT NULL, 

    SDT VARCHAR(20) NULL, 

    VaiTro NVARCHAR(30) NOT NULL CONSTRAINT DF_TAIKHOAN_VaiTro DEFAULT N'Customer', 

    TrangThai BIT NOT NULL CONSTRAINT DF_TAIKHOAN_TrangThai DEFAULT 1, 

    NgayTao DATETIME NOT NULL CONSTRAINT DF_TAIKHOAN_NgayTao DEFAULT GETDATE(), 

 

    CONSTRAINT PK_TAIKHOAN PRIMARY KEY (MaTK), 

    CONSTRAINT UQ_TAIKHOAN_Email UNIQUE (Email), 

    CONSTRAINT CK_TAIKHOAN_VaiTro CHECK (VaiTro IN (N'Admin', N'Customer', N'Staff')) 

); 

GO 

 

/*========================================================= 

    2. KHACHHANG 

=========================================================*/ 

 

CREATE TABLE dbo.KHACHHANG 

( 

    MaKH INT IDENTITY(1,1) NOT NULL, 

    MaTK INT NOT NULL, 

    HoTen NVARCHAR(100) NOT NULL, 

    SDT VARCHAR(20) NULL, 

    DiaChi NVARCHAR(255) NULL, 

    NgayDangKy DATETIME NOT NULL CONSTRAINT DF_KHACHHANG_NgayDangKy DEFAULT GETDATE(), 

 

    CONSTRAINT PK_KHACHHANG PRIMARY KEY (MaKH), 

    CONSTRAINT UQ_KHACHHANG_MaTK UNIQUE (MaTK), 

    CONSTRAINT FK_KHACHHANG_TAIKHOAN FOREIGN KEY (MaTK) 

        REFERENCES dbo.TAIKHOAN(MaTK) 

); 

GO 

 

/*========================================================= 

    3. DANHMUC 

=========================================================*/ 

 

CREATE TABLE dbo.DANHMUC 

( 

    MaDM INT IDENTITY(1,1) NOT NULL, 

    TenDM NVARCHAR(100) NOT NULL, 

    MoTa NVARCHAR(500) NULL, 

    HinhAnh NVARCHAR(255) NULL, 

    Slug VARCHAR(200) NOT NULL, 

    MetaTitle NVARCHAR(255) NULL, 

    MetaDescription NVARCHAR(500) NULL, 

    MetaKeyword NVARCHAR(255) NULL, 

    TrangThai BIT NOT NULL CONSTRAINT DF_DANHMUC_TrangThai DEFAULT 1, 

    NgayTao DATETIME NOT NULL CONSTRAINT DF_DANHMUC_NgayTao DEFAULT GETDATE(), 

 

    CONSTRAINT PK_DANHMUC PRIMARY KEY (MaDM), 

    CONSTRAINT UQ_DANHMUC_Slug UNIQUE (Slug) 

); 

GO 

 

/*========================================================= 

    4. TIN_TUC 

=========================================================*/ 

 

CREATE TABLE dbo.TIN_TUC 

( 

    MaTin INT IDENTITY(1,1) NOT NULL, 

    MaTK INT NOT NULL, 

    TieuDe NVARCHAR(255) NOT NULL, 

    NoiDung NVARCHAR(MAX) NULL, 

    HinhAnh NVARCHAR(255) NULL, 

    Slug VARCHAR(250) NOT NULL, 

    MetaTitle NVARCHAR(255) NULL, 

    MetaDescription NVARCHAR(500) NULL, 

    MetaKeywords NVARCHAR(255) NULL, 

    TrangThai BIT NOT NULL CONSTRAINT DF_TIN_TUC_TrangThai DEFAULT 1, 

    NgayDang DATETIME NOT NULL CONSTRAINT DF_TIN_TUC_NgayDang DEFAULT GETDATE(), 

 

    CONSTRAINT PK_TIN_TUC PRIMARY KEY (MaTin), 

    CONSTRAINT UQ_TIN_TUC_Slug UNIQUE (Slug), 

    CONSTRAINT FK_TIN_TUC_TAIKHOAN FOREIGN KEY (MaTK) 

        REFERENCES dbo.TAIKHOAN(MaTK) 

); 

GO 

 

/*========================================================= 

    5. SANPHAM 

=========================================================*/ 

 

CREATE TABLE dbo.SANPHAM 

( 

    MaSP INT IDENTITY(1,1) NOT NULL, 

    MaDM INT NOT NULL, 

    TenSP NVARCHAR(150) NOT NULL, 

    MoTa NVARCHAR(MAX) NULL, 

    GiaHienTai DECIMAL(18,2) NOT NULL, 

    SoLuongTon INT NOT NULL CONSTRAINT DF_SANPHAM_SoLuongTon DEFAULT 0, 

    HinhAnh NVARCHAR(255) NULL, 

    ThuongHieu NVARCHAR(100) NULL, 

    BaoHanh NVARCHAR(100) NULL, 

    VAT DECIMAL(5,2) NOT NULL CONSTRAINT DF_SANPHAM_VAT DEFAULT 0, 

    Slug VARCHAR(250) NOT NULL, 

    MetaTitle NVARCHAR(255) NULL, 

    MetaDescription NVARCHAR(500) NULL, 

    MetaKeyword NVARCHAR(255) NULL, 

    NoiBat BIT NOT NULL CONSTRAINT DF_SANPHAM_NoiBat DEFAULT 0, 

    TrangThai BIT NOT NULL CONSTRAINT DF_SANPHAM_TrangThai DEFAULT 1, 

    NgayTao DATETIME NOT NULL CONSTRAINT DF_SANPHAM_NgayTao DEFAULT GETDATE(), 

 

    CONSTRAINT PK_SANPHAM PRIMARY KEY (MaSP), 

    CONSTRAINT UQ_SANPHAM_Slug UNIQUE (Slug), 

    CONSTRAINT FK_SANPHAM_DANHMUC FOREIGN KEY (MaDM) 

        REFERENCES dbo.DANHMUC(MaDM), 

    CONSTRAINT CK_SANPHAM_GiaHienTai CHECK (GiaHienTai >= 0), 

    CONSTRAINT CK_SANPHAM_SoLuongTon CHECK (SoLuongTon >= 0), 

    CONSTRAINT CK_SANPHAM_VAT CHECK (VAT >= 0 AND VAT <= 100) 

); 

GO 

 

/*========================================================= 

    6. GIA - Lịch sử / kế hoạch thay đổi giá 

=========================================================*/ 

 

CREATE TABLE dbo.GIA 

( 

    MaGia INT IDENTITY(1,1) NOT NULL, 

    MaSP INT NOT NULL, 

    GiaCu DECIMAL(18,2) NOT NULL, 

    GiaMoi DECIMAL(18,2) NOT NULL, 

    NgayBatDau DATE NOT NULL, 

    NgayKetThuc DATE NULL, 

    LyDoThayDoi NVARCHAR(500) NULL, 

    TrangThai NVARCHAR(50) NOT NULL CONSTRAINT DF_GIA_TrangThai DEFAULT N'Chờ áp dụng', 

    NgayTao DATETIME NOT NULL CONSTRAINT DF_GIA_NgayTao DEFAULT GETDATE(), 

 

    CONSTRAINT PK_GIA PRIMARY KEY (MaGia), 

    CONSTRAINT FK_GIA_SANPHAM FOREIGN KEY (MaSP) 

        REFERENCES dbo.SANPHAM(MaSP), 

    CONSTRAINT CK_GIA_GiaCu CHECK (GiaCu >= 0), 

    CONSTRAINT CK_GIA_GiaMoi CHECK (GiaMoi >= 0), 

    CONSTRAINT CK_GIA_Ngay CHECK (NgayKetThuc IS NULL OR NgayKetThuc >= NgayBatDau), 

    CONSTRAINT CK_GIA_TrangThai CHECK (TrangThai IN (N'Đang áp dụng', N'Chờ áp dụng', N'Ngừng áp dụng')) 

); 

GO 

 

/*========================================================= 

    7. GIOHANG 

=========================================================*/ 

 

CREATE TABLE dbo.GIOHANG 

( 

    MaGH INT IDENTITY(1,1) NOT NULL, 

    MaKH INT NOT NULL, 

    NgayTao DATETIME NOT NULL CONSTRAINT DF_GIOHANG_NgayTao DEFAULT GETDATE(), 

    NgayCapNhat DATETIME NULL, 

    TrangThai NVARCHAR(30) NOT NULL CONSTRAINT DF_GIOHANG_TrangThai DEFAULT N'Đang mở', 

 

    CONSTRAINT PK_GIOHANG PRIMARY KEY (MaGH), 

    CONSTRAINT FK_GIOHANG_KHACHHANG FOREIGN KEY (MaKH) 

        REFERENCES dbo.KHACHHANG(MaKH), 

    CONSTRAINT CK_GIOHANG_TrangThai CHECK (TrangThai IN (N'Đang mở', N'Đã đặt hàng', N'Đã hủy')) 

); 

GO 

 

/*========================================================= 

    8. CHITIET_GIOHANG 

=========================================================*/ 

 

CREATE TABLE dbo.CHITIET_GIOHANG 

( 

    MaGH INT NOT NULL, 

    MaSP INT NOT NULL, 

    SoLuong INT NOT NULL, 

    Gia DECIMAL(18,2) NOT NULL, 

 

    CONSTRAINT PK_CHITIET_GIOHANG PRIMARY KEY (MaGH, MaSP), 

    CONSTRAINT FK_CTGIOHANG_GIOHANG FOREIGN KEY (MaGH) 

        REFERENCES dbo.GIOHANG(MaGH), 

    CONSTRAINT FK_CTGIOHANG_SANPHAM FOREIGN KEY (MaSP) 

        REFERENCES dbo.SANPHAM(MaSP), 

    CONSTRAINT CK_CTGIOHANG_SoLuong CHECK (SoLuong > 0), 

    CONSTRAINT CK_CTGIOHANG_Gia CHECK (Gia >= 0) 

); 

GO 

 

/*========================================================= 

    9. DONHANG 

=========================================================*/ 

 

CREATE TABLE dbo.DONHANG 

( 

    MaDH INT IDENTITY(1,1) NOT NULL, 

    MaKH INT NOT NULL, 

    NgayDat DATETIME NOT NULL CONSTRAINT DF_DONHANG_NgayDat DEFAULT GETDATE(), 

    DiaChiGiaoHang NVARCHAR(255) NOT NULL, 

    GhiChu NVARCHAR(500) NULL, 

    TongTien DECIMAL(18,2) NOT NULL CONSTRAINT DF_DONHANG_TongTien DEFAULT 0, 

    TrangThai NVARCHAR(50) NOT NULL CONSTRAINT DF_DONHANG_TrangThai DEFAULT N'Chờ xác nhận', 

 

    CONSTRAINT PK_DONHANG PRIMARY KEY (MaDH), 

    CONSTRAINT FK_DONHANG_KHACHHANG FOREIGN KEY (MaKH) 

        REFERENCES dbo.KHACHHANG(MaKH), 

    CONSTRAINT CK_DONHANG_TongTien CHECK (TongTien >= 0), 

    CONSTRAINT CK_DONHANG_TrangThai CHECK 

    ( 

        TrangThai IN (N'Chờ xác nhận', N'Đã xác nhận', N'Đang giao', N'Hoàn thành', N'Đã hủy') 

    ) 

); 

GO 

 

/*========================================================= 

    10. CHITIET_DONHANG 

=========================================================*/ 

 

CREATE TABLE dbo.CHITIET_DONHANG 

( 

    MaCTDH INT IDENTITY(1,1) NOT NULL, 

    MaDH INT NOT NULL, 

    MaSP INT NOT NULL, 

    SoLuong INT NOT NULL, 

    GiaBan DECIMAL(18,2) NOT NULL, 

    ThanhTien DECIMAL(18,2) NOT NULL, 

 

    CONSTRAINT PK_CHITIET_DONHANG PRIMARY KEY (MaCTDH), 

    CONSTRAINT FK_CTDONHANG_DONHANG FOREIGN KEY (MaDH) 

        REFERENCES dbo.DONHANG(MaDH), 

    CONSTRAINT FK_CTDONHANG_SANPHAM FOREIGN KEY (MaSP) 

        REFERENCES dbo.SANPHAM(MaSP), 

    CONSTRAINT CK_CTDONHANG_SoLuong CHECK (SoLuong > 0), 

    CONSTRAINT CK_CTDONHANG_GiaBan CHECK (GiaBan >= 0), 

    CONSTRAINT CK_CTDONHANG_ThanhTien CHECK (ThanhTien >= 0) 

); 

GO 

 

/*========================================================= 

    11. THANHTOAN 

=========================================================*/ 

 

CREATE TABLE dbo.THANHTOAN 

( 

    MaTT INT IDENTITY(1,1) NOT NULL, 

    MaDH INT NOT NULL, 

    PhuongThuc NVARCHAR(50) NOT NULL, 

    SoTien DECIMAL(18,2) NOT NULL, 

    NgayThanhToan DATETIME NULL, 

    TrangThai NVARCHAR(50) NOT NULL CONSTRAINT DF_THANHTOAN_TrangThai DEFAULT N'Chưa thanh toán', 

 

    CONSTRAINT PK_THANHTOAN PRIMARY KEY (MaTT), 

    CONSTRAINT UQ_THANHTOAN_MaDH UNIQUE (MaDH), 

    CONSTRAINT FK_THANHTOAN_DONHANG FOREIGN KEY (MaDH) 

        REFERENCES dbo.DONHANG(MaDH), 

    CONSTRAINT CK_THANHTOAN_SoTien CHECK (SoTien >= 0), 

    CONSTRAINT CK_THANHTOAN_PhuongThuc CHECK (PhuongThuc IN (N'COD', N'Chuyển khoản', N'Ví điện tử', N'Thẻ ngân hàng')), 

    CONSTRAINT CK_THANHTOAN_TrangThai CHECK (TrangThai IN (N'Chưa thanh toán', N'Đã thanh toán', N'Thanh toán thất bại')) 

); 

GO 

 

/*========================================================= 

    12. VANCHUYEN 

=========================================================*/ 

 

CREATE TABLE dbo.VANCHUYEN 

( 

    MaVC INT IDENTITY(1,1) NOT NULL, 

    MaDH INT NOT NULL, 

    TenDonViVanChuyen NVARCHAR(100) NULL, 

    MaVanDon VARCHAR(100) NULL, 

    PhiVanChuyen DECIMAL(18,2) NOT NULL CONSTRAINT DF_VANCHUYEN_PhiVanChuyen DEFAULT 0, 

    PhiCongKenh DECIMAL(18,2) NOT NULL CONSTRAINT DF_VANCHUYEN_PhiCongKenh DEFAULT 0, 

    PhiLapRap DECIMAL(18,2) NOT NULL CONSTRAINT DF_VANCHUYEN_PhiLapRap DEFAULT 0, 

    TongPhiVanChuyen DECIMAL(18,2) NOT NULL CONSTRAINT DF_VANCHUYEN_TongPhi DEFAULT 0, 

    NgayGiaoDuKien DATE NULL, 

    NgayGiaoThucTe DATE NULL, 

    TrangThai NVARCHAR(50) NOT NULL CONSTRAINT DF_VANCHUYEN_TrangThai DEFAULT N'Chờ xử lý', 

 

    CONSTRAINT PK_VANCHUYEN PRIMARY KEY (MaVC), 

    CONSTRAINT UQ_VANCHUYEN_MaDH UNIQUE (MaDH), 

    CONSTRAINT FK_VANCHUYEN_DONHANG FOREIGN KEY (MaDH) 

        REFERENCES dbo.DONHANG(MaDH), 

    CONSTRAINT CK_VANCHUYEN_Phi CHECK 

    ( 

        PhiVanChuyen >= 0 AND PhiCongKenh >= 0 AND PhiLapRap >= 0 AND TongPhiVanChuyen >= 0 

    ), 

    CONSTRAINT CK_VANCHUYEN_TrangThai CHECK 

    ( 

        TrangThai IN (N'Chờ xử lý', N'Đang giao', N'Đã giao', N'Giao thất bại', N'Đã hủy') 

    ) 

); 

GO 

 

/*========================================================= 

    13. DANHGIA 

=========================================================*/ 

 

CREATE TABLE dbo.DANHGIA 

( 

    MaDG INT IDENTITY(1,1) NOT NULL, 

    MaKH INT NOT NULL, 

    MaSP INT NOT NULL, 

    SoSao INT NOT NULL, 

    NoiDung NVARCHAR(1000) NULL, 

    NgayDanhGia DATETIME NOT NULL CONSTRAINT DF_DANHGIA_NgayDanhGia DEFAULT GETDATE(), 

    TrangThai BIT NOT NULL CONSTRAINT DF_DANHGIA_TrangThai DEFAULT 1, 

 

    CONSTRAINT PK_DANHGIA PRIMARY KEY (MaDG), 

    CONSTRAINT FK_DANHGIA_KHACHHANG FOREIGN KEY (MaKH) 

        REFERENCES dbo.KHACHHANG(MaKH), 

    CONSTRAINT FK_DANHGIA_SANPHAM FOREIGN KEY (MaSP) 

        REFERENCES dbo.SANPHAM(MaSP), 

    CONSTRAINT CK_DANHGIA_SoSao CHECK (SoSao BETWEEN 1 AND 5) 

); 

GO 

 

/*========================================================= 

    INDEXES 

=========================================================*/ 

 

CREATE INDEX IX_KHACHHANG_MaTK ON dbo.KHACHHANG(MaTK); 

CREATE INDEX IX_TIN_TUC_MaTK ON dbo.TIN_TUC(MaTK); 

CREATE INDEX IX_SANPHAM_MaDM ON dbo.SANPHAM(MaDM); 

CREATE INDEX IX_GIA_MaSP ON dbo.GIA(MaSP); 

CREATE INDEX IX_GIOHANG_MaKH ON dbo.GIOHANG(MaKH); 

CREATE INDEX IX_CHITIET_GIOHANG_MaSP ON dbo.CHITIET_GIOHANG(MaSP); 

CREATE INDEX IX_DONHANG_MaKH ON dbo.DONHANG(MaKH); 

CREATE INDEX IX_CHITIET_DONHANG_MaDH ON dbo.CHITIET_DONHANG(MaDH); 

CREATE INDEX IX_CHITIET_DONHANG_MaSP ON dbo.CHITIET_DONHANG(MaSP); 

CREATE INDEX IX_DANHGIA_MaSP ON dbo.DANHGIA(MaSP); 

GO 

 

/*========================================================= 

    TEST SELECT 

=========================================================*/ 

 

SELECT * FROM dbo.DANHMUC; 

SELECT * FROM dbo.SANPHAM; 

SELECT * FROM dbo.GIA; 

SELECT * FROM dbo.DONHANG; 

GO 

 

/*========================================================= 

    END SCRIPT 

=========================================================*/ 

 