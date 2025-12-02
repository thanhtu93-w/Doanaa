using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DoAn.Models
{
   
        public class CreateClassViewModel
        {
            [Required]
            [StringLength(100, ErrorMessage = "Tên lớp tối đa 100 ký tự")]
            public string Name { get; set; }

            [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
            public string Description { get; set; }

             [Required(ErrorMessage = "Chọn thời gian bắt đầu")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Phải chọn thời gian ")]
        [DataType(DataType.Time)]
            public TimeSpan TimeFrom { get; set; }  // giờ bắt đầu

            [Required(ErrorMessage = "Chọn thời gian kết thúc")]
            [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Phải chọn thời gian ")]
            [DataType(DataType.Time)]
            public TimeSpan TimeTo { get; set; }    // giờ kết thúc

            [Required(ErrorMessage = "Chọn lịch tập")]
            [RegularExpression(@"^(2,4,6|3,5,7)$", ErrorMessage = "Chỉ chọn 2,4,6 hoặc 3,5,7")]
            public string RecurrenceDays { get; set; } // "2,4,6" hoặc "3,5,7"

            [Required]
            [Range(10, 20, ErrorMessage = "Số lượng học viên phải từ 10 đến 20")]
            public int Capacity { get; set; }

            [Required]
            [Range(30, 180, ErrorMessage = "DurationMinutes phải từ 60 đến 180 phút")]
            public int DurationMinutes { get; set; }

            public bool IsFeatured { get; set; }
        }



        public class AttendanceViewModel
        {

            public GymClass GymClass { get; set; }
            public List<Booking> Bookings { get; set; }
            public List<int> RecurrenceDays { get; set; }
      
            public DateTime SelectedSessionDate { get; set; }
        

    }


    public class PTSlotViewModel
    {

       
            public TimeSpan StartHour { get; set; } 
            public TimeSpan EndHour { get; set; }
        

    }

}