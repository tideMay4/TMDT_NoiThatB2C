using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAnCK.Areas.Admin.Models
{
    public class ProductViewsChartViewModel
    {
        // Danh sách các nhãn ngày trong tuần (T2, T3, T4, T5, T6, T7, CN)
        public List<string> Labels { get; set; }

        // Dữ liệu lượt xem tuần này của toàn sàn
        public List<int> ViewsThisWeek { get; set; }

        // Dữ liệu lượt xem tuần trước của toàn sàn
        public List<int> ViewsLastWeek { get; set; }
    }
}