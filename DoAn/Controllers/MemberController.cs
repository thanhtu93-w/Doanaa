using System;
using System.Linq;
using System.Web.Mvc;
using DoAn.Models;
using System.Data.Entity;
using System.Transactions;
using System.Collections.Generic;

namespace DoAn.Controllers
{
    public class MemberController : Controller
    {
        private GymCenterDBEntities1 db = new GymCenterDBEntities1();





  
  

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BookClass(int classId)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int memberId = (int)Session["UserId"];


            var classObj = db.GymClass.Find(classId);
            if (classObj == null)
            {
                TempData["Error"] = "Lớp tập không tồn tại.";
                return RedirectToAction("Index");
            }


            int currentBookingCount = db.Booking
                .Count(b => b.ClassId == classId && b.Status != "Cancelled");

            if (currentBookingCount >= classObj.Capacity)
            {
                TempData["Error"] = "Lớp đã đủ học viên.";
                return RedirectToAction("ClassDetails", new { id = classId });
            }


            var booking = new Booking
            {
                MemberId = memberId,
                ClassId = classId,
                Status = "Pending", 
                BookingTime = DateTime.Now
            };

            db.Booking.Add(booking);
            db.SaveChanges();

            TempData["Success"] = $"Bạn đã đăng ký lớp '{classObj.Name}' thành công!";
            return RedirectToAction("ClassDetails", new { id = classId });
        }



        // ================= 4. Book PT 1-1 =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BookPT(int trainerId, int numSessions)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            string role = Session["Role"] as string;
            if (role != "Member")
            {
                TempData["Error"] = "Chỉ member mới được đăng ký PT 1-1.";
                return RedirectToAction("TrainerDetails", new { trainerId });
            }

            int memberId = (int)Session["UserId"];

            var trainer = db.TrainerProfile.FirstOrDefault(t => t.TrainerId == trainerId && t.IsAvailableForPT == true);
            if (trainer == null)
            {
                TempData["Error"] = "Trainer hiện không nhận PT 1-1";
                return RedirectToAction("TrainerDetails", new { trainerId });
            }

            var memberPackage = db.MemberPackage
                                  .Include(mp => mp.Package)
                                  .FirstOrDefault(mp => mp.MemberId == memberId &&
                                                        mp.Package.PackageType == "PT" &&
                                                        mp.SessionsRemaining > 0);
            if (memberPackage == null)
            {
                TempData["Error"] = "Bạn không có gói PT còn buổi để đăng ký.";
                return RedirectToAction("TrainerDetails", new { trainerId });
            }

            if (numSessions > memberPackage.SessionsRemaining)
            {
                TempData["Error"] = $"Số buổi tối đa bạn có thể đăng ký là {memberPackage.SessionsRemaining}.";
                return RedirectToAction("TrainerDetails", new { trainerId });
            }

            using (var scope = new TransactionScope())
            {
                for (int i = 0; i < numSessions; i++)
                {
                    DateTime slotDate = DateTime.UtcNow.Date.AddDays(i);

                    TimeSpan start = new TimeSpan(DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, 0);
                    TimeSpan end = start.Add(TimeSpan.FromHours(1));

                    var slot = new PTSlot
                    {
                        TrainerId = trainerId,
                        StartTime = start,
                        EndTime = end,
                        Status = "Confirmed",
                        MemberId = memberId,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    db.PTSlot.Add(slot);
                    db.SaveChanges();   // Quan trọng: cần SlotId

                    var booking = new Booking
                    {
                        MemberId = memberId,
                        SlotId = slot.SlotId,
                        Status = "Confirmed",
                        BookingTime = DateTime.UtcNow
                    };

                    db.Booking.Add(booking);
                    db.SaveChanges();
                }

                memberPackage.SessionsRemaining -= numSessions;
                if (memberPackage.SessionsRemaining <= 0)
                {
                    TempData["Warning"] = "Gói tập của bạn đã hết buổi.";
                }

                scope.Complete();
            }


            TempData["Success"] = $"Đăng ký PT 1-1 thành công ({numSessions} buổi).";
            return RedirectToAction("TrainerDetails", new { trainerId });
        }



