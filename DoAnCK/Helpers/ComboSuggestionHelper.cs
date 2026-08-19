using DoAnCK.Models;
using DoAnCK.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DoAnCK.Helpers
{
    public class ComboSuggestionHelper
    {
        public static List<ComboSuggestionViewModel> GetInventoryComboSuggestions(DoAnNoiThatB2CEntities db)
        {
            int stockThreshold = 10;

            var products = db.Database.SqlQuery<ComboProductTempViewModel>(@"
                SELECT 
                    SP.MaSP,
                    SP.TenSP,
                    SP.HinhAnh,
                    SP.GiaHienTai,
                    SP.SoLuongTon,
                    DM.TenDM AS DanhMuc
                FROM SANPHAM SP
                INNER JOIN DANHMUC DM ON SP.MaDM = DM.MaDM
                WHERE 
                    SP.TrangThai = 1
                    AND SP.SoLuongTon >= @p0
            ", stockThreshold).ToList();

            var combos = new List<ComboSuggestionViewModel>();

            AddCombo(
                combos,
                products,
                "TABLE_ARMCHAIR",
                "Combo bàn + ghế",
                new List<string> { "Table" },
                new List<string> { "Armchair" },
                5
            );

            AddCombo(
                combos,
                products,
                "LIGHTING_MIRROR",
                "Combo đèn + gương",
                new List<string> { "Lighting" },
                new List<string> { "Mirror" },
                5
            );

            AddCombo(
                combos,
                products,
                "SOFA_TABLE",
                "Combo sofa + bàn",
                new List<string> { "Sofa", "Sopha" },
                new List<string> { "Table" },
                7
            );

            AddCombo(
                combos,
                products,
                "ARMCHAIR_BED",
                "Combo ghế thư giãn + giường",
                new List<string> { "Armchair" },
                new List<string> { "Bed" },
                8
            );

            return combos.Take(4).ToList();
        }

        private static void AddCombo(
            List<ComboSuggestionViewModel> combos,
            List<ComboProductTempViewModel> products,
            string comboCode,
            string comboName,
            List<string> category1Names,
            List<string> category2Names,
            decimal discountPercent)
        {
            var product1 = products
                .Where(x => category1Names.Any(c =>
                    string.Equals((x.DanhMuc ?? "").Trim(), c, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.SoLuongTon)
                .FirstOrDefault();

            var product2 = products
                .Where(x => category2Names.Any(c =>
                    string.Equals((x.DanhMuc ?? "").Trim(), c, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.SoLuongTon)
                .FirstOrDefault();

            if (product1 == null || product2 == null)
            {
                return;
            }

            combos.Add(new ComboSuggestionViewModel
            {
                ComboCode = comboCode,
                ComboName = comboName,

                Product1Id = product1.MaSP,
                Product1Name = product1.TenSP,
                Product1Image = string.IsNullOrWhiteSpace(product1.HinhAnh)
                    ? "~/Content/images/no-image.jpg"
                    : product1.HinhAnh,
                Product1Price = product1.GiaHienTai,
                Product1Stock = product1.SoLuongTon,

                Product2Id = product2.MaSP,
                Product2Name = product2.TenSP,
                Product2Image = string.IsNullOrWhiteSpace(product2.HinhAnh)
                    ? "~/Content/images/no-image.jpg"
                    : product2.HinhAnh,
                Product2Price = product2.GiaHienTai,
                Product2Stock = product2.SoLuongTon,

                DiscountPercent = discountPercent
            });
        }
    }

    public class ComboProductTempViewModel
    {
        public int MaSP { get; set; }

        public string TenSP { get; set; }

        public string HinhAnh { get; set; }

        public decimal GiaHienTai { get; set; }

        public int SoLuongTon { get; set; }

        public string DanhMuc { get; set; }
    }
}