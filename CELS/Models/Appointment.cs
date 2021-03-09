using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CELS.Models
{
    [DynamoDBTable("Appointments")]
    public class Appointment
    {
        [DynamoDBHashKey]
        public string AppointmentId { get; set; }

        [Required]
        public string TeacherId { get; set; }

        [Required]
        public string StudentId { get; set; }

        [Required]
        public string Date { get; set; }

        [Required]
        public string StartTime { get; set; }

        [Required]
        public string EndTime { get; set; }

        [Required]
        public string Course { get; set; }

        public string Message { get; set; }

        public bool Satisfied { get; set; }

        /* THE FOLLOWING FIELDS/METHODS ARE USED IN VIEWS */
        private AppUser teacher, student;
        public AppUser GetTeacher() => teacher;
        public void SetTeacher(AppUser _teacher) => teacher = _teacher;
        public AppUser GetStudent() => student;
        public void SetStudent(AppUser _student) => student = _student;

        public static async Task<IEnumerable<Appointment>> GetAllAppointments(AmazonDynamoDBClient client)
        {
            DynamoDBContext context = new DynamoDBContext(client);
            List<ScanCondition> scanConditions = new List<ScanCondition>();
            return await context.ScanAsync<Appointment>(scanConditions).GetRemainingAsync();
        }

        public bool ConflictsWith(DateTime date, DateTime start, DateTime end)
        {
            if (date != DateTime.Parse(Date))
                return false;

            var thisStart = DateTime.Parse(StartTime);
            var thisEnd = DateTime.Parse(EndTime);

            if (start >= thisStart && start <= thisEnd)
                return true;

            if (end > thisStart && end < thisEnd)
                return true;

            return false;
        }
        public string GetDate() => $"{DateTime.Parse(Date):dddd, d MMMMM yyyy}";
        public string GetTime() => $"{DateTime.Parse(StartTime):h:mm tt} - {DateTime.Parse(EndTime):h:mm tt}";

        public DateTime GetDateTime()
        {
            var date = DateTime.Parse(Date);
            var from = DateTime.Parse(StartTime);
            date = date.AddHours(from.Hour);
            date = date.AddMinutes(from.Minute);
            return date;
        }

        public static async Task<int> CalculateSatisfactionRating(AmazonDynamoDBClient client, string teacherId)
        {
            var appointments = await Appointment.GetAllAppointments(client);
            double rating = 0;
            double total = appointments.Where(a => a.TeacherId == teacherId).Count();
            double satisfied = appointments.Where(a => a.TeacherId == teacherId)
                .Where(a => a.Satisfied).Count();

            if (total > 0)
                rating = (satisfied / total) * 100;

            return (int)Math.Round(rating);
        }
    }

    public enum Courses
    {
        English_as_a_Second_Language,
        Business_Communications,
        Academic_English,
        Courses_for_Teachers,
        Writing_and_Grammar,
        Pronunciation_and_Speaking,
        Reading_and_Vocabulary,
        High_School_English,
        Post_Secondary_Level_English,
        Other
    }
}
