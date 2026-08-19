using DoAnCK.Helpers;
using DoAnCK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace DoAnCK.Filters
{
    public class JwtAuthorizeAttribute : AuthorizeAttribute
    {
        public string Roles { get; set; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var accessCookie = httpContext.Request.Cookies["accessToken"];
            var refreshCookie = httpContext.Request.Cookies["refreshToken"];

            if (accessCookie == null && refreshCookie == null)
                return false;

            ClaimsPrincipal principal = null;

            // 1. Kiểm tra AccessToken
            if (accessCookie != null)
            {
                principal = JwtHelper.GetPrincipalFromToken(accessCookie.Value);
            }

            // 2. Nếu AccessToken hết hạn -> Dùng RefreshToken để gia hạn tự động
            if (principal == null && refreshCookie != null)
            {
                principal = TryRefreshToken(httpContext, refreshCookie.Value);
            }

            if (principal == null)
                return false;

            // 3. Kiểm tra Phân quyền (Roles)
            if (!string.IsNullOrEmpty(Roles))
            {
                var userRole = principal.FindFirst(ClaimTypes.Role)?.Value;
                if (userRole == null || !Roles.Split(',').Contains(userRole))
                {
                    return false;
                }
            }

            // Gán thông tin User vào Context
            httpContext.User = principal;
            return true;
        }

        private ClaimsPrincipal TryRefreshToken(HttpContextBase httpContext, string refreshToken)
        {
            using (var db = new DoAnNoiThatB2CEntities())
            {
                var user = db.TAIKHOANs.FirstOrDefault(u => u.RefreshToken == refreshToken
                                                         && u.RefreshTokenExpiryTime > DateTime.Now
                                                         && u.TrangThai == true);

                if (user == null) return null;

                // Cấp AccessToken mới
                string newAccessToken = JwtHelper.GenerateAccessToken(user.MaTK, user.Email, user.VaiTro);

                var cookie = new HttpCookie("accessToken", newAccessToken)
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddMinutes(30),
                    Path = "/"
                };
                httpContext.Response.Cookies.Add(cookie);

                return JwtHelper.GetPrincipalFromToken(newAccessToken);
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            var accessCookie = httpContext.Request.Cookies["accessToken"];

            // 1. Chưa đăng nhập (Không có token/Token hết hạn) -> Đá về Login
            if (accessCookie == null)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                { "controller", "Account" },
                { "action", "Login" }
                    });
            }
            // 2. Đã đăng nhập nhưng Sai Role (VD: Khách hàng cố vào Admin) -> Trả về trang Cấm truy cập 403
            else
            {
                filterContext.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/AccessDenied.cshtml" // Hoặc RedirectToAction("AccessDenied", "Account")
                };
            }
        }
    }
}