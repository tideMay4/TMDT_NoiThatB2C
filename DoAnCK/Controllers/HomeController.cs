using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using DoAnCK.Models;

namespace DoAnCK.Controllers
{
    public class HomeController : Controller
    {
        private DoAnNoiThatB2CEntities db = new DoAnNoiThatB2CEntities();

        public ActionResult Index()
        {
            var sanPhamMoi = db.SANPHAMs
                .Include(x => x.DANHMUC)
                .Where(x => x.TrangThai == true)
                .OrderByDescending(x => x.NgayTao)
                .ThenByDescending(x => x.MaSP)
                .Take(6)
                .ToList();

            return View(sanPhamMoi);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}