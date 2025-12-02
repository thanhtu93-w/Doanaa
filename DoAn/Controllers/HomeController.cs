using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DoAn.Models;
using static System.Collections.Specialized.BitVector32;

namespace DoAn.Controllers
{
    public class HomeController : Controller
    {
        private GymCenterDBEntities1 db = new GymCenterDBEntities1();

        // ================= 1. Trang chủ =================
        public ActionResult Index(string section = "")
        {

            var featuredClasses = db.GymClass
                .Where(c => c.IsFeatured == true && c.IsActive == true)
                .OrderByDescending(c => c.CreatedAt ?? DateTime.MinValue)
                .Take(6)
                .Select(c => new ViewModels.GymClassVM
                {
                    ClassId = c.ClassId,
                    Name = c.Name,
                    Description = c.Description,
                    Capacity = c.Capacity,
                    CreatedAt = c.CreatedAt,
                    RecurrenceDays = c.RecurrenceDays,
                    TimeFrom = c.TimeFrom,
                    TimeTo = c.TimeTo,
                    TrainerId = c.TrainerId
                })
                .ToList();

            var featuredTrainers = db.Users
                .Where(u => u.Role == "Trainer" && u.IsActive == true)
                .Where(t => db.GymClass.Any(c => c.TrainerId == t.UserId && c.IsActive == true))
                .Select(t => new ViewModels.TrainerVM
                {
                    UserId = t.UserId,
                    Username = t.Username,
                    Avatar = t.TrainerProfile.Avatar,
                    Description = t.TrainerProfile.Description
                })
                .ToList();

            var model = new ViewModels.HomeIndexViewModel
            {
                FeaturedClasses = featuredClasses,
                FeaturedTrainers = featuredTrainers
            };

            ViewBag.Section = section;
            return View(model);
        }

        // ================= 2. Xem chi tiết lớp =================
        public ActionResult ClassDetails(int classId)
        {
            var gymClass = db.GymClass
                .Where(c => c.ClassId == classId && c.IsActive == true)
                .Select(c => new ViewModels.ClassDetailsViewModel
                {
                    ClassId = c.ClassId,
                    Name = c.Name,
                    Description = c.Description,
                    Capacity = c.Capacity,
                    CreatedAt = c.CreatedAt,
                    RecurrenceDays = c.RecurrenceDays,
                    TimeFrom = c.TimeFrom,
                    TimeTo = c.TimeTo,
                    TrainerId = c.TrainerId,
                    TrainerName = c.Users.Username,
                    TrainerAvatar = c.Users.TrainerProfile.Avatar,
                    TrainerDescription = c.Users.TrainerProfile.Description
                })
                .FirstOrDefault();

            if (gymClass == null)
                return HttpNotFound();

            return View(gymClass);
        }

        // ================= 3. Xem chi tiết Trainer =================
        public ActionResult TrainerDetails(int trainerId)
        {
            var trainer = db.Users
                   .Include("TrainerProfile")
                   .FirstOrDefault(u => u.UserId == trainerId && u.Role == "Trainer" && u.IsActive == true);

            if (trainer == null)
                return HttpNotFound();

            var classes = db.GymClass
                            .Where(c => c.TrainerId == trainerId && c.IsActive == true)
                            .ToList();

            var ratings = db.TrainerRating
                            .Where(r => r.TrainerId == trainerId)
                            .ToList();

            var availableSlots = db.PTSlot
                          .Where(s => s.TrainerId == trainerId
                                   && s.MemberId == null
                                   && s.Status == "Available")
                          .OrderBy(s => s.StartTime)   
                          .ToList();


            var vm = new Models.ViewModels.TrainerVM
            {
                UserId = trainer.UserId,
                Username = trainer.Username,
                Email = trainer.Email,
                Avatar = trainer.TrainerProfile?.Avatar,
                FullName = !string.IsNullOrEmpty(trainer.TrainerProfile?.FullName)
                           ? trainer.TrainerProfile.FullName
                           : trainer.Username,
                Description = trainer.TrainerProfile?.Description,
                Gender = trainer.TrainerProfile?.Gender,
                BirthDate = trainer.TrainerProfile?.BirthDate,
                Address = trainer.TrainerProfile?.Address,
                ExperienceYears = trainer.TrainerProfile?.ExperienceYears,
                Specialties = trainer.TrainerProfile?.Specialties,
                Classes = classes,
                AverageRating = ratings.Any() ? ratings.Average(r => r.Rating) : 0,
                RatingCount = ratings.Count(),
                IsAvailableForPT = trainer.TrainerProfile?.IsAvailableForPT ?? false,
                AvailablePTSlots = availableSlots   
            };

            return View(vm);
        }


        public ActionResult AllClasses()
        {
            var classes = db.GymClass
                .Where(c => c.IsActive == true)
                .OrderByDescending(c => c.CreatedAt ?? DateTime.MinValue)
                .Select(c => new ViewModels.GymClassVM
                {
                    ClassId = c.ClassId,
                    Name = c.Name,
                    Description = c.Description,
                    Capacity = c.Capacity,
                    CreatedAt = c.CreatedAt,
                    RecurrenceDays = c.RecurrenceDays,
                    TimeFrom = c.TimeFrom,
                    TimeTo = c.TimeTo,
                    TrainerId = c.TrainerId,
                    TrainerName = c.Users.Username
                })
                .ToList();

            return View(classes);
        }

        // ================= Xem full list Trainer =================
        public ActionResult AllTrainers()
        {
            var trainers = db.Users
                .Where(u => u.Role == "Trainer" && u.IsActive == true)
                .Select(t => new ViewModels.TrainerVM
                {
                    UserId = t.UserId,
                    Username = t.Username,
                    Avatar = t.TrainerProfile.Avatar,
                    Description = t.TrainerProfile.Description,
                    ClassCount = db.GymClass.Count(c => c.TrainerId == t.UserId && c.IsActive == true)
                })
                .ToList();

            return View(trainers);
        }

       
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();
            base.Dispose(disposing);
        }
    }
}
