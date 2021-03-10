1. Run the following commands...

In NUGET Package Manager Console:
- add-migration Initial -context AppIdentityDbContext
- update-database -Context AppIdentityDbContext

OR

In CMD (CD to project directory):
- dotnet ef migrations add Initial --context AppIdentityDbContext
- dotnet ef database update --context AppIdentityDbContext

2. In appsettings.json, modify credentials.
3. In Startup.cs, edit connection strings.
4. In ApplicationSeedData, select Bucket names.
5. In AccountController, select Bucket names.