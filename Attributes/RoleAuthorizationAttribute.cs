using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HeatmapSystem.Attributes
{
    /// <summary>
    /// Attribute để kiểm tra quyền Admin/User
    /// </summary>
    public class RoleAuthorizationAttribute : ActionFilterAttribute
    {
        private readonly bool _requireAdmin;

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
                context.Result = new RedirectToActionResult("DangNhap", "Account", null);
                return;
            }

            // Kiểm tra IsAdmin
            var isAdmin = httpContext.Session.GetString("IsAdmin");
            
            if (isAdmin == null)
            {
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

    /// <summary>
    /// Attribute cho User thường (IsAdmin = false)
    /// </summary>
    public class UserOnlyAttribute : RoleAuthorizationAttribute
    {
        public UserOnlyAttribute() : base(false) { }
    }

    /// <summary>
    /// Attribute cho Admin (IsAdmin = true)
    /// </summary>
    public class AdminOnlyAttribute : RoleAuthorizationAttribute
    {
        public AdminOnlyAttribute() : base(true) { }
    }

    /// <summary>
    /// Attribute để kiểm tra Permission (Read/Update)
    /// Chỉ áp dụng cho User thường, Admin không bị giới hạn
    /// </summary>
    public class RequirePermissionAttribute : ActionFilterAttribute
    {
        private readonly string _requiredPermission; // "Read" hoặc "Update"

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="requiredPermission">"Read" = cần quyền Read hoặc Update, "Update" = cần quyền Update</param>
        public RequirePermissionAttribute(string requiredPermission)
        {
            _requiredPermission = requiredPermission;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            
            // Kiểm tra đã đăng nhập chưa
            var svnCode = httpContext.Session.GetString("SVNCode");
            if (string.IsNullOrEmpty(svnCode))
            {
                context.Result = new RedirectToActionResult("DangNhap", "Account", null);
                return;
            }

            // Lấy thông tin IsAdmin và Permission
            var isAdmin = httpContext.Session.GetString("IsAdmin") == "true";
            var permission = httpContext.Session.GetString("Permission") ?? "None";

            // Admin thì được làm tất cả
            if (isAdmin)
            {
                base.OnActionExecuting(context);
                return;
            }

            // Kiểm tra Permission cho User thường
            bool hasPermission = false;

            if (_requiredPermission == "Read")
            {
                // Cần quyền Read -> cho phép Read hoặc Update
                hasPermission = permission == "Read" || permission == "Update";
            }
            else if (_requiredPermission == "Update")
            {
                // Cần quyền Update -> chỉ cho phép Update
                hasPermission = permission == "Update";
            }

            if (!hasPermission)
            {
                // Không có quyền -> trả về JSON error hoặc redirect với message
                httpContext.Session.SetString("PermissionError", "Bạn không có quyền thực hiện thao tác này!");
                
                // Nếu là AJAX request -> trả JSON
                if (httpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    context.Result = new JsonResult(new 
                    { 
                        success = false, 
                        message = "Bạn không có quyền thực hiện thao tác này!" 
                    })
                    {
                        StatusCode = 403
                    };
                    return;
                }

                // Nếu không phải AJAX -> redirect về trang trước với message
                var referer = httpContext.Request.Headers["Referer"].ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    context.Result = new RedirectResult(referer);
                }
                else
                {
                    context.Result = new RedirectToActionResult("Home", "Heatmap", null);
                }
                return;
            }

            base.OnActionExecuting(context);
        }
    }

    /// <summary>
    /// Shorthand: Yêu cầu quyền Read (hoặc Update)
    /// </summary>
    public class RequireReadAttribute : RequirePermissionAttribute
    {
        public RequireReadAttribute() : base("Read") { }
    }

    /// <summary>
    /// Shorthand: Yêu cầu quyền Update (full quyền)
    /// </summary>
    public class RequireUpdateAttribute : RequirePermissionAttribute
    {
        public RequireUpdateAttribute() : base("Update") { }
    }
}