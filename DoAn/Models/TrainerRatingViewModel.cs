using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DoAn.Models
{
    public class TrainerRatingViewModel
    {
        public int RatingId { get; set; }
        public string TrainerUsername { get; set; }
        public string MemberUsername { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

}