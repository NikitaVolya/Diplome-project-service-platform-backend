using BLL.Public;
using Domain.Common;
using System.Security.Claims;

namespace AdminPanel.Models;

public static class CabinetRole
{
    private const string Cookie = "vybir_side";

    private const string Executor = "executor";

    private const string Customer = "customer";

    public static bool CanExecute(ClaimsPrincipal user) => user.IsInRole(AppRoles.Executor);

    public static bool CanOrder(ClaimsPrincipal user) =>
        user.IsInRole(AppRoles.Customer)
        || user.IsInRole(AppRoles.Admin)
        || user.IsInRole(AppRoles.Moderator)
        || user.IsInRole(AppRoles.Support);

    public static bool CanSwitch(ClaimsPrincipal user) => CanExecute(user) && CanOrder(user);

    public static CabinetSide Current(HttpContext context)
    {
        var user = context.User;

        if (!CanExecute(user))
        {
            return CabinetSide.Customer;
        }

        if (!CanOrder(user))
        {
            return CabinetSide.Executor;
        }

        return context.Request.Cookies[Cookie] == Customer ? CabinetSide.Customer : CabinetSide.Executor;
    }

    public static bool IsExecutor(HttpContext context) => Current(context) == CabinetSide.Executor;

    public static void Remember(HttpContext context, CabinetSide side)
    {
        context.Response.Cookies.Append(
            Cookie,
            side == CabinetSide.Executor ? Executor : Customer,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
    }
}
