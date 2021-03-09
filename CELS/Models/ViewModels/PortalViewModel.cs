using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CELS.Models.ViewModels
{
    public class PortalViewModel
    {
        public AppUser User { get; set; }

        public bool IsTeacher { get; set; }

        public Comment LatestComment { get; set; }

        public Appointment NextAppointment { get; set; }

        public AppUser BestTeacher { get; set; }
    }
}