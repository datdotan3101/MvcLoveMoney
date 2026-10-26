using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MvcLoveMoney.Helpers
{
    // For the "logged out" pages (log in, sign up, forgot password...).
    // A logged-in user who reaches them (e.g. with the browser's Back button) is sent to the dashboard,
    // so the only way back to these pages is to log out.
    public class RedirectIfAuthenticatedAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }
}
