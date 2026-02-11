using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HeatmapSystem.Attributes
{

    // Attribute để kiểm tra quyền Admin/User

    public class RoleAuthorizationAttribute : ActionFilterAttribute
    {
        private readonly bool _requireAdmin;

 
        /// <param name="requireAdmin">true = yêu cầu Admin, false = yêu cầu User thường</param>
        public RoleAuthorizationAttribute(bool requireAdmin)
        {
            _requireAdmin = requireAdmin;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            
            // Kiểm tra đã đăng nhập chưa
            var svnCode = httpContext.Session.GetString("SVNCode");
            if (string.IsNullOrEmpty(svnCode))
            {
                // Chưa đăng nhập -> redirect về trang đăng nhập
                context.Result = new RedirectToActionResult("DangNhap", "Account", null);
                return;
            }

            // Kiểm tra IsAdmin
            var isAdmin = httpContext.Session.GetString("IsAdmin");
            
            if (isAdmin == null)
            {
                // Không có IsAdmin trong session -> đăng xuất
                httpContext.Session.Clear();
                context.Result = new RedirectToActionResult("DangNhap", "Account", null);
                return;
            }

            bool userIsAdmin = isAdmin == "true";

            // Kiểm tra quyền
            if (_requireAdmin && !userIsAdmin)
            {
                // Yêu cầu Admin nhưng user là User thường -> redirect về Heatmap
                context.Result = new RedirectToActionResult("Home", "Heatmap", null);
                return;
            }

            if (!_requireAdmin && userIsAdmin)
            {
                // Yêu cầu User thường nhưng user là Admin -> redirect về Admin
                context.Result = new RedirectToActionResult("Users", "Admin", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }


    // Attribute cho User thường (IsAdmin = false)

    public class UserOnlyAttribute : RoleAuthorizationAttribute
    {
        public UserOnlyAttribute() : base(false) { }
    }


    // Attribute cho Admin (IsAdmin = true)

    public class AdminOnlyAttribute : RoleAuthorizationAttribute
    {
        public AdminOnlyAttribute() : base(true) { }
    }
}
