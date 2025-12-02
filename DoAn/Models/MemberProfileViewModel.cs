using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAn.Models
{
	public class MemberProfileViewModel
	{
        public int UserId { get; set; }        
        public string Username { get; set; }  
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public string FullName { get; set; }   
        public string Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Avatar { get; set; }
        public decimal? Height { get; set; }
        public decimal? Weight { get; set; }
        public string Address { get; set; }
    }
}