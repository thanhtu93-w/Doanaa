using System;
using System.Linq;
using System.Web.Mvc;
using DoAn.Models;
using System.Data.Entity;
using System.Web;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static DoAn.Models.ViewModels;
using System.Data;
using System.Data.Common.CommandTrees.ExpressionBuilder;

namespace DoAn.Controllers
{

    public class TrainerController : Controller
    {
        private GymCenterDBEntities1 db = new GymCenterDBEntities1();




        // Helper lấy TrainerId từ Session
        private int GetTrainerId() {
            if (Session["UserId"] == null) throw new Exception("Session expired. Please login again."); 
            return Convert.ToInt32(Session["UserId"]); 
        }


        // ================= 1. Profile =================
        public ActionResult Profile()
        {
            int trainerId = GetTrainerId();

            var trainer = db.Users.Find(trainerId);
            var profile = db.TrainerProfile.FirstOrDefault(p => p.TrainerId == trainerId);

            if (trainer == null) return HttpNotFound();

            var model = new TrainerProfileViewModel
            {
                UserId = trainer.UserId,
                Username = trainer.Username,
                Email = trainer.Email,
                PhoneNumber = trainer.PhoneNumber,
                FullName = profile?.FullName,
                Gender = profile?.Gender,
                BirthDate = profile?.BirthDate,
                Address = profile?.Address,
                Avatar = profile?.Avatar,
                Description = profile?.Description,
                Specialties = profile?.Specialties,
                ExperienceYears = profile?.ExperienceYears
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(TrainerProfileViewModel model, HttpPostedFileBase AvatarFile)
        {
            if (!ModelState.IsValid)
                return View(model);

            int trainerId = GetTrainerId();

            var trainer = db.Users.Find(trainerId);
            var profile = db.TrainerProfile.FirstOrDefault(p => p.TrainerId == trainerId);

            if (trainer == null) return HttpNotFound();

            // Cập nhật Users
            trainer.Email = model.Email;
            trainer.PhoneNumber = model.PhoneNumber;

            // Cập nhật TrainerProfile
            if (profile == null)
            {
                profile = new TrainerProfile { TrainerId = trainerId };
                db.TrainerProfile.Add(profile);
            }

            profile.FullName = model.FullName;
            profile.Gender = model.Gender;
            profile.BirthDate = model.BirthDate;
            profile.Address = model.Address;
            profile.Description = model.Description;
            profile.Specialties = model.Specialties;
            profile.ExperienceYears = model.ExperienceYears;

            // Upload avatar
            if (AvatarFile != null && AvatarFile.ContentLength > 0)
            {
                var fileName = Guid.NewGuid() + System.IO.Path.GetExtension(AvatarFile.FileName);
                var path = Server.MapPath("~/Content/Images/Avatars/" + fileName);
                AvatarFile.SaveAs(path);
                profile.Avatar = "/Content/Images/Avatars/" + fileName;
            }

            db.SaveChanges();
            TempData["Success"] = "Cập nhật profile thành công!";
            return RedirectToAction("Profile");
        }


        // ================= 2. My Classes =================
        public ActionResult MyClasses()
        {
            int trainerId = GetTrainerId();
            var classes = db.GymClass
                            .Where(c => c.TrainerId == trainerId)
                            .OrderByDescending(c => c.CreatedAt)
                            .ToList();
            return View(classes);
        }

        // ================= 3. Class Members =================
        public ActionResult ClassMembers(int classId)
        {
            int trainerId = GetTrainerId();

            var gymClass = db.GymClass.Find(classId);
            if (gymClass == null || gymClass.TrainerId != trainerId)
                return HttpNotFound();

            var members = db.Booking
                            .Where(b => b.ClassId == classId && b.Status != "Cancelled")
                            .Select(b => new MemberProfileViewModel
                            {
                                UserId = b.Users.UserId,
                                Username = b.Users.Username,
                                Email = b.Users.Email,
                                PhoneNumber = b.Users.PhoneNumber,

                                FullName = b.Users.MemberProfile.FullName,
                                Gender = b.Users.MemberProfile.Gender,
                                BirthDate = b.Users.MemberProfile.BirthDate,
                                Avatar = b.Users.MemberProfile.Avatar,
                                Height = b.Users.MemberProfile.Height,
                                Weight = b.Users.MemberProfile.Weight,
                                Address = b.Users.MemberProfile.Address
                            })
                            .ToList();

            ViewBag.ClassName = gymClass.Name;

            return View(members);
        }


        // ================= 4. Create Group Class =================
        [HttpGet]
        public ActionResult CreateClass()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateClass(CreateClassViewModel model)
        {
            int trainerId = GetTrainerId();

            if (!ModelState.IsValid)
                return View(model);

            if (model.TimeFrom >= model.TimeTo)
            {
                ModelState.AddModelError("", "Giờ bắt đầu phải nhỏ hơn giờ kết thúc");
                return View(model);
            }

            model.DurationMinutes = (int)(model.TimeTo - model.TimeFrom).TotalMinutes;

            if (model.DurationMinutes < 60)
            {
                ModelState.AddModelError("", "Thời lượng lớp phải từ 60 phút trở lên");
                return View(model);
            }

            var gymClass = new GymClass
            {
                Name = model.Name,
                ClassType = "Group",
                TrainerId = trainerId,
                Description = model.Description,
                TimeFrom = model.TimeFrom,
                TimeTo = model.TimeTo,
                RecurrenceDays = model.RecurrenceDays,
                DurationMinutes = model.DurationMinutes,
                Capacity = model.Capacity,
                IsFeatured = model.IsFeatured,
                IsActive = false, // CHỜ ADMIN DUYỆT
                CreatedAt = DateTime.UtcNow
            };

            db.GymClass.Add(gymClass);
            db.SaveChanges();

            // GỬI THÔNG BÁO TỚI ADMIN
            var adminId = GetAdminId();
            if (adminId > 0)
            {
                db.Notification.Add(new Notification
                {
                    UserId = adminId,
                    Message = $"Trainer ID {trainerId} vừa đề xuất lớp mới: {model.Name}.",
                    IsRead = false
                });
                db.SaveChanges();
            }

            TempData["Success"] = "Lớp đã được đề xuất và đang chờ Admin duyệt";
            return RedirectToAction("MyClasses");
        }
        private int GetAdminId()
        {
            return db.Users
                .Where(u => u.Role == "Admin")
                .Select(u => u.UserId)
                .FirstOrDefault();
        }



        // ================= 5. Create PT Slot =================
        [HttpGet]
        public ActionResult CreatePTSlot()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreatePTSlot(PTSlotViewModel model)
        {
            int trainerId = GetTrainerId();

            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra khoảng cách giờ >= 1 giờ 30 phút
            var duration = model.EndHour - model.StartHour;
            if (duration.TotalMinutes < 90)
            {
                TempData["Error"] = "Khung giờ PT phải dài ít nhất 1 giờ 30 phút.";
                return View(model);
            }

            // Kiểm tra trùng khung giờ với các slot tuần hiện tại
            bool overlap = db.PTSlot.Any(s =>
                s.TrainerId == trainerId &&
                ((model.StartHour >= s.StartTime && model.StartHour < s.EndTime) ||
                 (model.EndHour > s.StartTime && model.EndHour <= s.EndTime) ||
                 (model.StartHour <= s.StartTime && model.EndHour >= s.EndTime))
            );

            if (overlap)
            {
                TempData["Error"] = "Khung giờ trùng với slot hiện tại!";
                return View(model);
            }

            // Tạo slot chỉ với khung giờ, không sinh ngày
            var slot = new PTSlot
            {
                TrainerId = trainerId,
                StartTime = model.StartHour,
                EndTime = model.EndHour,
                Status = "Available",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.PTSlot.Add(slot);

            try
            {
                db.SaveChanges();
                TempData["Success"] = "Đã tạo khung giờ PT cố định cho tuần!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi tạo slot: " + ex.Message;
                return View(model);
            }

            return RedirectToAction("MyPTSlots");
        }





        // ================= 6. My PT Slots =================
        public ActionResult MyPTSlots()
        {
            int trainerId = GetTrainerId();

            var slots = db.PTSlot
                .Where(s => s.TrainerId == trainerId)
                .OrderBy(s => s.StartTime)
                .Select(s => new PTSlotDisplayViewModel
                {
                    SlotId = s.SlotId,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Status = s.Status,
                    MemberId = s.MemberId, // gán vào đây
                    MemberUsername = s.MemberId != null ? s.Users.Username : null
                })
                .ToList();

            return View(slots);
        }






        // ================= 7. Confirm / Reject PT =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmPT(int slotId)
        {
            int trainerId = GetTrainerId();
            var slot = db.PTSlot.Find(slotId);
            if (slot == null || slot.TrainerId != trainerId) return HttpNotFound();
            if (slot.Status != "Pending") TempData["Error"] = "Slot không phải Pending";
            else slot.Status = "Confirmed";

            db.SaveChanges();
            return RedirectToAction("MyPTSlots");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectPT(int slotId)
        {
            int trainerId = GetTrainerId();
            var slot = db.PTSlot.Find(slotId);
            if (slot == null || slot.TrainerId != trainerId) return HttpNotFound();
            if (slot.Status != "Pending") TempData["Error"] = "Slot không phải Pending";
            else
            {
                slot.Status = "Rejected";
                slot.MemberId = null;
            }

            db.SaveChanges();
            return RedirectToAction("MyPTSlots");
        }

        // ================= 8. Attendance =================
        public ActionResult Attendance(int classId)
        {
            int trainerId = GetTrainerId(); 
            var gymClass = db.GymClass.Find(classId);
            if (gymClass == null || gymClass.TrainerId != trainerId) return HttpNotFound();

            var bookings = db.Booking
                             .Where(b => b.ClassId == classId && b.Status != "Cancelled")
                             .ToList();

            var recurrenceDays = new List<int>();
            if (!string.IsNullOrEmpty(gymClass.RecurrenceDays))
                recurrenceDays = gymClass.RecurrenceDays.Split(',').Select(int.Parse).ToList();

            var vm = new AttendanceViewModel
            {
                GymClass = gymClass,
                Bookings = bookings,
                RecurrenceDays = recurrenceDays
            };
            return View(vm);
        }

     
        public ActionResult AttendanceClassDetail(int classId, DateTime classDate)
        {
            var gymClass = db.GymClass.Find(classId);
            var bookings = db.Booking.Where(b => b.ClassId == classId && b.Status != "Cancelled").ToList();

            var vm = new AttendanceViewModel
            {
                GymClass = gymClass,
                Bookings = bookings,
                SelectedSessionDate = classDate
            };

            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveAttendanceList(int classId, FormCollection form)
        {
            int trainerId = (int)Session["UserId"];

            var bookings = db.Booking
                .Include(b => b.Users)
                .Include(b => b.Users.MemberPackage.Select(mp => mp.Package))
                .Where(b => b.ClassId == classId)
                .ToList();

            foreach (var b in bookings)
            {
                string statusKey = $"attendanceStatus[{b.BookingId}]";
                string status = form[statusKey];

                // Lấy attendance mới nhất nếu có (theo Booking)
                var attendance = b.Attendance
                    .OrderByDescending(a => a.AttendanceTime)
                    .FirstOrDefault();

                if (attendance == null || attendance.AttendanceTime.Date != DateTime.Today)
                {
                    // Tạo attendance mới
                    attendance = new Attendance
                    {
                        BookingId = b.BookingId,
                        Status = status,
                        AttendanceTime = DateTime.Now // lưu thời gian điểm danh hiện tại
                    };

                    db.Attendance.Add(attendance);
                }
                else
                {
                    // Cập nhật status nếu đã tồn tại attendance hôm nay
                    attendance.Status = status;
                    db.Entry(attendance).State = EntityState.Modified;
                }

                // Cập nhật SessionsRemaining cho Group Package
                if (status == "Present" || status == "Late")
                {
                    int needed = 1;

                    var groupPackages = db.MemberPackage
                        .Include(x => x.Package)
                        .Where(x => x.MemberId == b.MemberId &&
                                    x.SessionsRemaining > 0 &&
                                    x.Package.PackageType == "Group")
                        .OrderBy(x => x.CreatedAt)
                        .ToList();

                    foreach (var mp in groupPackages)
                    {
                        if (needed == 0) break;

                        if (mp.SessionsRemaining >= needed)
                        {
                            mp.SessionsRemaining -= needed;
                            needed = 0;
                        }
                        else
                        {
                            needed -= mp.SessionsRemaining;
                            mp.SessionsRemaining = 0;
                        }

                        db.Entry(mp).State = EntityState.Modified;

                        if (mp.SessionsRemaining == 0)
                            db.MemberPackage.Remove(mp);
                    }
                }

                // Giữ tối đa 20 bản ghi attendance gần nhất
                var history = db.Attendance
                                .Where(a => a.BookingId == b.BookingId)
                                .OrderByDescending(a => a.AttendanceTime)
                                .ToList();

                if (history.Count > 20)
                {
                    var toRemove = history.Skip(20).ToList();
                    foreach (var item in toRemove)
                    {
                        db.Attendance.Remove(item);
                    }
                }
            }

            db.SaveChanges();

            TempData["Success"] = "Đã lưu điểm danh thành công!";
            return RedirectToAction("AttendanceClassDetail", new { classId = classId });
        }




        public ActionResult _AttendanceHistory(int classId)
        {
            ViewBag.ClassId = classId;

            // Lấy 20 ngày điểm danh gần nhất
            var sessions = db.Attendance
                             .Where(a => a.Booking.ClassId == classId)
                             .GroupBy(a => a.AttendanceTime.Date) // nhóm theo ngày
                             .OrderByDescending(g => g.Key)
                             .Take(20)
                             .Select(g => g.Key)
                             .ToList();

            return PartialView("_AttendanceHistory", sessions);
        }
        public ActionResult _AttendanceHistoryDetail(int classId, DateTime attendanceDate)
        {
            var attendances = db.Attendance
                                .Where(a => a.Booking.ClassId == classId
                                         && a.AttendanceTime.Date == attendanceDate.Date)
                                .ToList();

            return PartialView("_AttendanceHistoryDetail", attendances);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckInPT(int slotId)
        {
            var slot = db.PTSlot.FirstOrDefault(s => s.SlotId == slotId);
            if (slot == null)
            {
                TempData["Error"] = "Không tìm thấy slot.";
                return RedirectToAction("Index");
            }

            if (slot.MemberId == null)
            {
                TempData["Error"] = "Slot chưa có hội viên đặt.";
                return RedirectToAction("ScheduleSlot", new { id = slotId });
            }

            int memberId = slot.MemberId.Value;

            var booking = db.Booking
                .FirstOrDefault(b => b.SlotId == slotId && b.MemberId == memberId);

            if (booking == null)
            {
                TempData["Error"] = "Không tìm thấy booking tương ứng.";
                return RedirectToAction("ScheduleSlot", new { id = slotId });
            }

            var todayAttendance = booking.Attendance
                .FirstOrDefault(a => a.AttendanceTime.Date == DateTime.Today);

            if (todayAttendance != null)
            {
                todayAttendance.Status = "Present"; 
                db.Entry(todayAttendance).State = EntityState.Modified;
            }
            else
            {
                var attendance = new Attendance
                {
                    BookingId = booking.BookingId,
                    SlotId = slotId,
                    Status = "Present",
                    AttendanceTime = DateTime.Now 
                };
                db.Attendance.Add(attendance);
            }


            if (slot.TotalSessions > 0)
                slot.TotalSessions -= 1;

            if (slot.TotalSessions == 0)
            {
                slot.MemberId = null;
                slot.Status = "Available";
            }

            db.SaveChanges();

            TempData["Success"] = "Điểm danh PT thành công!";
            return RedirectToAction("ScheduleSlot", new { id = slotId });
        }



        public ActionResult ScheduleSlot(int? slotId)
        {
            PTSlot slot;

            if (slotId.HasValue)
            {
                slot = db.PTSlot.Find(slotId.Value);
            }
            else
            {
                slot = db.PTSlot.FirstOrDefault();
            }

            if (slot == null)
                return HttpNotFound();

            var schedules = db.WeeklyPTSchedule
                      .Where(s => s.TrainerId == slot.TrainerId && s.SlotId == slot.SlotId)
                      .OrderBy(s => s.WeekDay)
                      .ToList();

            var vm = new ScheduleSlotVM
            {
                SlotId = slot.SlotId,
                TrainerId = slot.TrainerId,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime,
                ExistingSchedules = schedules
            };
            ModelState.Remove("WeekDay");
            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ScheduleSlot(Models.ViewModels.ScheduleSlotVM model)
        {
            
            var slot = db.PTSlot.Find(model.SlotId);
            if (slot == null)
                return HttpNotFound();

            var oldSchedules = db.WeeklyPTSchedule
                  .Where(s => s.TrainerId == slot.TrainerId)
                  .ToList();

            foreach (var s in oldSchedules)
            {
                db.WeeklyPTSchedule.Remove(s);
            }

            db.SaveChanges();

            Random rnd = new Random();
            var weekdays = new List<int> { 2, 3, 4, 5, 6, 7, 8 };
            var selectedDays = weekdays.OrderBy(x => rnd.Next()).Take(4).ToList();

            foreach (var day in selectedDays)
            {
                var schedule = new WeeklyPTSchedule
                {
                    SlotId = slot.SlotId,
                    TrainerId = slot.TrainerId,
                    WeekDay = day,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.WeeklyPTSchedule.Add(schedule);
            }

            db.SaveChanges();

            if (slot.MemberId.HasValue)
            {
                db.Notification.Add(new Notification
                {
                    UserId = slot.MemberId.Value,
                    Message = $"Trainer {slot.TrainerId} đã lên lịch mới: {string.Join(", ", selectedDays.Select(d => "Thứ " + (d == 8 ? "CN" : d.ToString())))}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                db.SaveChanges();
            }

            TempData["Success"] = "Đã tạo 4 buổi ngẫu nhiên trong tuần và gửi thông báo đến member!";
            return RedirectToAction("ScheduleSlot", new { slotId = slot.SlotId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSchedule(int scheduleId)
        {
            string key = "WeekDay_" + scheduleId;
            if (!int.TryParse(Request.Form[key], out int WeekDay))
            {
                TempData["Error"] = "Không có giá trị ngày trong tuần!";
                return RedirectToAction("ScheduleSlot");
            }

            var schedule = db.WeeklyPTSchedule.Find(scheduleId);
            if (schedule == null)
                return HttpNotFound();

            if (schedule.WeekDay == WeekDay)
            {
                TempData["Error"] = "Bạn chưa thay đổi ngày, không thể sửa!";
                return RedirectToAction("ScheduleSlot", new { slotId = schedule.SlotId });
            }

            bool isDuplicate = db.WeeklyPTSchedule.Any(s =>
                s.SlotId == schedule.SlotId &&
                s.WeekDay == WeekDay &&
                s.StartTime == schedule.StartTime &&
                s.EndTime == schedule.EndTime &&
                s.ScheduleId != scheduleId);

            if (isDuplicate)
            {
                TempData["Error"] = "Lịch đã tồn tại vào ngày này. Vui lòng chọn ngày khác.";
                return RedirectToAction("ScheduleSlot", new { slotId = schedule.SlotId });
            }

            schedule.WeekDay = WeekDay;
            db.SaveChanges();

            var slot = db.PTSlot.Find(schedule.SlotId);
            if (slot != null && slot.MemberId.HasValue)
            {
                db.Notification.Add(new Notification
                {
                    UserId = slot.MemberId.Value,
                    Message = $"Trainer {slot.TrainerId} đã sửa lịch buổi {schedule.ScheduleId} sang Thứ {(WeekDay == 8 ? "CN" : WeekDay.ToString())}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            }

            TempData["Success"] = "Đã sửa lịch thành công và gửi thông báo cho member!";
            return RedirectToAction("ScheduleSlot", new { slotId = schedule.SlotId });
        }



        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
