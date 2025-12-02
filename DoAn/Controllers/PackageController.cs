using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Transactions;
using System.Web.Mvc;
using DoAn.Models;

namespace DoAn.Controllers
{
 
    public class PackageController : Controller
    {
        private GymCenterDBEntities1 db = new GymCenterDBEntities1();

        // ================= 1. Xem tất cả gói (Member) =================


        public ActionResult Index()
        {
            
            var allPackages = db.Package
                                .OrderByDescending(p => p.PackageId)
                                .ToList();

            List<MemberPackage> myPackages = null;
            if (Session["UserId"] != null)
            {
                int memberId = Convert.ToInt32(Session["UserId"]);
              
                myPackages = db.MemberPackage
                               .Where(mp => mp.MemberId == memberId && mp.SessionsRemaining > 0)
                               .Include(mp => mp.Package)
                               .ToList();
            }

            var viewModel = new PackageIndexViewModel
            {
                AllPackages = allPackages, 
                MyPackages = myPackages
            };

            return View(viewModel);
        }


        // ================= 2. Đăng ký gói (Member) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Subscribe(int packageId)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Register", "Account");

            int memberId = (int)Session["UserId"];

            var package = db.Package.Find(packageId);
            if (package == null)
            {
                TempData["Error"] = "Gói tập không tồn tại.";
                return RedirectToAction("Index"); 
            }

            var memberPackage = db.MemberPackage
                                  .FirstOrDefault(mp => mp.MemberId == memberId && mp.PackageId == packageId);

            if (memberPackage != null)
            {
                memberPackage.SessionsRemaining += package.TotalSessions;
                memberPackage.CreatedAt = DateTime.Now;
                db.Entry(memberPackage).State = EntityState.Modified;
            }
            else
            {
                memberPackage = new MemberPackage
                {
                    MemberId = memberId,
                    PackageId = packageId,
                    SessionsRemaining = package.TotalSessions,
                    CreatedAt = DateTime.Now
                };
                db.MemberPackage.Add(memberPackage);
            }

            db.SaveChanges();

            TempData["Success"] = $"Bạn đã đăng ký gói tập '{package.Name}' thành công!";
            return RedirectToAction("Index"); 
        }


        // ================= 3. Xem chi tiết gói =================

        public ActionResult Details(int packageId)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int memberId = Convert.ToInt32(Session["UserId"]);

            var package = db.Package.Find(packageId);
            if (package == null)
                return HttpNotFound();

            var memberPackage = db.MemberPackage
                                  .Where(mp => mp.MemberId == memberId && mp.PackageId == packageId && mp.SessionsRemaining > 0)
                                  .OrderByDescending(mp => mp.MemberPackageId)
                                  .FirstOrDefault();

            var viewModel = new PackageViewModel
            {
                Package = package,
                MemberPackage = memberPackage
            };

            return View(viewModel);
        }


        // ================= 5. Xem lịch sử attendance (Member) =================
        public ActionResult AttendanceHistory()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int memberId = Convert.ToInt32(Session["UserId"]);

            var attendanceList = db.Attendance
                .Include(a => a.Booking)
                .Include(a => a.Booking.GymClass)
                .Include(a => a.PTSlot)
                .Where(a =>
                    (a.Booking != null && a.Booking.MemberId == memberId) 
                    || (a.SlotId != null && a.PTSlot != null && a.PTSlot.MemberId == memberId) 
                )
                .OrderByDescending(a => a.AttendanceTime)  
                .Take(20)
                .ToList();

            return View(attendanceList);
        }


        // ================= 6. Quản lý gói (Admin) =================
        private bool IsAdmin()
        {
            return Session["UserId"] != null &&
                   Session["Role"] != null &&
                   Session["Role"].ToString() == "Admin";
        }

        public ActionResult ManagePackages()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var packages = db.Package.OrderByDescending(p => p.PackageId).ToList();
            return View(packages);
        }

        [HttpGet]
        public ActionResult CreatePackage() {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            return View(); 
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreatePackage(Package model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid) return View(model);

            try
            {
                db.Package.Add(model);
                db.SaveChanges();
                TempData["Success"] = "Tạo gói tập thành công";
                return RedirectToAction("ManagePackages");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Tạo gói thất bại: " + ex.Message;
                return View(model);
            }
        }


        [HttpGet]
        public ActionResult EditPackage(int packageId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var package = db.Package.Find(packageId);
            if (package == null) return HttpNotFound();
            return View(package);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPackage(Package model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var package = db.Package.Find(model.PackageId);
            if (package == null) return HttpNotFound();

            package.Name = model.Name;
            package.PackageType = model.PackageType;
            package.TotalSessions = model.TotalSessions;
            package.IsFeatured = model.IsFeatured;

            try
            {
                db.SaveChanges();
                TempData["Success"] = "Cập nhật gói tập thành công";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Cập nhật thất bại: " + ex.Message;
            }

            return RedirectToAction("ManagePackages");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePackage(int packageId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Account");

            var package = db.Package.Include(p => p.MemberPackage).FirstOrDefault(p => p.PackageId == packageId);
            if (package == null) return HttpNotFound();

            if (package.MemberPackage.Any())
            {
                TempData["Error"] = "Không thể xóa gói còn Member đăng ký";
                return RedirectToAction("ManagePackages");
            }

            try
            {
                db.Package.Remove(package);
                db.SaveChanges();
                TempData["Success"] = "Xóa gói tập thành công";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Xóa thất bại: " + ex.Message;
            }

            return RedirectToAction("ManagePackages");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
