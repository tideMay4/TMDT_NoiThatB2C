using System.Linq;
using System.Web.Mvc;
using DoAnCK.Models;

namespace DoAnCK.Areas.Admin.Controllers
{
    // Đổi tên Class bỏ chữ Admin đi
    public class KhachHangController : Controller
    {
        DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        public ActionResult Index()
        {
            var khachHangs = db.KHACHHANGs.OrderByDescending(k => k.MaKH).ToList();
            return View(khachHangs);
        }
        // 1. XEM CHI TIẾT
        public ActionResult Details(int id)
        {
            var khachHang = db.KHACHHANGs.Find(id);
            if (khachHang == null) return HttpNotFound();
            return View(khachHang);
        }

        // 2. HIỂN THỊ GIAO DIỆN SỬA
        public ActionResult Edit(int id)
        {
            var khachHang = db.KHACHHANGs.Find(id);
            if (khachHang == null) return HttpNotFound();
            return View(khachHang);
        }

        // 3. LƯU DỮ LIỆU KHI BẤM SỬA
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(KHACHHANG model)
        {
            if (ModelState.IsValid)
            {
                var khachHang = db.KHACHHANGs.Find(model.MaKH);
                khachHang.HoTen = model.HoTen;
                khachHang.SDT = model.SDT;
                khachHang.DiaChi = model.DiaChi;
                // Thêm các trường khác của KHACHHANG nếu có
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // 4. XỬ LÝ XÓA
        public ActionResult Delete(int id)
        {
            var khachHang = db.KHACHHANGs.Find(id);
            if (khachHang != null)
            {
                db.KHACHHANGs.Remove(khachHang);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}