using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CELS.Models.ViewModels
{
    public class AppointmentsViewModel
    {
        public IQueryable<Appointment> Appointments;

        public string UserName { get; set; }

        public bool UserIsTeacher { get; set; }

        public bool PastAppointments { get; set; }
    }
}