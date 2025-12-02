using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAn.Models
{
    public class TrainerProfileViewModel
    {
        // Fields from Users
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; } 

        public int TrainerId { get; set; } 
        public string FullName { get; set; }
        public string Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Address { get; set; }
        public string Avatar { get; set; }
        public string Description { get; set; }
        public string Specialties { get; set; }
        public int? ExperienceYears { get; set; }
    }

}