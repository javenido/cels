using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
using CELS.Models;
using CELS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CELS.Controllers
{
    [Authorize]
    public class ApplicationController : Controller
    {
        private UserManager<AppUser> userManager;
        private readonly AmazonDynamoDBClient dynamoDBClient;

        public ApplicationController(UserManager<AppUser> userMgr, IConfiguration configuration)
        {
            userManager = userMgr;
            var credentials = new BasicAWSCredentials(configuration["AWSCredentials:AccesskeyID"],
                    configuration["AWSCredentials:Secretaccesskey"]);
            dynamoDBClient = new AmazonDynamoDBClient(credentials, RegionEndpoint.USEast1);
        }

        public async Task<ViewResult> Portal()
        {
            // get current user
            AppUser user = await userManager.GetUserAsync(HttpContext.User);

            // get next appointment
            var nextAppointment = (await Appointment.GetAllAppointments(dynamoDBClient))
                .Where(a => user.UserName == a.TeacherId || user.UserName == a.StudentId)
                .Where(a => a.GetDateTime() >= DateTime.Now)
                .OrderBy(a => DateTime.Parse(a.Date))
                .ThenBy(a => DateTime.Parse(a.StartTime))
                .FirstOrDefault();

            // if there's an appointment found, set its teacher and student
            if (nextAppointment != null)
            {
                nextAppointment.SetTeacher(await userManager.FindByNameAsync(nextAppointment.TeacherId));
                nextAppointment.SetStudent(await userManager.FindByNameAsync(nextAppointment.StudentId));
            }

            // instantiate the view model
            PortalViewModel model = new PortalViewModel
            {
                User = user,
                IsTeacher = await userManager.IsInRoleAsync(user, "tutor"),
                LatestComment = (await Comment.GetAllComments(dynamoDBClient))
                    .Where(c => c.TeacherId == user.UserName)
                    .OrderByDescending(c => c.DateSubmitted)
                    .FirstOrDefault(),
                NextAppointment = nextAppointment
            };

            // get the latest comment (for teacher's profile only)
            if (model.IsTeacher)
            {
                // get all comments on this teacher
                var comments = (await Comment.GetAllComments(dynamoDBClient))
                    .Where(c => c.TeacherId == user.UserName);

                // set latest comment
                model.LatestComment = comments
                    .OrderByDescending(c => c.DateSubmitted)
                    .FirstOrDefault();

                if (model.LatestComment != null)
                {
                    model.LatestComment.SetAuthor((await userManager
                        .FindByNameAsync(model.LatestComment.StudentId)));
                }
            }

            // get the best teacher (for student's profile only)
            else
            {
                var allTeachers = await userManager.GetUsersInRoleAsync("tutor");

                if (allTeachers.Any())
                {
                    foreach (var teacher in allTeachers)
                    {
                        int rating = await Appointment.CalculateSatisfactionRating(dynamoDBClient, teacher.UserName);
                        teacher.SetSatisfactionRate(rating);
                        teacher.SetTimesBooked((await Appointment.GetAllAppointments(dynamoDBClient))
                            .Where(a => a.TeacherId == teacher.UserName).Count());
                    }

                    model.BestTeacher = allTeachers.OrderByDescending(t => t.GetTimesBooked())
                        .ThenByDescending(t => t.GetSatisfactionRate())
                        .ThenByDescending(t => t.GetYearsOfExperience())
                        .First();
                }
            }

            // return the model to the view
            return View(model);
        }

        public async Task<ViewResult> Profile(string userName = "")
        {
            AppUser user;
            if (userName.Length == 0)
            {
                user = await userManager.GetUserAsync(HttpContext.User);
            }
            else
            {
                user = await userManager.FindByNameAsync(userName);
            }

            ProfileViewModel model = new ProfileViewModel
            {
                User = user,
                IsTeacher = await userManager.IsInRoleAsync(user, "tutor")
            };

            if (model.IsTeacher)
            {
                // get all comments on this teacher
                var comments = (await Comment.GetAllComments(dynamoDBClient))
                    .Where(c => c.TeacherId == user.UserName);

                // set comment count
                model.CommentCount = comments.Count();

                // set latest comment here
                model.LatestComment = comments
                    .OrderByDescending(c => c.DateSubmitted)
                    .FirstOrDefault();

                if (model.LatestComment != null)
                {
                    model.LatestComment.SetAuthor((await userManager
                        .FindByNameAsync(model.LatestComment.StudentId)));
                }

                var appointments = await Appointment.GetAllAppointments(dynamoDBClient);
                double rating = 0;
                double total = appointments.Where(a => a.TeacherId == model.User.UserName).Count();
                double satisfied = appointments.Where(a => a.TeacherId == model.User.UserName)
                    .Where(a => a.Satisfied).Count();

                if (total > 0)
                {
                    rating = (satisfied / total) * 100;
                }
                model.User.SetSatisfactionRate((int)rating);
                model.User.SetTimesBooked((int)total);
            }

            return View(model);
        }

        public async Task<ViewResult> BrowseTeachers()
        {
            return View(await userManager.GetUsersInRoleAsync("tutor"));
        }
    }
}
