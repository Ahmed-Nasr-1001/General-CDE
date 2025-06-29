using ACC.ViewModels.TeamsVM;
using DataLayer;
using DataLayer.Models;
using DataLayer.Models.ClassHelper;
using DataLayer.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ACC.Controllers.ProjectDetailsController
{
    public class ProjectTeamController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager; private static readonly Dictionary<string, List<string>> FilePermissions = new()
{
    { "View", new() { Permissions.ViewFiles } },
    { "Create", new() { Permissions.ViewFiles, Permissions.UploadFiles, Permissions.CreateFolders } },
    { "Delete", new() { Permissions.ViewFiles, Permissions.UploadFiles, Permissions.CreateFolders, Permissions.DeleteFiles, Permissions.MoveFiles, Permissions.RenameFolders, Permissions.DeleteFolders } },
    { "Edit", new() { Permissions.ViewFiles, Permissions.UploadFiles, Permissions.CreateFolders, Permissions.DeleteFiles, Permissions.MoveFiles, Permissions.RenameFolders, Permissions.DeleteFolders, Permissions.EditFiles } },
    { "Manage", new() { Permissions.ViewFiles, Permissions.UploadFiles, Permissions.CreateFolders, Permissions.DeleteFiles, Permissions.MoveFiles, Permissions.RenameFolders, Permissions.DeleteFolders, Permissions.EditFiles } }
};

        private static readonly Dictionary<string, List<string>> IssuePermissions = new()
{
    { "View", new() { Permissions.ViewIssues } },
    { "Create", new() { Permissions.ViewIssues, Permissions.CreateIssues } },
    { "Delete", new() { Permissions.ViewIssues, Permissions.CreateIssues, Permissions.EditIssues, Permissions.DeleteIssues } },
    { "Edit", new() { Permissions.ViewIssues, Permissions.CreateIssues, Permissions.EditIssues, Permissions.DeleteIssues } },
    { "Manage", new() { Permissions.ViewIssues, Permissions.CreateIssues, Permissions.EditIssues, Permissions.DeleteIssues } }
};

        private static readonly Dictionary<string, List<string>> ReviewPermissions = new()
{
    { "View", new() { Permissions.ViewReviews } },
    { "Create", new() { Permissions.ViewReviews, Permissions.CreateReviews, Permissions.SubmitReviews } },
    { "Delete", new() { Permissions.ViewReviews, Permissions.CreateReviews, Permissions.SubmitReviews, Permissions.ApproveRejectReviews } },
    { "Edit", new() { Permissions.ViewReviews, Permissions.CreateReviews, Permissions.SubmitReviews, Permissions.ApproveRejectReviews } },
    { "Manage", new() { Permissions.ViewReviews, Permissions.CreateReviews, Permissions.SubmitReviews, Permissions.ApproveRejectReviews } }
};


        public ProjectTeamController(AppDbContext context, IWebHostEnvironment env , UserManager<ApplicationUser> userManager , RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index(int Id)
        {
            var teams = _context.Teams
                .Where(t => t.ProjectId == Id)
                .Select(t => new TeamFolderViewModel
                {
                    Id = t.Id,
                    TeamName = t.Name,
                    ParentFolder = t.ParentFolder.Name,
                    MemberCount = t.Members.Count,
                }).ToList();

            var vm = new TeamListViewModel
            {
                ExistingTeams = teams,
                ProjectId = Id
            };
            ViewBag.Id = Id;
            return View(vm);
        }

        [HttpPost]
        public IActionResult AddTeam(string newTeamName, int projectId)
        {
            if (string.IsNullOrWhiteSpace(newTeamName))
                return BadRequest("Team name is required.");

            if (_context.Teams.Any(t => t.ProjectId == projectId && t.Name == newTeamName))
                return BadRequest("A team with this name already exists in this project.");

            var parentFolders = _context.Folders
     .Where(f => f.ProjectId == projectId)
     .Take(3)
     .ToList();

            foreach (var folder in parentFolders)
            {
                var team = new Team
                {
                    Name = newTeamName,
                    ProjectId = projectId,
                    ParentFolderId = folder.Id
                };

                _context.Teams.Add(team);
                _context.SaveChanges();

                string folderPath = Path.Combine("uploads", projectId.ToString(), folder.Id.ToString(), team.Id.ToString());
                string fullPath = Path.Combine(_env.WebRootPath, folderPath);
                Directory.CreateDirectory(fullPath);

                _context.Folders.Add(new Folder
                {
                    Name = newTeamName,
                    ProjectId = projectId,
                    ParentFolderId = folder.Id,
                    SubFolders = new List<Folder>(),
                    Documents = new List<Document>(),
                    CreatedBy = User.Identity.Name,
                });

                _context.SaveChanges();
            }


            var teams = _context.Teams
                       .Where(t => t.ProjectId == projectId)
                       .Select(t => new TeamFolderViewModel
                       {
                           Id = t.Id,
                           TeamName = t.Name,
                           ParentFolder = t.ParentFolder.Name,
                           MemberCount = t.Members.Count
                       }).ToList();

            return PartialView("_TeamsTable", teams);
        }
        [HttpGet]
        public IActionResult AddMembersModal(int teamId)
        {
            var team = _context.Teams
     .Include(t => t.Project)
     .Include(t => t.Members)
         .ThenInclude(tm => tm.User)
     .FirstOrDefault(t => t.Id == teamId);

            if (team == null)
                return NotFound();

            // Get the user IDs already in the team
            var teamMemberIds = team.Members
                .Select(tm => tm.UserId)
                .ToHashSet();

            // Get users in the same project but NOT in the team
            var projectUsers = _context.ApplicationUserRoles
                .Where(ur => ur.ProjectId == team.ProjectId && !teamMemberIds.Contains(ur.UserId))
                .Include(ur => ur.User)
                .Select(ur => new {
                    ur.UserId,
                    ur.User.UserName
                })
                .Distinct()
                .ToList();

            var permissionLevels = new[] { "View", "Create", "Delete" };

            var model = new AddTeamMembersViewModel
            {
                TeamId = team.Id,
                TeamName = team.Name,
                ProjectUsers = projectUsers.Select(u => new UserSelectionViewModel
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    FilePermission = "View", // default
                    ReviewPermission = "View", // default
                    IssuePermission = "View", // default
                }).ToList()
            };


            return PartialView("_AddMembersModal", model);
        }
        [HttpPost]
        public IActionResult AddMembers([FromForm] AddTeamMembersPostModel model)
        {
            if (model == null || model.SelectedUsers == null)
                return BadRequest("Invalid data.");

            var team = _context.Teams
                .FirstOrDefault(t => t.Id == model.TeamId);

            if (team == null)
                return NotFound();

            var filePermissions = new Dictionary<string, List<string>>
{
    { "View", new()
        {
            Permissions.ViewFiles
        }
    },
    { "Create", new()
        {
            Permissions.ViewFiles,
            Permissions.UploadFiles,
            Permissions.CreateFolders
        }
    },
    { "Delete", new()
        {
            Permissions.ViewFiles,
            Permissions.UploadFiles,
            Permissions.CreateFolders,
            Permissions.DeleteFiles,
            Permissions.MoveFiles,
            Permissions.RenameFolders,
            Permissions.DeleteFolders
        }
    },
    { "Edit", new()
        {
            Permissions.ViewFiles,
            Permissions.UploadFiles,
            Permissions.CreateFolders,
            Permissions.DeleteFiles,
            Permissions.MoveFiles,
            Permissions.RenameFolders,
            Permissions.DeleteFolders,
            Permissions.EditFiles // ← make sure this exists
        }
    },
    { "Manage", new()
        {
            Permissions.ViewFiles,
            Permissions.UploadFiles,
            Permissions.CreateFolders,
            Permissions.DeleteFiles,
            Permissions.MoveFiles,
            Permissions.RenameFolders,
            Permissions.DeleteFolders,
            Permissions.EditFiles,
        }
    }
};

            var issuePermissions = new Dictionary<string, List<string>>
{
    { "View", new()
        {
            Permissions.ViewIssues
        }
    },
    { "Create", new()
        {
            Permissions.ViewIssues,
            Permissions.CreateIssues
        }
    },
    { "Delete", new()
        {
            Permissions.ViewIssues,
            Permissions.CreateIssues,
            Permissions.EditIssues,
            Permissions.DeleteIssues
        }
    },
    { "Edit", new()
        {
            Permissions.ViewIssues,
            Permissions.CreateIssues,
            Permissions.EditIssues,
            Permissions.DeleteIssues
        }
    },
    { "Manage", new()
        {
            Permissions.ViewIssues,
            Permissions.CreateIssues,
            Permissions.EditIssues,
            Permissions.DeleteIssues,
        }
    }
};

            var reviewPermissions = new Dictionary<string, List<string>>
{
    { "View", new()
        {
            Permissions.ViewReviews
        }
    },
    { "Create", new()
        {
            Permissions.ViewReviews,
            Permissions.CreateReviews,
            Permissions.SubmitReviews
        }
    },
    { "Delete", new()
        {
            Permissions.ViewReviews,
            Permissions.CreateReviews,
            Permissions.SubmitReviews,
            Permissions.ApproveRejectReviews
        }
    },
    { "Edit", new()
        {
            Permissions.ViewReviews,
            Permissions.CreateReviews,
            Permissions.SubmitReviews,
            Permissions.ApproveRejectReviews,
        }
    },
    { "Manage", new()
        {
            Permissions.ViewReviews,
            Permissions.CreateReviews,
            Permissions.SubmitReviews,
            Permissions.ApproveRejectReviews,
       
        }
    }
};



            foreach (var userEntry in model.SelectedUsers.Where(u => u.IsSelected))
            {
                var user = _context.Users.Find(userEntry.UserId);
                if (user == null) continue;

                // File Permissions
                if (filePermissions.TryGetValue(userEntry.FilePermission, out var fileRoles))
                {
                    foreach (var perm in fileRoles)
                    {
                        AddUserRoleIfMissing(user.Id, team.ProjectId, perm, team.Id);
                    }
                }

                // Issue Permissions
                if (issuePermissions.TryGetValue(userEntry.IssuePermission, out var issueRoles))
                {
                    foreach (var perm in issueRoles)
                    {
                        AddUserRoleIfMissing(user.Id, team.ProjectId, perm, team.Id);
                    }
                }

                // Review Permissions
                if (reviewPermissions.TryGetValue(userEntry.ReviewPermission, out var reviewRoles))
                {
                    foreach (var perm in reviewRoles)
                    {
                        AddUserRoleIfMissing(user.Id, team.ProjectId, perm,team.Id);
                    }
                }

                // TeamMember record
                if (!_context.TeamMembers.Any(tm => tm.TeamId == model.TeamId && tm.UserId == user.Id))
                {
                    _context.TeamMembers.Add(new TeamMember
                    {
                        TeamId = model.TeamId,
                        UserId = user.Id
                    });
                }
            }


            _context.SaveChanges();
            return Ok("Members with permissions added.");
        }

        void AddUserRoleIfMissing(string userId, int projectId, string roleName, int teamId)
        {
            var role = _context.Roles.FirstOrDefault(r => r.Name == roleName);
            if (role == null) return;

            var hasRole = _context.ApplicationUserRoles.Any(ur =>
                ur.UserId == userId &&
                ur.ProjectId == projectId &&
                ur.TeamId == teamId && // check for specific team
                ur.RoleId == role.Id);

            if (!hasRole)
            {
                _context.ApplicationUserRoles.Add(new ApplicationUserRole
                {
                    UserId = userId,
                    ProjectId = projectId,
                    TeamId = teamId,
                    RoleId = role.Id
                });
            }
        }

        [HttpGet]
        public IActionResult TeamDetails(int teamId)
        {
            var team = _context.Teams
                .Include(t => t.Project)
                .Include(t => t.Members)
                .ThenInclude(u => u.User) // Include roles directly if using Identity
                .FirstOrDefault(t => t.Id == teamId);

            if (team == null)
                return NotFound();

            var projectId = team.ProjectId;

            var model = new TeamDetailsViewModel
            {
                TeamId = team.Id,
                TeamName = team.Name,
                ProjectName = team.Project.Name,
                Members = team.Members.Select(m =>
                {
                    var userRoles = _context.ApplicationUserRoles
                        .Where(r => r.UserId == m.UserId && r.ProjectId == projectId && r.TeamId == team.Id) // Filter by team
                        .Include(r => r.Role)
                        .Select(r => r.Role.Name)
                        .ToList();

                    return new TeamMemberViewModel
                    {
                        UserId = m.UserId,
                        UserName = m.User.UserName,
                        Permissions = userRoles
                    };
                }).ToList()
            };

            ViewBag.ProId = team.ProjectId;
            return View("TeamDetails", model);
        }
        [HttpPost]
        public IActionResult DeleteTeam(int id)
        {
            var team = _context.Teams
                .Include(t => t.Members)
                .Include(t => t.ParentFolder)
                .Include(t => t.Folders)
                .FirstOrDefault(t => t.Id == id);

            if (team == null)
                return NotFound("Team not found.");

            int projectId = team.ProjectId;
            int parentFolderId = team.ParentFolderId;

            // 🧹 1. Remove team members
            var teamMembers = _context.TeamMembers.Where(tm => tm.TeamId == id).ToList();
            _context.TeamMembers.RemoveRange(teamMembers);

            // 🧹 2. Delete folder from DB that was created for this team
            var teamFolder = _context.Folders
                .FirstOrDefault(f => f.ProjectId == projectId && f.ParentFolderId == parentFolderId && f.Name == team.Name);
            if (teamFolder != null)
                _context.Folders.Remove(teamFolder);

            // 🧹 3. Delete the physical folder from wwwroot/uploads
            string folderPath = Path.Combine("uploads", projectId.ToString(), parentFolderId.ToString(), team.Id.ToString());
            string fullPath = Path.Combine(_env.WebRootPath, folderPath);
            if (Directory.Exists(fullPath))
                Directory.Delete(fullPath, recursive: true);

            // 🧹 4. Delete the team itself
            _context.Teams.Remove(team);
            _context.SaveChanges();

            return Ok();
        }

        [HttpPost]
        public IActionResult RemoveMember(int teamId, string userId)
        {
            var teamMember = _context.TeamMembers
                .FirstOrDefault(tm => tm.TeamId == teamId && tm.UserId == userId);

            if (teamMember != null)
                _context.TeamMembers.Remove(teamMember);

            var team = _context.Teams.Include(t => t.Project).FirstOrDefault(t => t.Id == teamId);
            if (team != null)
            {
                int projectId = team.ProjectId;

                var userRoles = _context.ApplicationUserRoles
                    .Where(r => r.UserId == userId && r.ProjectId == projectId && r.TeamId == teamId)
                    .ToList();

                _context.ApplicationUserRoles.RemoveRange(userRoles);
            }

            _context.SaveChanges();
            return Ok();
        }
        [HttpGet]
        public IActionResult EditMemberPermissions(int teamId, string userId)
        {
            var team = _context.Teams
                .Include(t => t.Project)
                .FirstOrDefault(t => t.Id == teamId);

            if (team == null) return NotFound();

            var user = _context.Users.Find(userId);
            if (user == null) return NotFound();

            var userRoles = _context.ApplicationUserRoles
                .Where(r => r.UserId == userId && r.ProjectId == team.ProjectId)
                .Include(r => r.Role)
                .Select(r => r.Role.Name)
                .ToList();

            var model = new EditMemberPermissionsViewModel
            {
                TeamId = teamId,
                UserId = userId,
                UserName = user.UserName,
                Permissions = new Dictionary<string, string>
        {
            { "File", GetPermissionLevel(userRoles, FilePermissions) },
            { "Issue", GetPermissionLevel(userRoles, IssuePermissions) },
            { "Review", GetPermissionLevel(userRoles, ReviewPermissions) }
        }
            };

            return PartialView("_EditPermissionsModal", model);
        }

        private string GetPermissionLevel(List<string> userRoles, Dictionary<string, List<string>> permissionMap)
        {
            foreach (var kvp in permissionMap.Reverse())
            {
                if (kvp.Value.All(r => userRoles.Contains(r)))
                    return kvp.Key;
            }
            return "View";
        }
        [HttpPost]
        public IActionResult UpdateMemberPermissions([FromForm] EditMemberPermissionsViewModel model)
        {
            var team = _context.Teams.FirstOrDefault(t => t.Id == model.TeamId);
            if (team == null) return NotFound();

            var userRoles = _context.ApplicationUserRoles
                .Where(r => r.UserId == model.UserId && r.ProjectId == team.ProjectId && r.TeamId == team.Id)
                .ToList();

            _context.ApplicationUserRoles.RemoveRange(userRoles);

            foreach (var permCategory in model.Permissions)
            {
                var roleList = permCategory.Key switch
                {
                    "File" => FilePermissions[permCategory.Value],
                    "Issue" => IssuePermissions[permCategory.Value],
                    "Review" => ReviewPermissions[permCategory.Value],
                    _ => new List<string>()
                };

                foreach (var roleName in roleList)
                {
                    var role = _context.Roles.FirstOrDefault(r => r.Name == roleName);
                    if (role != null)
                    {
                        _context.ApplicationUserRoles.Add(new ApplicationUserRole
                        {
                            UserId = model.UserId,
                            ProjectId = team.ProjectId,
                            RoleId = role.Id,
                            TeamId = team.Id
                        });
                    }
                }
            }

            _context.SaveChanges();
            return Ok();
        }

    }
}
