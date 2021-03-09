using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CELS.Models.ViewModels
{
    public class ProfileViewModel
    {
        public AppUser User { get; set; }

        public bool IsTeacher { get; set; }

        public int CommentCount { get; set; }

        public Comment LatestComment { get; set; }

        public int TimesBooked { get; set; }

        public int SatisfactionRate { get; set; }
    }
}
