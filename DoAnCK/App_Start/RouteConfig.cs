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