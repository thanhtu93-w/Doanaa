using System;
using System.Linq;
using System.Web.Mvc;
using DoAn.Models;

namespace DoAn.Controllers
{
    
    public class NotificationController : Controller
    {
        private GymCenterDBEntities1 db = new GymCenterDBEntities1();

        // ================= 1. Xem danh sách thông báo (có phân trang) =================
        public ActionResult Index(int page = 1, int pageSize = 20)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int userId = Convert.ToInt32(Session["UserId"]);

            var notifications = db.Notification
                                  .Where(n => n.UserId == userId)
                                  .OrderByDescending(n => n.CreatedAt)
                                  .Skip((page - 1) * pageSize)
                                  .Take(pageSize)
                                  .ToList();

            return View(notifications);
        }

        // ================= 2. Đánh dấu đã đọc =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsRead(int notificationId)
        {
            if (Session["UserId"] == null)
                return Json(new { success = false, message = "Unauthorized" });

            int userId = Convert.ToInt32(Session["UserId"]);
            var notification = db.Notification.Find(notificationId);

            if (notification == null)
                return Json(new { success = false, message = "Notification not found" });

            if (notification.UserId != userId)
                return Json(new { success = false, message = "Forbidden" });

            notification.IsRead = true;
            db.SaveChanges();

            return Json(new { success = true });
        }

        // ================= 3. Xóa thông báo =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int notificationId)
        {
            if (Session["UserId"] == null)
                return Json(new { success = false, message = "Unauthorized" });

            int userId = Convert.ToInt32(Session["UserId"]);
            var notification = db.Notification.Find(notificationId);

            if (notification == null)
                return Json(new { success = false, message = "Notification not found" });

            if (notification.UserId != userId)
                return Json(new { success = false, message = "Forbidden" });

            db.Notification.Remove(notification);
            db.SaveChanges();

            return Json(new { success = true });
        }

        // ================= 4. Tạo thông báo (Admin) =================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(int userId, string message)
        {
            if (!db.Users.Any(u => u.UserId == userId))
            {
                ModelState.AddModelError("", "User không tồn tại");
                return View();
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                ModelState.AddModelError("", "Message không được để trống");
                return View();
            }

            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            db.Notification.Add(notification);
            db.SaveChanges();

            TempData["Success"] = "Tạo thông báo thành công";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
