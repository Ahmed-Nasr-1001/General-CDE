using ACC.Services;
using Aspose.CAD.FileFormats.Collada.FileParser.Elements;
using BusinessLogic.Repository.RepositoryClasses;
using BusinessLogic.Repository.RepositoryInterfaces;
using BusinessLogic.Services;
using DataLayer;
using DataLayer.Models;
using Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;

namespace ACC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
    
            builder.Services.AddDbContext<AppDbContext>((options) =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });
            builder.Services
                   .AddIdentity<ApplicationUser, ApplicationRole>(options =>
                   {
                       options.Password.RequiredLength = 8;
                       options.Password.RequireDigit = true;
                       options.Password.RequireNonAlphanumeric = true;
                       options.Password.RequireUppercase = true;
                       options.Password.RequireLowercase = true;
                   })
                   .AddUserStore<UserStore<
                       ApplicationUser,
                       ApplicationRole,
                       AppDbContext,
                       string,
                       IdentityUserClaim<string>,
                       ApplicationUserRole,
                       IdentityUserLogin<string>,
                       IdentityUserToken<string>,
                       IdentityRoleClaim<string>>>()
                   .AddRoleStore<RoleStore<
                       ApplicationRole,
                       AppDbContext,
                       string,
                       ApplicationUserRole,
                       IdentityRoleClaim<string>>>()
                   .AddEntityFrameworkStores<AppDbContext>()
                   .AddDefaultTokenProviders();


           


            #region Dependency injection
            builder.Services.AddControllers().AddJsonOptions(options =>
               {
                   options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
               });

          
            builder.Services.AddControllersWithViews();

            
            builder.Services.AddScoped<IProjetcRepository, ProjectRepository>();
            builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IProjectActivityRepository, ProjectActivityRepository>();
             // Add services to the container of isuue//
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<IIssueRepository, IssueRepository>();
            builder.Services.AddSingleton<IWebHostEnvironment>(env => builder.Environment);
            builder.Services.AddSingleton<Helpers.FileHelper>();
            builder.Services.AddScoped<IfcFileRepository>();
            builder.Services.AddScoped<IfcFileService>();
            builder.Services.AddScoped<ITransmittalRepository, TransmittalRepository>();
            builder.Services.AddScoped<IFolderRepository, FolderRepository>();
            builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
            builder.Services.AddScoped<IDocumentVersionRepository, DocumentVersionRepository>();
            builder.Services.AddScoped<IIssueCommentRepository, IssueCommentRepository>();
            builder.Services.AddScoped<IIssueNotificationRepository, IssueNotificationRepository>();


            builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
            builder.Services.AddScoped<IWorkflowRepository, WorkflowRepository>();
            builder.Services.AddScoped<IWorkFlowStepRepository, WorkflowStepRepository>();
            builder.Services.AddScoped<FolderService>();
            builder.Services.AddScoped<ReviewFolderService>();
            builder.Services.AddScoped<ReviewDocumentService>();
            builder.Services.AddScoped<WorkflowStepsUsersService>();
            builder.Services.AddScoped<ReviewStepUsersService>();
            builder.Services.AddScoped<IssueReviewersService>();
            builder.Services.AddScoped<UserRoleService>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<INotificationService, NotificationService>();

            #endregion


            var app = builder.Build();


            async Task SeedDataAsync()
            {
                using var scope = app.Services.CreateScope();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await DBInitializer.SeedAsync(roleManager, context, userManager);
            }

            // ❗ Await it before running the app
            SeedDataAsync().GetAwaiter().GetResult();



            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles(new StaticFileOptions
            {
                ServeUnknownFileTypes = true,
                DefaultContentType = "application/octet-stream" // or a more specific type if known
            });


            app.UseRouting();

            app.UseAuthorization(); 

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Account}/{action=LogIn}/{id?}");

            app.Run();
        }
    }
}
