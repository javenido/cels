using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CELS.Models
{
    public class AppUser : IdentityUser
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int YearOfInterest { get; set; }

        public string FirstLanguage { get; set; }

        public string AvailabilityStart { get; set; }

        public string AvailabilityEnd { get; set; }

        public string PhotoUrl { get; set; }

        public string ResumeUrl { get; set; }

        /* THE FOLLOWING PROPERTIES/METHODS ARE USED BY VIEW MODELS */
        private int satisfactionRate = 0;
        private int timesBooked = 0;
        public void SetSatisfactionRate(int rate) => satisfactionRate = rate;
        public int GetSatisfactionRate() => satisfactionRate;
        public void SetTimesBooked(int count) => timesBooked = count;
        public int GetTimesBooked() => timesBooked;

        public int GetYearsOfExperience() => DateTime.Now.Year - YearOfInterest;

        public string GetAvailability()
        {
            DateTime from = DateTime.Parse(AvailabilityStart);
            DateTime to = DateTime.Parse(AvailabilityEnd);
            return $"{from:h:mm tt}-{to:h:mm tt}";
        }

        public string GetFullName()
        {
            return $"{FirstName} {LastName}";
        }
    }

    public enum Languages
    {
        English, Mandarin, Hindi, Spanish, French, Arabic, Bengali, Russian, Portuguese, Indonesian, Tagalog, Other
    }
}
