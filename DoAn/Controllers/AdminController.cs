using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DoAn.Models;

namespace DoAn.Controllers
{
    public class AdminController : Controller
    {
        

        private GymCenterDBEntities1 db = new GymCenterDBEntities1();
        private bool IsAdmin()
        {
            return Session["UserId"] != null &&
                   Session["Role"] != null &&
                   Session["Role"].ToString() == "Admin";
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!IsAdmin())
            {
                filterContext.Result = new RedirectResult("/Account/Login");
                return;
            }

            base.OnActionExecuting(filterContext);
        }

        // ================= 1. Dashboard =================
        public ActionResult Dashboard()
        {
            ViewBag.TotalMembers = db.Users.Count(u => u.Role == "Member");
            ViewBag.TotalTrainers = db.Users.Count(u => u.Role == "Trainer");
            ViewBag.TotalClasses = db.GymClass.Count();
            ViewBag.TotalBookings = db.Booking.Count();
            return View();
        }

        // ================= 2. Quản lý GymClass =================
        public ActionResult Classes()
        {
            var classes = db.GymClass.OrderByDescending(c => c.CreatedAt).ToList();
            return View(classes);
        }

        [HttpGet]
        public ActionResult CreateClass()
        {
            ViewBag.Trainers = db.Users
                .Where(u => u.Role == "Trainer" && u.IsActive == true)
                .ToList();

            return View(new CreateClassViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateClass(CreateClassViewModel model, int trainerId)
        {
            ViewBag.Trainers = db.Users
                .Where(u => u.Role == "Trainer" && u.IsActive == true)
                .ToList();

            if (!ModelState.IsValid)
                return View(model);

            var trainer = db.Users.FirstOrDefault(u => u.UserId == trainerId && u.Role == "Trainer" && u.IsActive == true);
            if (trainer == null)
            {
                ModelState.AddModelError("", "Trainer không hợp lệ hoặc đã bị vô hiệu hóa.");
                return View(model);
            }

            if (model.TimeFrom == default(TimeSpan) || model.TimeTo == default(TimeSpan))
            {
                ModelState.AddModelError("", "Vui lòng nhập giờ bắt đầu và kết thúc.");
                return View(model);
            }
            if (model.TimeFrom >= model.TimeTo)
            {
                ModelState.AddModelError("", "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc.");
                return View(model);
            }

            HashSet<int> newDays;
            try
            {
                newDays = model.RecurrenceDays?
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.Parse(s.Trim()))
                    .ToHashSet() ?? new HashSet<int>();
            }
            catch
            {
                ModelState.AddModelError("", "RecurrenceDays không hợp lệ.");
                return View(model);
            }

            var existingClasses = db.GymClass
                .Where(c => c.TrainerId == trainerId && c.IsActive == true)
                .ToList();

            foreach (var c in existingClasses)
            {
                if (string.IsNullOrWhiteSpace(c.RecurrenceDays))
                    continue;

                HashSet<int> existDays;
                try
                {
                    existDays = c.RecurrenceDays
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => int.Parse(s.Trim()))
                        .ToHashSet();
                }
                catch
                {
                    continue;
                }

                existDays.IntersectWith(newDays);
                if (existDays.Count == 0)
                    continue;

                if (!c.TimeFrom.HasValue || !c.TimeTo.HasValue)
                    continue; 

                var newStart = model.TimeFrom;
                var newEnd = model.TimeTo;
                var exStart = c.TimeFrom.Value;
                var exEnd = c.TimeTo.Value;

                bool isOverlap = (newStart < exEnd) && (exStart < newEnd);
                if (isOverlap)
                {
                    ModelState.AddModelError("", $"Xung đột lịch với lớp '{c.Name}' (ID {c.ClassId}) — cùng ngày lặp và giờ bị chồng.");
                    return View(model);
                }
            }

            model.DurationMinutes = (int)(model.TimeTo - model.TimeFrom).TotalMinutes;
            if (model.DurationMinutes < 60)
            {
                ModelState.AddModelError("", "Thời lượng lớp phải từ 60 phút trở lên.");
                return View(model);
            }

            var gymClass = new GymClass
            {
                Name = model.Name,
                ClassType = "Group",
                TrainerId = trainerId,
                Description = model.Description,
                DurationMinutes = model.DurationMinutes,
                Capacity = model.Capacity,
                IsFeatured = model.IsFeatured,
                IsActive = true, 
                CreatedAt = DateTime.UtcNow,
                TimeFrom = model.TimeFrom,
                TimeTo = model.TimeTo,
                RecurrenceDays = model.RecurrenceDays
            };

            db.GymClass.Add(gymClass);
            db.SaveChanges();

            TempData["Success"] = "Tạo lớp thành công.";
            return RedirectToAction("Classes");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult OpenClass(int classId)
        {
            var gymClass = db.GymClass.Find(classId);
            if (gymClass == null) return HttpNotFound();

            gymClass.IsActive = true;
            db.SaveChanges();

            TempData["Success"] = "Lớp đã được mở lại!";
            return RedirectToAction("Classes");
        }


        [HttpGet]
        public ActionResult EditClass(int classId)
        {
            var gymClass = db.GymClass.Find(classId);
            if (gymClass == null) return HttpNotFound();

            ViewBag.Trainers = db.Users.Where(u => u.Role == "Trainer" && u.IsActive == true).ToList();
            return View(gymClass);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditClass(GymClass model, int trainerId)
        {
            var gymClass = db.GymClass.Find(model.ClassId);
            if (gymClass == null) return HttpNotFound();

            // So sánh TimeFrom và TimeTo
            if (model.TimeFrom >= model.TimeTo)
            {
                ModelState.AddModelError("", "Thời gian bắt đầu phải nhỏ hơn thời gian kết thúc");
                ViewBag.Trainers = db.Users.Where(u => u.Role == "Trainer" && u.IsActive == true).ToList();
                return View(model);
            }

            // Cập nhật các trường
            gymClass.Name = model.Name;
            gymClass.Description = model.Description;
            gymClass.TimeFrom = model.TimeFrom;
            gymClass.TimeTo = model.TimeTo;
            gymClass.DurationMinutes = model.DurationMinutes;
            gymClass.Capacity = model.Capacity;
            gymClass.TrainerId = trainerId;
            gymClass.IsFeatured = model.IsFeatured;
            gymClass.IsActive = model.IsActive;
            gymClass.RecurrenceDays = model.RecurrenceDays; 

            db.SaveChanges();
            TempData["Success"] = "Cập nhật lớp thành công";
            return RedirectToAction("Classes");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteClass(int classId)
        {
            var gymClass = db.GymClass.Find(classId);
            if (gymClass == null) return HttpNotFound();

            gymClass.IsActive = false;
            db.SaveChanges();

            TempData["Success"] = "Đã vô hiệu hóa lớp ";
            return RedirectToAction("Classes");
        }

        // ================= 3. Quản lý Trainer =================
        public ActionResult Trainers()
        {
            var trainers = (from u in db.Users
                            join t in db.TrainerProfile
                            on u.UserId equals t.TrainerId into gj
                            from tf in gj.DefaultIfEmpty()
                            where u.Role == "Trainer"
                            select new TrainerProfileViewModel
                            {
                                UserId = u.UserId,
                                Username = u.Username,
                                Email = u.Email,
                                PhoneNumber = u.PhoneNumber,
                                FullName = tf.FullName,
                                Gender = tf.Gender,
                                BirthDate = tf.BirthDate,
                                Address = tf.Address,
                                Avatar = tf.Avatar,
                                Description = tf.Description,
                                Specialties = tf.Specialties,
                                ExperienceYears = tf.ExperienceYears
                            }).ToList();

            return View(trainers);
        }


        [HttpGet]
        public ActionResult EditTrainer(int trainerId)
        {
            var trainer = (from u in db.Users
                           join t in db.TrainerProfile
                           on u.UserId equals t.TrainerId into gj
                           from tf in gj.DefaultIfEmpty()
                           where u.UserId == trainerId
                           select new TrainerProfileViewModel
                           {
                               UserId = u.UserId,
                               Username = u.Username,
                               Email = u.Email,
                               PhoneNumber = u.PhoneNumber,
                               FullName = tf.FullName,
                               Gender = tf.Gender,
                               BirthDate = tf.BirthDate,
                               Address = tf.Address,
                               Avatar = tf.Avatar,
                               Description = tf.Description,
                               Specialties = tf.Specialties,
                               ExperienceYears = tf.ExperienceYears
                           }).FirstOrDefault();

            if (trainer == null) return HttpNotFound();

            return View(trainer);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTrainer(TrainerProfileViewModel model)
        {
            var trainer = db.Users.Find(model.UserId);
            if (trainer == null) return HttpNotFound();

            // Update from ViewModel → Users table
            trainer.Email = model.Email;
            trainer.PhoneNumber = model.PhoneNumber;


            // Trainer Profile
            var profile = db.TrainerProfile.Find(model.UserId);
            if (profile != null)
            {
                profile.FullName = model.FullName;
                profile.Gender = model.Gender;
                profile.BirthDate = model.BirthDate;
                profile.Address = model.Address;
                profile.Description = model.Description;
                profile.Specialties = model.Specialties;
                profile.ExperienceYears = model.ExperienceYears;
                profile.Avatar = model.Avatar;
            }

            db.SaveChanges();

            TempData["Success"] = "Cập nhật Trainer thành công";
            return RedirectToAction("Trainers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteTrainer(int trainerId)
        {
            var trainer = db.Users.Find(trainerId);
            if (trainer == null) return HttpNotFound();

            trainer.IsActive = false;
            db.SaveChanges();

            TempData["Success"] = "Đã vô hiệu hóa Trainer";
            return RedirectToAction("Trainers");
        }


        // ================= 4. Quản lý Member =================
        public ActionResult Members()
        {
            var members = db.Users.Where(u => u.Role == "Member").ToList();
            return View(members);
        }

        [HttpGet]
        public ActionResult EditMember(int memberId)
        {
            var member = db.Users.Find(memberId);
            if (member == null) return HttpNotFound();
            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditMember(Users model)
        {
            var member = db.Users.Find(model.UserId);
            if (member == null) return HttpNotFound();

            member.Email = model.Email;
            member.PhoneNumber = model.PhoneNumber;
            member.IsActive = model.IsActive;

            db.SaveChanges();
            TempData["Success"] = "Cập nhật Member thành công";
            return RedirectToAction("Members");
        }

        // ================= 5. Quản lý Booking =================
        public ActionResult Bookings()
        {
            var bookings = db.Booking.OrderByDescending(b => b.BookingTime).ToList();
            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateBookingStatus(int bookingId, string status)
        {
            var booking = db.Booking.Find(bookingId);
            if (booking == null) return HttpNotFound();

            if (new[] { "Pending", "Confirmed", "Cancelled" }.Contains(status))
            {
                booking.Status = status;
                db.SaveChanges();
                TempData["Success"] = "Cập nhật trạng thái booking thành công";
            }

            return RedirectToAction("Bookings");
        }


        // ================= 7. Xem Rating Trainer =================
        public ActionResult TrainerRatings()
        {
            var ratings = db.TrainerRating
                .Select(r => new TrainerRatingViewModel
                {
                    RatingId = r.RatingId,
                    TrainerUsername = r.Users.Username,
                    MemberUsername = r.Users1.Username,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            return View(ratings);
        }

        // ================= 8. Notification =================
        public ActionResult Notifications()
        {
            var notifications = db.Notification.OrderByDescending(n => n.CreatedAt).ToList();
            return View(notifications);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
