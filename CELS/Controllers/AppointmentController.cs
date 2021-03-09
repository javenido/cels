using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Runtime;
using CELS.Models;
using CELS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CELS.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private UserManager<AppUser> userManager;
        private AmazonDynamoDBClient dynamoDBClient;

        public AppointmentController(UserManager<AppUser> userMgr, IConfiguration configuration)
        {
            userManager = userMgr;
            var credentials = new BasicAWSCredentials(configuration["AWSCredentials:AccesskeyID"],
                    configuration["AWSCredentials:Secretaccesskey"]);
            dynamoDBClient = new AmazonDynamoDBClient(credentials, RegionEndpoint.USEast1);
        }

        public async Task<ViewResult> Appointments(string userName = "", bool pastAppointments = false)
        {
            // if there is no specified user, select current user
            if (userName.Length == 0)
            {
                userName = User.Identity.Name;
            }

            // select all appointments this user is involved with, sorted according to date
            var selectedAppointments = (await Appointment.GetAllAppointments(dynamoDBClient))
                .Where(a => userName == a.TeacherId || userName == a.StudentId)
                .OrderBy(a => DateTime.Parse(a.Date))
                .ThenBy(a => DateTime.Parse(a.StartTime))
                .AsQueryable();

            // determine if request is to retrieve past or upcoming appointments
            if (pastAppointments)
            {
                selectedAppointments = selectedAppointments.Where(a => a.GetDateTime() < DateTime.Now);
                selectedAppointments = selectedAppointments.Reverse();
            }
            else
            {
                selectedAppointments = selectedAppointments.Where(a => a.GetDateTime() > DateTime.Now);
            }

            // pass references to the teacher and student of each appointment to the view
            foreach (Appointment appointment in selectedAppointments)
            {
                appointment.SetTeacher(await userManager.FindByNameAsync(appointment.TeacherId));
                appointment.SetStudent(await userManager.FindByNameAsync(appointment.StudentId));
            }

            // get the specified user
            var user = await userManager.FindByNameAsync(userName);

            // instantiate and return view model
            return View(new AppointmentsViewModel
            {
                Appointments = selectedAppointments,
                UserName = user.GetFullName(),
                UserIsTeacher = await userManager.IsInRoleAsync(user, "tutor"),
                PastAppointments = pastAppointments
            });
        }

        [HttpGet]
        [Authorize(Roles = "tutee")]
        public async Task<ViewResult> BookAppointment(string teacherId, string studentId)
        {
            Appointment appointment = new Appointment
            {
                TeacherId = teacherId,
                StudentId = studentId
            };

            appointment.SetTeacher(await userManager.FindByNameAsync(teacherId));
            return View(appointment);
        }

        [HttpPost]
        [Authorize(Roles = "tutee")]
        public async Task<IActionResult> BookAppointment(Appointment model)
        {
            model.SetTeacher(await userManager.FindByNameAsync(model.TeacherId));
            if (ModelState.IsValid)
            {
                // check if date is in the future
                DateTime modelTime = DateTime.Parse(model.Date + " " + model.StartTime);
                if (modelTime < DateTime.Now)
                {
                    ModelState.AddModelError("", "Please select a date that is in the future.");
                    return View(model);
                }

                // check if time provided is valid (startTime < endTime)
                if (DateTime.Parse(model.StartTime) >= DateTime.Parse(model.EndTime))
                {
                    ModelState.AddModelError("", "Please specify a valid time slot.");
                    return View(model);
                }

                // check if there's scheduling conflict
                var appointments = (await Appointment.GetAllAppointments(dynamoDBClient))
                    .Where(a => a.TeacherId == model.TeacherId || a.StudentId == model.StudentId);

                if (appointments.Any(a => a.ConflictsWith(
                    DateTime.Parse(model.Date),
                    DateTime.Parse(model.StartTime),
                    DateTime.Parse(model.EndTime))))
                {
                    ModelState.AddModelError("", "You or the teacher already have an appointment scheduled within this time frame. " +
                        "Verify that you are free during this period. " +
                        "You can also check the teacher's schedule via the link below to avoid scheduling conflicts.");
                    return View(model);
                }

                // generate random ID for the appointment
                model.AppointmentId = Guid.NewGuid().ToString();

                // save to table
                DynamoDBContext context = new DynamoDBContext(dynamoDBClient);
                await context.SaveAsync(model);

                // email teacher
                model.SetStudent(await userManager.FindByNameAsync(model.StudentId));
                //SendEmail(model.GetTeacher(), model.GetStudent(), model, HttpContext.Request.Host.ToString());

                return RedirectToAction("Portal", "Application");
            }

            model.SetTeacher(await userManager.FindByNameAsync(model.TeacherId));
            return View(model);
        }

        [Authorize(Roles = "tutee")]
        public async Task<IActionResult> SatisfyAppointment(string id)
        {
            // if no id provided
            if (id == null)
                return RedirectToAction("Portal", "Application");

            Appointment appointment = (await Appointment.GetAllAppointments(dynamoDBClient))
                .FirstOrDefault(a => a.AppointmentId == id);

            // if no appointment with id provided exists
            if (appointment == null)
                return RedirectToAction("Portal", "Application");

            // if current user is not the student of the appointment
            if (appointment.StudentId != User.Identity.Name)
                return RedirectToAction("Portal", "Application");

            // if appointment is already satisfied
            if (appointment.Satisfied)
                return RedirectToAction("Portal", "Application");

            // set satisfied to true
            appointment.Satisfied = true;
            DynamoDBContext context = new DynamoDBContext(dynamoDBClient);
            await context.SaveAsync(appointment);

            return Redirect(HttpContext.Request.Headers["referer"]);
        }

        [Authorize(Roles = "tutee")]
        private static void SendEmail(AppUser recipient, AppUser student, Appointment appointment, string host)
        {
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", 587);

            smtpClient.Credentials = new System.Net.NetworkCredential("ets.appointments@gmail.com", "etspassword");
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.EnableSsl = true;

            MailMessage mail = new MailMessage();
            mail.Subject = "(noreply) CELS - Appointment Confirmation";
            mail.IsBodyHtml = true;
            mail.Body = $"Dear {recipient.GetFullName()},<br/><br/>";
            mail.Body += $"You have been booked for a scheduled online meeting with a student.<br/></br/>";
            mail.Body += $"Student Name: {student.GetFullName()}<br/>When: {appointment.GetDate()}<br/>";
            mail.Body += $"Time: {appointment.GetTime()}<br/>";
            mail.Body += (appointment.Message != null) ? $"<br/>Message from student: {appointment.Message}" : "";

            string link = $"{host}/Appointment/Appointments?userName={recipient.UserName}";
            mail.Body += $"<br/>You can go to this link to view your upcoming appointments: <a href=\"{link}\">{link}</a>";
            mail.Body += $"<br/><br/>Centennial English Learning Services (CELS), Technical Support";

            // Setting From and To
            mail.From = new MailAddress("ets.appointments@gmail.com", "CELS-noreply");
            mail.To.Add(new MailAddress(recipient.Email));

            smtpClient.Send(mail);
        }
    }
}