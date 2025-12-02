using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace DoAn.Models
{
	
        public class RateTrainerViewModel
        {
            [Required]
            public int TrainerId { get; set; }

            [Required]
            [StringLength(100)]
            public string TrainerName { get; set; }

            [Required]
            [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5")]
            public int Rating { get; set; }

            [StringLength(500)]
            public string Comment { get; set; }
        }
        public class BookClassViewModel
        {
            [Required]
            public int ClassId { get; set; }

            [Required]
            [StringLength(100)]
            public string ClassName { get; set; }

            [Required]
            [StringLength(100)]
            public string TrainerName { get; set; }
        }



    
}