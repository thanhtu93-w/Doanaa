using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAn.Models
{
	public class ViewModels
	{
        public class HomeIndexViewModel
        {
            public List<GymClassVM> FeaturedClasses { get; set; }
            public List<TrainerVM> FeaturedTrainers { get; set; }
        }

        public class GymClassVM
        {
            public int ClassId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int Capacity { get; set; }
            public DateTime? CreatedAt { get; set; }
            public string RecurrenceDays { get; set; }
            public TimeSpan? TimeFrom { get; set; }
            public TimeSpan? TimeTo { get; set; }
            public int TrainerId { get; set; }
            public string TrainerName { get; set; }
        }

        public class TrainerVM
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public string Email { get; set; }
            public string Avatar { get; set; }
            public string FullName { get; set; }
            public string Description { get; set; }
            public string Gender { get; set; }
            public DateTime? BirthDate { get; set; }
            public string Address { get; set; }
            public int? ExperienceYears { get; set; }
            public string Specialties { get; set; }

            public List<GymClass> Classes { get; set; }
            public double AverageRating { get; set; }
            public int RatingCount { get; set; }

            public int ClassCount { get; set; }
            public bool IsAvailableForPT { get; set; }
            public List<PTSlot> AvailablePTSlots { get; set; } = new List<PTSlot>();

        }
        public class ClassDetailsViewModel
        {
            public int ClassId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public int Capacity { get; set; }
            public DateTime? CreatedAt { get; set; }
            public string RecurrenceDays { get; set; }
            public TimeSpan? TimeFrom { get; set; }
            public TimeSpan? TimeTo { get; set; }

            // Trainer info
            public int TrainerId { get; set; }
            public string TrainerName { get; set; }
            public string TrainerAvatar { get; set; }
            public string TrainerDescription { get; set; }
        }
        public class PackageVM
        {
            public int PackageId { get; set; }
            public string Name { get; set; }
            public string PackageType { get; set; }
            public int TotalSessions { get; set; }
            public int? MaxSessionPerWeek { get; set; }
            public bool IsFeatured { get; set; }
        }
        public class ScheduleSlotVM
        {
            public int SlotId { get; set; }
            public int TrainerId { get; set; }
            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }

            public List<WeeklyPTSchedule> ExistingSchedules { get; set; }
            public int ScheduleId { get; set; }
            public int WeekDay { get; set; }
            public List<int> UsedWeekDays(int currentScheduleId)
            {
                return ExistingSchedules
                    .Where(s => s.ScheduleId != currentScheduleId)
                    .Select(s => s.WeekDay)
                    .ToList();
            }
        }
       

    }
}