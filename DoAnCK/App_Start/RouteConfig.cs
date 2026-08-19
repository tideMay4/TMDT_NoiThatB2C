using System.Web.Mvc;
using System.Web.Routing;

namespace DoAnCK
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "AccountLogin",
                url: "Account/Login",
                defaults: new
                {
                    controller = "Account",
                    action = "Login"
                },
                namespaces: new[] { "DoAnCK.Controllers" }
            );

            // Gửi đánh giá sản phẩm
            routes.MapRoute(
                name: "ProductAddReview",
                url: "Product/AddReview",
                defaults: new
                {
                    controller = "Product",
                    action = "AddReview"
                },
                namespaces: new[] { "DoAnCK.Controllers" }
            );

            // FIX yêu thích sản phẩm
            routes.MapRoute(
                name: "ProductToggleFavorite",
                url: "Product/ToggleFavorite",
                defaults: new
                {
                    controller = "Product",
                    action = "ToggleFavorite"
                },
                namespaces: new[] { "DoAnCK.Controllers" }
            );

            routes.MapRoute(
                name: "ProductDetail",
                url: "san-pham/{slug}",
                defaults: new
                {
                    controller = "Product",
                    action = "Detail",
                    slug = UrlParameter.Optional
                },
                namespaces: new[] { "DoAnCK.Controllers" }
            );

            routes.MapRoute(
                name: "ProductByCategory",
                url: "Product/{slug}",
                defaults: new
                {
                    controller = "Product",
                    action = "Index",
                    slug = UrlParameter.Optional
                },
                namespaces: new[] { "DoAnCK.Controllers" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new
                {
                    controller = "Home",
                    action = "Index",
                    id = UrlParameter.Optional
                },
                namespaces: new[] { "DoAnCK.Controllers" }
            );
        }
    }
}