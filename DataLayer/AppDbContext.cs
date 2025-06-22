using DataLayer.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;

using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer
{
    public class AppDbContext : IdentityDbContext<
    ApplicationUser,
    ApplicationRole,
    string,
    IdentityUserClaim<string>,
    ApplicationUserRole,
    IdentityUserLogin<string>,
    IdentityRoleClaim<string>,
    IdentityUserToken<string>>
    {
        public AppDbContext(DbContextOptions options):base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Define Composite Primary Key for Many-to-Many
            builder.Entity<ProjectMembers>().HasKey(pm => new { pm.ProjectId, pm.MemberId });

            // Configure Relationships
            builder.Entity<ProjectMembers>()
                .HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId);

            builder.Entity<ProjectMembers>()
                .HasOne(pm => pm.Member)
                .WithMany(u => u.Projects)
                .HasForeignKey(pm => pm.MemberId);

            builder.Entity<ProjectActivities>()
               .HasOne(pm => pm.project)
               .WithMany(p => p.Activities)
               .HasForeignKey(pm => pm.projectId);




            builder.Entity<ProjectCompany>()
        .HasKey(pc => new { pc.ProjectId, pc.CompanyId }); // Composite Key

            builder.Entity<ProjectCompany>()
                .HasOne(pc => pc.Project)
                .WithMany(p => p.ProjectCompany)
                .HasForeignKey(pc => pc.ProjectId);

            builder.Entity<ProjectCompany>()
                .HasOne(pc => pc.Company)
                .WithMany(c => c.ProjectCompany)
                .HasForeignKey(pc => pc.CompanyId);


            builder.Entity<Review>()
              .HasOne(wf => wf.WorkflowTemplate)
              .WithMany(u => u.Reviews)
              .HasForeignKey(pm => pm.WorkflowTemplateId);

            builder.Entity<ReviewDocument>()
                 .HasKey(rd => new { rd.ReviewId, rd.DocumentId });

            builder.Entity<ReviewDocument>()
                .HasOne(rd => rd.Review)
                .WithMany(r => r.ReviewDocuments)
                .HasForeignKey(rd => rd.ReviewId);

            builder.Entity<ReviewDocument>()
                .HasOne(rd => rd.Document)
                .WithMany(d => d.ReviewDocuments)
                .HasForeignKey(rd => rd.DocumentId);

            builder.Entity<ReviewFolder>()
               .HasKey(rd => new { rd.ReviewId, rd.FolderId });

            builder.Entity<ReviewFolder>()
                .HasOne(rd => rd.Review)
                .WithMany(r => r.ReviewFolders)
                .HasForeignKey(rd => rd.ReviewId);

            builder.Entity<ReviewFolder>()
                .HasOne(rd => rd.Folder)
                .WithMany(d => d.ReviewFolders)
                .HasForeignKey(rd => rd.FolderId);

            builder.Entity<WorkflowStepUser>()
               .HasKey(w => new { w.UserId, w.StepId  });

            builder.Entity<WorkflowStepUser>()
                .HasOne(rd => rd.Step)
                .WithMany(r => r.workflowStepUsers)
                .HasForeignKey(rd => rd.StepId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<WorkflowStepUser>()
                .HasOne(rd => rd.User)
                .WithMany(d => d.workflowStepUsers)
                .HasForeignKey(rd => rd.UserId)
                .OnDelete(DeleteBehavior.Restrict);



            builder.Entity<ReviewStepUser>()
              .HasKey(w => new { w.UserId, w.StepId , w.ReviewId });

            builder.Entity<ReviewStepUser>()
                .HasOne(rd => rd.Step)
                .WithMany(r => r.ReviewStepUsers)
                .HasForeignKey(rd => rd.StepId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<ReviewStepUser>()
                .HasOne(rd => rd.User)
                .WithMany(d => d.ReviewStepUsers)
                .HasForeignKey(rd => rd.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReviewStepUser>()
                .HasOne(rd => rd.Review)
                .WithMany(d => d.ReviewStepUsers)
                .HasForeignKey(rd => rd.ReviewId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReviewDocumentComment>()
                   .HasOne(c => c.Review)
                   .WithMany(r => r.Comments)
                   .HasForeignKey(c => c.ReviewId)
                   .OnDelete(DeleteBehavior.Restrict); 



            /// issue
            builder.Entity<Issue>()
                .HasOne(i => i.Project)
                .WithMany(p => p.Issues)
                .HasForeignKey(i => i.ProjectId);

            builder.Entity<Issue>()
           .HasOne(i => i.Document)
           .WithMany()
           .HasForeignKey(i => i.DocumentId)
           .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<IssueReviwers>()
           .HasKey(w => new { w.ReviewerId, w.IssueId });

            builder.Entity<IssueReviwers>()
                .HasOne(rd => rd.Issue)
                .WithMany(r => r.IssueReviwers)
                .HasForeignKey(rd => rd.IssueId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<IssueReviwers>()
                .HasOne(rd => rd.Reviewer)
                .WithMany(d => d.IssueReviwers)
                .HasForeignKey(rd => rd.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<IssueComment>()
               .HasOne(c => c.Issue)
               .WithMany(i => i.Comments)
               .HasForeignKey(c => c.IssueId);

            builder.Entity<IssueComment>()
                .HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            //Notification
            builder.Entity<Notification>(entity =>
            {
                entity.HasOne(n => n.Recipient)
                    .WithMany(u => u.ReceivedNotifications)
                    .HasForeignKey(n => n.RecipientId)
                    .OnDelete(DeleteBehavior.NoAction)

                    ;

            //Roles and permissions ------------------------------------

            builder.Ignore<IdentityUserRole<string>>();
            // Composite Key: ApplicationUserRole
            builder.Entity<ApplicationUserRole>()
                .HasKey(ur => new { ur.Id });

            builder.Entity<ApplicationUserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            builder.Entity<ApplicationUserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);





                entity.HasOne(n => n.Sender)
                    .WithMany(u => u.SentNotifications)
                    .HasForeignKey(n => n.SenderId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(n => n.Review)
                    .WithMany()
                    .HasForeignKey(n => n.ReviewId)
                    .OnDelete(DeleteBehavior.NoAction);
            });




        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<ProjectCompany> ProjectCompanies { get; set; }

        public DbSet<Folder> Folders { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentVersion> DocumentVersions { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMembers> ProjectMembers { get; set; }
        public DbSet<ProjectActivities> ProjectActivities { get; set; }
        public DbSet<ProjectCompany> ProjectCompany { get; set; }
        //issue DBSet
        public DbSet<Issue> Issues { get; set; }
        public DbSet<IssueReviwers> IssueReviwers { get; set; }
        public DbSet<IssueComment> IssueComments { get; set; }
        public DbSet<IssueNotification> IssueNotifications { get; set; }

        public DbSet<IfcFile> IfcFiles { get; set; }

        public DbSet<Transmittal> Transmittals { get; set; }
        public DbSet<TransmittalDocument> TransmittalDocuments { get; set; }

        public DbSet<Review> Reviews { get; set; }
        public DbSet<WorkflowTemplate> WorkflowTemplates { get; set; }
        public DbSet<WorkflowStepTemplate> WorkflowStepTemplates { get; set; }
        public DbSet<ReviewFolder> ReviewsFolders { get; set; }
        public DbSet<ReviewDocument> ReviewDocuments { get; set; }

        public DbSet<ReviewDocumentComment> ReviewDocumentComments { get; set; }
        public DbSet<ReviewStepUser> ReviewStepUsers { get; set; }
        public DbSet<WorkflowStepUser> WorkflowStepUsers { get; set; }
        
       

        public DbSet<ApplicationUserRole> ApplicationUserRoles { get; set; }

        public DbSet<Notification> Notifications { get; set; }

  

    }
}
