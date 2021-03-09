using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CELS.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public int YearOfInterest { get; set; }

        [Required]
        public string Description { get; set; }

        public string FirstLanguage { get; set; }

        public string User_AvailabilityStart { get; set; }

        public string User_AvailabilityEnd { get; set; }

        public string PhotoUrl { get; set; }

        public string ResumeUrl { get; set; }

        public bool IsTeacher { get; set; }

        public IFormFile ProfilePic { get; set; }

        public IFormFile Resume { get; set; }
    }
}
