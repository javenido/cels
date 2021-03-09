using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using CELS.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CELS
{
    public class Startup
    {
        // This property stores the configuration information from appsettings.json
        private IConfiguration Configuration { get; }

        // Constructor
        public Startup(IConfiguration configuration) => Configuration = configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlServer(Configuration["Data:CELSIdentity:ConnectionString"]));
            services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlServer(Configuration["Data:CELSIdentity2:ConnectionString"]));
            //services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Connection2RDSCELSIdentity"), providerOptions => providerOptions.EnableRetryOnFailure()));
            services.AddIdentity<AppUser, IdentityRole>(opts => { opts.User.RequireUniqueEmail = true; })
                .AddEntityFrameworkStores<AppIdentityDbContext>()
                .AddDefaultTokenProviders();
            services.AddMvc(options => { options.EnableEndpointRouting = false; });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();
            app.UseStaticFiles();
            app.UseStatusCodePages();
            app.UseMvc(configureRoutes =>
            {
                configureRoutes.MapRoute(
                    name: "default",
                    template: "{controller=Account}/{action=Login}");
            });

            IdentitySeedData.EnsurePopulated(app).Wait();
            ApplicationSeedData.SetAWSCredentials(new BasicAWSCredentials(
                Configuration["AWSCredentials:AccesskeyID"],
                Configuration["AWSCredentials:Secretaccesskey"]));
            ApplicationSeedData.EnsureBucketsExistAsync().Wait();
            ApplicationSeedData.EnsureDynamoDBTablesCreated().Wait();
        }
    }
}