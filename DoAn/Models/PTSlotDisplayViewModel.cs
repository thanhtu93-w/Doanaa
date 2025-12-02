using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAn.Models
{
	public class PTSlotDisplayViewModel
	{
        public int SlotId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; }       
        public string MemberUsername { get; set; }


        public int? MemberId { get; set; } // Thêm MemberId
    }
}