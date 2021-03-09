using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using CELS.Models;
using CELS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CELS.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        //private const string BUCKET_RESUMES = "celsresumesmarvin";
        //private const string BUCKET_PHOTOS = "celsphotosmarvin";
        private const string BUCKET_RESUMES = "celsresumes";
        private const string BUCKET_PHOTOS = "celsphotos";

        private IConfiguration Configuration { get; }
        private UserManager<AppUser> userManager;
        private SignInManager<AppUser> signInManager;

        private static IAmazonS3 s3Client;

        public AccountController(UserManager<AppUser> userMgr, SignInManager<AppUser> signInMgr, IConfiguration configuration)
        {
            userManager = userMgr;
            signInManager = signInMgr;
            Configuration = configuration;

            s3Client = new AmazonS3Client(new BasicAWSCredentials(Configuration["AWSCredentials:AccesskeyID"],
                Configuration["AWSCredentials:Secretaccesskey"]), RegionEndpoint.USEast1);
        }

        private static async Task CreateBucket(string bucketName)
        {
            PutBucketRequest request = new PutBucketRequest
            {
                BucketName = bucketName,
                UseClientRegion = true,
                CannedACL = S3CannedACL.PublicRead
            };
            await s3Client.PutBucketAsync(request);
        }

        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            // redirect to Portal if user is already signed in
            if (HttpContext.User.Identity.IsAuthenticated)
                return RedirectToAction("Portal", "Application");

            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                AppUser user = await userManager.FindByNameAsync(model.UserName);
                if (user != null)
                {
                    if ((await signInManager.PasswordSignInAsync(user, model.Password, false, false)).Succeeded)
                    {
                        return RedirectToAction("Portal", "Application");
                    }
                    ModelState.AddModelError("", "Incorrect password");
                }
                else
                {
                    ModelState.AddModelError("", "User name does not exist");
                }
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public ViewResult Register(bool isTeacher)
        {
            return View("Register", new RegisterViewModel { IsTeacher = isTeacher, User = new AppUser() });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                AppUser user = model.User;
                IdentityResult result = await userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // if user uploaded a profile picture
                    if (model.ProfilePic != null)
                    {
                        user.PhotoUrl = await UploadFileToBucket(model.ProfilePic, BUCKET_PHOTOS);
                    }

                    // if user uploaded a resume
                    if (model.Resume != null)
                    {
                        user.ResumeUrl = await UploadFileToBucket(model.Resume, BUCKET_RESUMES);
                    }

                    await userManager.UpdateAsync(user);
                    await userManager.AddToRoleAsync(user, model.IsTeacher ? "tutor" : "tutee");
                    await signInManager.SignInAsync(user, false);
                    return RedirectToAction("Portal", "Application");
                }

                // retireve all error messages
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }

        private static async Task<string> UploadFileToBucket(IFormFile file, string bucketName)
        {
            // upload selected file to temp directory
            var filePath = Path.Combine(Path.GetTempPath(), file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            // create a unique string for the file key
            string key = Guid.NewGuid().ToString();

            // upload to bucket using the key
            await UploadToBucket(filePath, bucketName, key);

            // delete temp file
            if ((System.IO.File.Exists(filePath)))
            {
                System.IO.File.Delete(filePath);
            }

            return $"https://{bucketName}.s3.amazonaws.com/{key}";
        }

        private static async Task UploadToBucket(string filePath, string bucketName, string key)
        {
            var fileTransferUtility = new TransferUtility(s3Client);
            var fileTransferUtilityRequest = new TransferUtilityUploadRequest
            {
                BucketName = bucketName,
                FilePath = filePath,
                Key = key,
                CannedACL = S3CannedACL.PublicRead
            };
            await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile(string userName = "")
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

            return View(new EditProfileViewModel
            {
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                YearOfInterest = user.YearOfInterest,
                Description = user.Description,
                FirstLanguage = user.FirstLanguage,
                User_AvailabilityStart = user.AvailabilityStart,
                User_AvailabilityEnd = user.AvailabilityEnd,
                PhotoUrl = user.PhotoUrl,
                ResumeUrl = user.ResumeUrl,
                IsTeacher = await userManager.IsInRoleAsync(user, "tutor")
            });
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                AppUser user = await userManager.FindByNameAsync(model.UserName);

                // update details
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;
                user.YearOfInterest = model.YearOfInterest;
                user.Description = model.Description;
                user.FirstLanguage = model.FirstLanguage;
                user.AvailabilityStart = model.User_AvailabilityStart;
                user.AvailabilityEnd = model.User_AvailabilityEnd;

                // if user has uploaded a new profile photo
                if (model.ProfilePic != null)
                {
                    user.PhotoUrl = await UploadFileToBucket(model.ProfilePic, BUCKET_PHOTOS);
                }

                // if user has uploaded a new resume
                if (model.Resume != null)
                {
                    user.ResumeUrl = await UploadFileToBucket(model.Resume, BUCKET_RESUMES);
                }

                // save changes
                IdentityResult result = await userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction("Profile", "Application");
                }

                // retrieve all error messages
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }
    }
}
