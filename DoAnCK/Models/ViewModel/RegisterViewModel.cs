using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DoAnCK.Models.ViewModel
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Họ tên")]
        [StringLength(100, ErrorMessage = "Họ tên không vượt quá 100 ký tự")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(150, ErrorMessage = "Email không vượt quá 150 ký tự")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không quá 20 ký tự")]
        public string SDT { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Địa chỉ")]
        [StringLength(255, ErrorMessage = "Địa chỉ không vượt quá 255 ký tự")]
        public string DiaChi { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận Mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không trùng khớp")]
        public string XacNhanMatKhau { get; set; }
    }
}