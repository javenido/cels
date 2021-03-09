using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using CELS.Models;
using CELS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CELS.Controllers
{
    [Authorize(Roles = "tutee")]
    public class TeacherController : Controller
    {
        private UserManager<AppUser> userManager;
        private readonly AmazonDynamoDBClient dynamoDBClient;

        private const int PAGE_SIZE = 6;

        public TeacherController(UserManager<AppUser> userMgr, IConfiguration configuration)
        {
            userManager = userMgr;
            var credentials = new BasicAWSCredentials(configuration["AWSCredentials:AccesskeyID"],
                    configuration["AWSCredentials:Secretaccesskey"]);
            dynamoDBClient = new AmazonDynamoDBClient(credentials, RegionEndpoint.USEast1);
        }

        public async Task<ViewResult> BrowseTeachers(int page = 1, string sortby = "rating", bool order = false, string search = "")
        {
            // select all teachers with names containing the search key
            var selectedTeachers = (await userManager.GetUsersInRoleAsync("tutor"))
                .Where(teacher => teacher.GetFullName().ToLower().Contains(search.Trim().ToLower()))
                .AsQueryable();

            // set satisfaction rating of each teacher
            foreach (AppUser teacher in selectedTeachers)
            {
                int rating = await Appointment.CalculateSatisfactionRating(dynamoDBClient, teacher.UserName);
                teacher.SetSatisfactionRate(rating);
            }

            // set number of times booked for each teacher
            foreach (AppUser teacher in selectedTeachers)
            {
                teacher.SetTimesBooked((await Appointment.GetAllAppointments(dynamoDBClient))
                    .Where(a => a.TeacherId == teacher.UserName).Count());
            }

            // apply sorting, in descending order by default
            switch (sortby)
            {
                case "bookings":
                    selectedTeachers = selectedTeachers.OrderByDescending(teacher => teacher.GetTimesBooked());
                    break;
                case "name":
                    selectedTeachers = selectedTeachers.OrderByDescending(teacher => teacher.GetFullName());
                    break;
                case "years":
                    selectedTeachers = selectedTeachers.OrderByDescending(teacher => teacher.GetYearsOfExperience());
                    break;
                default:
                    selectedTeachers = selectedTeachers.OrderByDescending(teacher => teacher.GetSatisfactionRate());
                    break;
            }

            // if order is ascending
            if (order)
                selectedTeachers = selectedTeachers.Reverse();

            // create and return view model
            return View(new TeachersViewModel(selectedTeachers)
            {
                PagingInfo = new PagingInfo
                {
                    ItemsPerPage = PAGE_SIZE,
                    CurrentPage = page,
                    TotalItems = selectedTeachers.Count()
                },
                SortBy = sortby,
                Order = order,
                SearchKey = search
            });
        }
    }
}
