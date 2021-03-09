using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CELS.Models
{
    public class IdentitySeedData
    {
        private const string ROLE_NAME_TUTOR = "tutor";
        private const string ROLE_NAME_TUTEE = "tutee";

        public static async Task EnsurePopulated(IApplicationBuilder app)
        {
            RoleManager<IdentityRole> roleManager = app.ApplicationServices
                .GetRequiredService<RoleManager<IdentityRole>>();

            // create the Tutor role
            IdentityRole tutorRole = await roleManager.FindByNameAsync(ROLE_NAME_TUTOR);
            if (tutorRole == null)
            {
                tutorRole = new IdentityRole(ROLE_NAME_TUTOR);
                await roleManager.CreateAsync(tutorRole);
            }

            // create the Tutee role
            IdentityRole tuteeRole = await roleManager.FindByNameAsync(ROLE_NAME_TUTEE);
            if (tuteeRole == null)
            {
                tuteeRole = new IdentityRole(ROLE_NAME_TUTEE);
                await roleManager.CreateAsync(tuteeRole);
            }
        }
    }
}
