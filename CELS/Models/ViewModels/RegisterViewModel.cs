using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CELS.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public bool IsTeacher { get; set; }

        [Required]
        public AppUser User { get; set; }

        [Required]
        public string Password { get; set; }

        public IFormFile Resume { get; set; }

        public IFormFile ProfilePic { get; set; }
    }
}