        // ================= 5. Xem danh sách booking cá nhân =================
        public ActionResult MyBookings()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int memberId = (int)Session["UserId"];
            var bookings = db.Booking
                             .Include(b => b.GymClass)
                             .Include(b => b.PTSlot)
                             .Where(b => b.MemberId == memberId)
                             .ToList();

            var model = bookings.Select(b => new BookingViewModel
            {
                BookingId = b.BookingId,
                Status = b.Status,
                BookingTime = b.BookingTime,
                ClassName = b.ClassId != null ? b.GymClass.Name : "PT 1-1",
                TrainerName = b.ClassId != null
                    ? db.Users.FirstOrDefault(u => u.UserId == b.GymClass.TrainerId)?.Username
                    : db.Users.FirstOrDefault(u => u.UserId == b.PTSlot.TrainerId)?.Username
            }).ToList();

            return View(model);

        }

        // ================= 6. Hủy booking =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelBooking(int bookingId)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int memberId = (int)Session["UserId"];

            var booking = db.Booking.Find(bookingId);
            if (booking == null || booking.MemberId != memberId)
                return HttpNotFound();

            bool canCancel = true;
            string trainerUsername = "";
            DateTime classDateTime = DateTime.MinValue;

            // ============================
            //   HỦY BOOKING GROUP CLASS
            // ============================
            if (booking.ClassId != null)
            {
                var session = db.Session
                                .Where(s => s.ClassId == booking.ClassId)
                                .OrderBy(s => s.SessionDate)
                                .FirstOrDefault();

                if (session != null)
                {
                    classDateTime = session.StartTime;

                    if (classDateTime <= DateTime.Now)
                        canCancel = false;

                    var gymClass = db.GymClass.Find(booking.ClassId);
                    trainerUsername = db.Users.Find(gymClass.TrainerId)?.Username ?? "Trainer";
                }
            }
            // ============================
            //   HỦY BOOKING PT SLOT
            // ============================
            else if (booking.SlotId != null)
            {
                var ptSlot = db.PTSlot.Find(booking.SlotId);
                if (ptSlot != null)
                {
                    DateTime bookingDate = booking.BookingTime.Value.Date;

                    DateTime actualStart = bookingDate.Add(ptSlot.StartTime);

                    classDateTime = actualStart;

                    if (actualStart <= DateTime.Now)
                        canCancel = false;

                    trainerUsername = ptSlot.Users?.Username ?? "Trainer";
                }
            }

            if (!canCancel)
            {
                TempData["Error"] = "Không thể hủy booking khi buổi tập đã bắt đầu";
                return RedirectToAction("MyBookings");
            }

            booking.Status = "Cancelled";
            db.SaveChanges();

            if (!string.IsNullOrEmpty(trainerUsername))
            {
                int trainerId = booking.ClassId != null
                                ? db.GymClass.Find(booking.ClassId).TrainerId
                                : db.PTSlot.Find(booking.SlotId).TrainerId;

                db.Notification.Add(new Notification
                {
                    UserId = trainerId,
                    Message = $"Member {db.Users.Find(memberId).Username} đã hủy booking với {trainerUsername} lúc {DateTime.Now:dd/MM/yyyy HH:mm}.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                });

                db.SaveChanges();
            }

            TempData["Success"] = "Booking đã bị hủy";
            return RedirectToAction("MyBookings");
        }





        // ================= 7. Xem lịch học =================
        public ActionResult MySchedule()
        {
            int memberId = Convert.ToInt32(Session["UserId"]); 


            var groupSchedule = (from b in db.Booking
                                 join c in db.GymClass on b.ClassId equals c.ClassId
                                 join t in db.Users on c.TrainerId equals t.UserId
                                 where b.MemberId == memberId && b.ClassId != null
                                 select new
                                 {
                                     c.ClassId,
                                     c.Name,
                                     TrainerName = t.Username,
                                     c.RecurrenceDays,
                                     c.TimeFrom,
                                     c.TimeTo,
                                     c.IsActive,
                                     BookingStatus = b.Status
                                 }).ToList()
                                .Select(x => new ClassVM
                                {
                                    ClassId = x.ClassId,
                                    Name = x.Name,
                                    TrainerName = x.TrainerName,
                                    RecurrenceDays = x.RecurrenceDays,
                                    TimeFrom = x.TimeFrom,
                                    TimeTo = x.TimeTo,
                                    IsActive = x.IsActive== true,
                                    Status = x.BookingStatus
                                }).ToList();

            ViewBag.GroupSchedule = groupSchedule;

            var ptSchedule = (from s in db.PTSlot
                              join t in db.Users on s.TrainerId equals t.UserId
                              join b in db.Booking on s.SlotId equals b.SlotId into sb
                              from booking in sb.DefaultIfEmpty()
                              where s.MemberId == memberId || (booking != null && booking.MemberId == memberId)
                              select new ScheduleItemVM
                              {
                                  SlotId = s.SlotId,
                                  Trainer = t.Username,
                                  WeekDay = 0, 
                                  TimeFrom = s.StartTime,
                                  TimeTo = s.EndTime,
                                  Status = booking != null ? booking.Status : s.Status
                              }).ToList();

            ViewBag.PTSchedule = ptSchedule;

            return View();
        }




        // ================= 8. Đánh giá Trainer =================
        [HttpGet]
        public ActionResult RateTrainer(int trainerId)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            int memberId = (int)Session["UserId"];

            bool alreadyRated = db.TrainerRating.Any(r => r.TrainerId == trainerId && r.MemberId == memberId);
            if (alreadyRated)
            {
                TempData["Error"] = "Bạn đã đánh giá Trainer này rồi";
                return RedirectToAction("MyBookings");
            }

            var trainer = db.Users.Find(trainerId);
            if (trainer == null) return HttpNotFound();

            var model = new RateTrainerViewModel
            {
                TrainerId = trainerId,
                TrainerName = trainer.Username
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RateTrainer(RateTrainerViewModel model)
        {
            if (Session["UserId"] == null) return RedirectToAction("Login", "Account");
            int memberId = (int)Session["UserId"];

            bool alreadyRated = db.TrainerRating.Any(r => r.TrainerId == model.TrainerId && r.MemberId == memberId);
            if (alreadyRated)
            {
                TempData["Error"] = "Bạn đã đánh giá Trainer này rồi";
                return RedirectToAction("MyBookings");
            }

            var rating = new TrainerRating
            {
                TrainerId = model.TrainerId,
                MemberId = memberId,
                Rating = model.Rating,
                Comment = model.Comment,
                CreatedAt = DateTime.UtcNow
            };
            db.TrainerRating.Add(rating);
            db.SaveChanges();

            TempData["Success"] = "Đánh giá Trainer thành công";
            return RedirectToAction("MyBookings");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelPTBooking(int slotId)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Account");

            int memberId = (int)Session["UserId"];

            var slot = db.PTSlot.Find(slotId);
            if (slot == null || slot.MemberId != memberId)
                return HttpNotFound();

            slot.MemberId = null;
            slot.Status = "Available";
            db.SaveChanges();

            db.Notification.Add(new Notification
            {
                UserId = slot.TrainerId,
                Message = $"Member {db.Users.Find(memberId).Username} đã hủy PT slot ({slot.StartTime:hh\\:mm} - {slot.EndTime:hh\\:mm}).",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();

            TempData["Success"] = "PT slot đã được hủy";
            return RedirectToAction("MySchedule");
        }



        public class ScheduleItemVM
        {

            public string Trainer { get; set; }



            public TimeSpan? TimeFrom { get; set; }
            public TimeSpan? TimeTo { get; set; }

            public string Status { get; set; }
            public string RecurrenceDays { get; set; }

            public int WeekDay { get; set; }
          
            public int? SlotId { get; set; }
        }
        public class ClassVM
        {
            public int ClassId { get; set; }
            public string Name { get; set; }
            public string TrainerName { get; set; }
            public string RecurrenceDays { get; set; }
            public TimeSpan? TimeFrom { get; set; }
            public TimeSpan? TimeTo { get; set; }
            public bool IsActive { get; set; }
                
           public string Status { get; set; }
        }

    }
}
