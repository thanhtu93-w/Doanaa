using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DoAn;
using DoAn.Models;

namespace DoAn.Controllers
{
    public class AccountController : Controller
    {
        private GymCenterDBEntities1 db = new GymCenterDBEntities1();

        // GET: Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        // POST: Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (db.Users.Any(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("", "Username đã tồn tại");
                    return View(model);
                }

                if (db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("", "Email đã tồn tại");
                    return View(model);
                }

                var user = new Users
                {
                    Username = model.Username,
                    PasswordHash = model.Password, // Lưu plain text cho đồ án nhỏ
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    Role = "Member",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                db.Users.Add(user);
                db.SaveChanges();

                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // GET: Login
        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.FirstOrDefault(u => u.Username == model.Username);

                if (user != null && user.IsActive == true &&
                    user.PasswordHash == model.Password)
                {
                    // ---------- CHỈ DÙNG SESSION ----------
                    Session["UserId"] = user.UserId;
                    Session["UserName"] = user.Username;
                    Session["Role"] = user.Role;
                    // -------------------------------------

                    // Điều hướng theo Role
                    if (user.Role == "Admin")
                        return RedirectToAction("Dashboard", "Admin");

                    if (user.Role == "Trainer")
                        return RedirectToAction("Profile", "Trainer");

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng hoặc tài khoản bị khóa!");
            }

            return View(model);
        }

        // GET: Logout

        public ActionResult Logout()
        {
            Session.RemoveAll();      // clear session
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login", "Account");
        }




        // GET: Profile
        public ActionResult Profile()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login");

            int userId = (int)Session["UserId"];

            var user = db.Users.Find(userId);
            var profile = db.MemberProfile.FirstOrDefault(p => p.MemberId == userId);

            if (user == null) return HttpNotFound();

            var model = new MemberProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FullName = profile?.FullName,
                Gender = profile?.Gender,
                BirthDate = profile?.BirthDate,
                Avatar = profile?.Avatar,
                Height = profile?.Height,
                Weight = profile?.Weight,
                Address = profile?.Address
            };

            return View(model);
        }

        // POST: Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(MemberProfileViewModel model, HttpPostedFileBase AvatarFile)
        {
            if (ModelState.IsValid)
            {
                var user = db.Users.Find(model.UserId);
                if (user == null) return HttpNotFound();

                // Cập nhật Users
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;

                // Cập nhật MemberProfile
                var profile = db.MemberProfile.FirstOrDefault(p => p.MemberId == model.UserId);
                if (profile == null)
                {
                    profile = new MemberProfile { MemberId = model.UserId };
                    db.MemberProfile.Add(profile);
                }

                profile.FullName = model.FullName;
                profile.Gender = model.Gender;
                profile.BirthDate = model.BirthDate;
                profile.Address = model.Address;
                profile.Height = model.Height;
                profile.Weight = model.Weight;

                // Xử lý upload avatar
                if (AvatarFile != null && AvatarFile.ContentLength > 0)
                {
                    var fileName = Guid.NewGuid() + System.IO.Path.GetExtension(AvatarFile.FileName);
                    var path = Server.MapPath("~/Content/Images/Avatars/" + fileName);
                    AvatarFile.SaveAs(path);
                    profile.Avatar = "/Content/Images/Avatars/" + fileName;
                }

                db.SaveChanges();
                TempData["Success"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }

            return View(model);
        }


        // GET: ChangePassword
        public ActionResult ChangePassword()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login");

            return View();
        }

        // POST: ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (Session["UserId"] == null)
                    return RedirectToAction("Login");

                int userId = (int)Session["UserId"];
                var user = db.Users.Find(userId);
                if (user == null) return HttpNotFound();

                if (user.PasswordHash != model.CurrentPassword)
                {
                    ModelState.AddModelError("", "Mật khẩu hiện tại không đúng");
                    return View(model);
                }

                user.PasswordHash = model.NewPassword;
                db.SaveChanges();

                TempData["Success"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile");
            }

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
