using System;
using System.Collections.Generic;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CELS.Controllers
{
    [Authorize]
    public class CommentController : Controller
    {
        private UserManager<AppUser> userManager;
        private AmazonDynamoDBClient dynamoDBClient;

        private const int PAGE_SIZE = 5;

        public CommentController(UserManager<AppUser> userMgr, IConfiguration configuration)
        {
            userManager = userMgr;
            var credentials = new BasicAWSCredentials(configuration["AWSCredentials:AccesskeyID"],
                    configuration["AWSCredentials:Secretaccesskey"]);
            dynamoDBClient = new AmazonDynamoDBClient(credentials, RegionEndpoint.USEast1);
        }

        public async Task<IActionResult> Comments(string teacherId, int page = 1)
        {
            // check if there's a teacher with the specified id
            var teacher = await userManager.FindByNameAsync(teacherId);

            // return to portal if there isn't
            if (teacher == null)
                return RedirectToAction("Portal", "Application");

            // select all comments on the teacher selected, sorted by date (latest)
            var selectedComments = (await Comment.GetAllComments(dynamoDBClient))
                .Where(c => c.TeacherId == teacherId)
                .OrderByDescending(c => c.DateSubmitted)
                .AsQueryable();

            // set author's name for each comment
            foreach (var comment in selectedComments)
            {
                comment.SetAuthor((await userManager.FindByNameAsync(comment.StudentId)));
            }

            // instantiate and return view model
            return View(new CommentsViewModel(selectedComments)
            {
                PagingInfo = new PagingInfo
                {
                    ItemsPerPage = PAGE_SIZE,
                    CurrentPage = page,
                    TotalItems = selectedComments.Count()
                },
                TeacherId = teacherId,
                TeacherName = teacher.GetFullName()
            });
        }

        public async Task<IActionResult> DeleteComment(string id)
        {
            var comment = (await Comment.GetAllComments(dynamoDBClient))
                    .Where(c => c.CommentId == id).FirstOrDefault();

            // return to portal if comment does not exist
            if (comment == null)
                return RedirectToAction("Portal", "Application");
            DynamoDBContext context = new DynamoDBContext(dynamoDBClient);
            await context.DeleteAsync(comment);
            return RedirectToAction("Profile", "Application", new { userName = comment.TeacherId });
        }

        [HttpGet]
        public async Task<ViewResult> EnterComment(string teacherId, string studentId, string id)
        {
            Comment comment;

            // adding a new comment
            if (id == null)
            {
                comment = new Comment
                {
                    TeacherId = teacherId,
                    StudentId = studentId
                };
            }

            // editing an existing comment
            else
            {
                comment = (await Comment.GetAllComments(dynamoDBClient))
                    .Where(c => c.CommentId == id).First();
            }

            comment.SetTeacher(await userManager.FindByNameAsync(teacherId));
            return View(comment);
        }

        [HttpPost]
        public async Task<IActionResult> EnterComment(Comment model)
        {
            if (ModelState.IsValid)
            {
                // new comment
                if (model.CommentId == null)
                {
                    model.CommentId = Guid.NewGuid().ToString();
                    model.DateSubmitted = DateTime.Now;
                }

                // existing comment
                else
                {
                    model.DateSubmitted = (await Comment.GetAllComments(dynamoDBClient))
                        .Where(c => c.CommentId == model.CommentId).First().DateSubmitted;
                    model.DateEdited = DateTime.Now;
                }

                // save to table
                DynamoDBContext context = new DynamoDBContext(dynamoDBClient);
                await context.SaveAsync(model);

                return RedirectToAction("Profile", "Application", new { userName = model.TeacherId });
            }

            model.SetTeacher(await userManager.FindByNameAsync(model.TeacherId));
            return View(model);
        }
    }
}

