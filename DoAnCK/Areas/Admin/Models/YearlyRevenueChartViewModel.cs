using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnCK.Areas.Admin.Models
{
    public class YearlyRevenueChartViewModel
    {
        public int Nam { get; set; }
        public decimal TongDoanhThuCaNam { get; set; }
        public List<decimal> DoanhThu12Thang { get; set; }
    }
}