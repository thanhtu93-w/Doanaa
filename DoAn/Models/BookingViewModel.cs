using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace DoAn.Models
{
    public class BookingViewModel
    {
        public int BookingId { get; set; }
        public string Status { get; set; }
        public DateTime? BookingTime { get; set; }
        public string TrainerName { get; set; } 
        public string ClassName { get; set; }
    }

}