using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DoAnCK.Models; // Đảm bảo đúng namespace Models chứa DoAnNoiThatB2CEntities của bạn

namespace DoAnCK.Areas.Admin.Controllers
{
    public class CuaHangController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        // GET: Admin/CuaHang
        public ActionResult Index(string searchString, int? statusFilter)
        {
            // 1. Lấy toàn bộ danh sách cửa hàng động từ SQL Server
            // (Nếu bảng trong DB của bạn tên khác, hãy đổi db.CUAHANGs thành db.TenBangCuaBạn)
            var query = db.CUAHANGs.AsQueryable();

            // 2. Xử lý chức năng Tìm kiếm Động thực tế
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.Trim().ToLower();
                query = query.Where(c => c.TenCH.ToLower().Contains(searchString) ||
                                         c.DiaChi.ToLower().Contains(searchString));
                ViewBag.CurrentSearch = searchString;
            }

            // 3. Xử lý Bộ lọc Trạng thái Động thực tế
            if (statusFilter.HasValue)
            {
                query = query.Where(c => c.TrangThai == statusFilter.Value);
                ViewBag.CurrentStatus = statusFilter.Value;
            }

            var danhSachCuaHang = query.OrderBy(c => c.MaCH).ToList();

            // 4. Tính toán số liệu CHUẨN ĐỘNG 100% từ Database cho 4 thẻ thống kê ở trên
            var tatCaCuaHang = db.CUAHANGs.ToList();
            ViewBag.TongCuaHang = tatCaCuaHang.Count;
            ViewBag.DangHoatDong = tatCaCuaHang.Count(c => c.TrangThai == 1);
            ViewBag.SapKhaiTruong = tatCaCuaHang.Count(c => c.TrangThai == 2); // Giả định trạng thái số 2 là sắp khai trương
            ViewBag.TamDong = tatCaCuaHang.Count(c => c.TrangThai == 0);

            return View(danhSachCuaHang);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        // 1. XEM CHI TIẾT
        public ActionResult Details(int id)
        {
            var cuaHang = db.CUAHANGs.Find(id);
            if (cuaHang == null) return HttpNotFound();
            return View(cuaHang);
        }

        // 2. HIỂN THỊ GIAO DIỆN SỬA
        public ActionResult Edit(int id)
        {
            var cuaHang = db.CUAHANGs.Find(id);
            if (cuaHang == null) return HttpNotFound();
            return View(cuaHang);
        }

        // 3. LƯU DỮ LIỆU KHI BẤM SỬA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CUAHANG model)
        {
            if (ModelState.IsValid)
            {
                var cuaHang = db.CUAHANGs.Find(model.MaCH);
                cuaHang.TenCH = model.TenCH;
                cuaHang.DienThoai = model.DienThoai;
                cuaHang.DiaChi = model.DiaChi;
                cuaHang.TrangThai = model.TrangThai;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // 4. XỬ LÝ XÓA
        public ActionResult Delete(int id)
        {
            var cuaHang = db.CUAHANGs.Find(id);
            if (cuaHang != null)
            {
                db.CUAHANGs.Remove(cuaHang);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}