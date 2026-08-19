using System.Web.Mvc;

namespace DoAnCK.Areas.Customer
{
    public class CustomerAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Customer";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                name: "Customer_default",
                url: "Customer/{controller}/{action}/{id}",
                defaults: new
                {
                    controller = "Home",
                    action = "Index",
                    id = UrlParameter.Optional
                },
                namespaces: new[]
                {
                    "DoAnCK.Areas.Customer.Controllers"
                }
            );
        }
    }
}