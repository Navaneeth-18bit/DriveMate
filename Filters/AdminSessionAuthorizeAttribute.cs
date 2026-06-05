using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace adminPage.Filters
{
    public class AdminSessionAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Allow anonymous actions (e.g. Register/Login) to bypass this filter
            var endpoint = context.HttpContext.GetEndpoint();
            if (endpoint != null && endpoint.Metadata.Any(m => m is AllowAnonymousAttribute))
            {
                base.OnActionExecuting(context);
                return;
            }

            var isLoggedIn = context.HttpContext.Session.GetString("AdminLoggedIn");

            if (isLoggedIn != "true")
            {
                context.Result = new RedirectToActionResult("Login", "Admin", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
