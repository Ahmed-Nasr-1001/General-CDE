using ACC.Services;
using ACC.ViewModels;
using BusinessLogic.Repository.RepositoryClasses;
using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer;
using DataLayer.Models;
using DataLayer.Models.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ACC.Controllers
{
    public class ProjectIssueController : Controller
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IIssueRepository issueRepository;
        private readonly IssueReviewersService issueReviewersService;
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly INotificationService _notificationService;
        private readonly string _uploadsPath = Path.Combine("wwwroot", "Uploads");
        private readonly string _convertedPath;
        public ProjectIssueController(IDocumentRepository documentRepository,IIssueRepository issueRepository, IssueReviewersService issueReviewersService, AppDbContext context, IWebHostEnvironment env, UserManager<ApplicationUser> userManager, INotificationService notificationService)
        {
            _documentRepository = documentRepository;
            this.issueRepository = issueRepository;
            this.issueReviewersService = issueReviewersService;
            _context = context;
            _env = env;
            this.userManager = userManager;
            this._notificationService = notificationService;
            _convertedPath = Path.Combine("wwwroot", "Converted");

        }

        public async Task<IActionResult> Index(int id, string searchTerm, string status, int page = 1)
        {
            int pageSize = 6;
            var currentUser = await userManager.GetUserAsync(User);
            List<Issue> issues = issueRepository.GetIssuesByUserId(currentUser.Id, id);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                issues = issues
                    .Where(i => i.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                             || i.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                issues = issues
                    .Where(i => string.Equals(i.Status.ToString(), status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var totalIssues = issues.Count();

            // ✅ حدد الصفحة الحالية فقط
            var pagedIssues = issues
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var issueViewModels = pagedIssues.Select(i => new ProjectIssueVM
            {
                Id = i.Id,
                Title = i.Title,
                Description = i.Description,
                Category = i.Category,
                Type = i.Type,
                Priority = i.Priority,
                Status = i.Status,
                ProjectId = i.ProjectId,
                CreatedAt = i.CreatedAt,
                DocumentId = i.Document?.Id,
                FilePath = i.Document?.Versions?.OrderByDescending(v => v.VersionNumber).FirstOrDefault()?.FilePath,
                InitiatorId = i.InitiatorID
            }).ToList();

            ViewBag.Id = id;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SelectedStatus = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalIssues / pageSize);
            ViewBag.CurrentPage = page;

            var currentUserId = userManager.GetUserId(User);

            var unread = await _context.IssueNotifications
                .Where(n => n.ReceiverId == currentUserId && !n.IsRead)
                .GroupBy(n => n.IssueId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.UnreadCommentsCount = unread;

            return View(issueViewModels);
        }



        public async Task<IActionResult> Create(int? id =1) 
        {
            var currentUser = await userManager.GetUserAsync(User);

            var vm = new ProjectIssueVM
            {
                ProjectId = id ?? 1, 
                applicationUsers = userManager.Users
                    .Where(u => u.Id != currentUser.Id)
                    .ToList()
            };

            return View("Create", vm);
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProjectIssueVM model)
        {
            var CurrentUser = await userManager.GetUserAsync(User);

            var issue = new Issue
            {
                Title = model.Title,
                Description = model.Description,
                Category = model.Category,
                Type = model.Type,
                Priority = model.Priority,
                Status = model.Status,
                ProjectId = model.ProjectId,
                InitiatorID = CurrentUser.Id,
            };

            issueRepository.Insert(issue);
            issueRepository.Save();

            foreach (var id in model.SelectedReviewerIds)
            {
                IssueReviwers issueReviwers = new IssueReviwers()
                {
                    IssueId = issue.Id,
                    ReviewerId = id
                };
                issueReviewersService.Insert(issueReviwers);
            }
            issueReviewersService.Save();

            //For notifications
            /////
            await _notificationService.NotifyIssueCreatedAsync(issue, model.ProjectId);
            /////

            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                int projectId = model.ProjectId;
                var project = await _context.Projects.FindAsync(projectId);
                if (project == null)
                {
                    ModelState.AddModelError("ProjectId", "Project not found.");
                    return View(model);
                }

                var extension = Path.GetExtension(model.Attachment.FileName).ToLower();

                // Get or create "Work In Progress" folder
                var wipFolder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.ProjectId == projectId && f.Name == "Work In Progress" && f.ParentFolderId == null);
                

                // Get or create "Issues" subfolder
                var issuesFolder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.ProjectId == projectId && f.Name == "Issues" && f.ParentFolderId == wipFolder.Id);
                if (issuesFolder == null)
                {
                    issuesFolder = new Folder
                    {
                        Name = "Issues",
                        ParentFolderId = wipFolder.Id,
                        ProjectId = projectId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = User.Identity.Name ?? "System"
                    };
                    _context.Folders.Add(issuesFolder);
                    await _context.SaveChangesAsync();
                }

                var issueFolderPath = Path.Combine(_env.WebRootPath, "uploads", projectId.ToString(), wipFolder.Id.ToString(), issuesFolder.Id.ToString());
                Directory.CreateDirectory(issueFolderPath);

                if (issuesFolder == null)
                {
                    return NotFound("Folder not found.");
                }

                

                // ISO 19650 file name pattern
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(model.Attachment.FileName);



                var filePath = Path.Combine(issueFolderPath, model.Attachment.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Attachment.CopyToAsync(stream);
                }

                var document = await _documentRepository.GetAllQueryable()
                    .Include(d => d.Versions)
                    .FirstOrDefaultAsync(d => d.FolderId == issuesFolder.Id && d.Name == Path.GetFileNameWithoutExtension(model.Attachment.FileName));

                if (document == null)
                {
                    document = new Document
                    {
                        Name = Path.GetFileNameWithoutExtension(model.Attachment.FileName),
                        FileType = extension,
                        FolderId = issuesFolder.Id,
                        ProjectId = projectId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = User.Identity.Name ?? "System",
                        Versions = new List<DocumentVersion>()
                    };

                    _documentRepository.Insert(document); // This should set document.Id after Save()
                }

                var version = new DocumentVersion
                {
                    FilePath = filePath,
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy = User.Identity.Name ?? "System",
                    VersionNumber = document.Versions.Count + 1
                };

                document.Versions.Add(version);
                _documentRepository.Save(); // This persists the document and its versions

                
                // Update the issue with the document ID
                var existingIssue = issueRepository.GetById(issue.Id);
                if (existingIssue == null)
                    throw new Exception("Issue not found.");

                existingIssue.DocumentId = document.Id;
                issueRepository.Update(existingIssue);
                issueRepository.Save();

            }

            //snapshot 
            if (!string.IsNullOrWhiteSpace(model.ScreenshotPath))
            {
               
                var fullFilePath = Path.Combine(_env.WebRootPath, model.ScreenshotPath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (System.IO.File.Exists(fullFilePath))
                {
                    var projectId = model.ProjectId;

                  
                    var wipFolder = await _context.Folders
                        .FirstOrDefaultAsync(f => f.ProjectId == projectId && f.Name == "Work In Progress" && f.ParentFolderId == null);
                    if (wipFolder == null)
                    {
                        wipFolder = new Folder
                        {
                            Name = "Work In Progress",
                            ProjectId = projectId,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = User.Identity?.Name ?? "System"
                        };
                        _context.Folders.Add(wipFolder);
                        await _context.SaveChangesAsync();
                    }

                    var snapshotsFolder = await _context.Folders
                        .FirstOrDefaultAsync(f => f.ProjectId == projectId && f.Name == "Snapshots" && f.ParentFolderId == wipFolder.Id);
                    if (snapshotsFolder == null)
                    {
                        snapshotsFolder = new Folder
                        {
                            Name = "Snapshots",
                            ParentFolderId = wipFolder.Id,
                            ProjectId = projectId,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = User.Identity?.Name ?? "System"
                        };
                        _context.Folders.Add(snapshotsFolder);
                        await _context.SaveChangesAsync();
                    }

                 
                    var document = new Document
                    {
                        Name = "Snapshot_" + DateTime.Now.Ticks,
                        FileType = Path.GetExtension(fullFilePath),
                        FolderId = snapshotsFolder.Id,
                        ProjectId = projectId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = User.Identity?.Name ?? "System",
                        Versions = new List<DocumentVersion>()
                    };

                    var version = new DocumentVersion
                    {
                        FilePath = fullFilePath,
                        UploadedAt = DateTime.UtcNow,
                        UploadedBy = User.Identity?.Name ?? "System",
                        VersionNumber = 1
                    };

                    document.Versions.Add(version);
                    _context.Documents.Add(document);
                    await _context.SaveChangesAsync();

                   
                    issue.DocumentId = document.Id;
                    issueRepository.Update(issue);
                    issueRepository.Save();
                }
            }


            return RedirectToAction("Index", new { id = model.ProjectId });
        }
        [HttpGet]
        public async Task<IActionResult> OpenFile(int documentId)
        {
            var document = await _documentRepository.GetAllQueryable()
                .Include(d => d.Versions.OrderByDescending(v => v.VersionNumber))
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null || document.Versions == null || !document.Versions.Any())
            {
                return NotFound(new { message = "Document or version not found." });
            }

            var latestVersion = document.Versions.First();
            var filePath = latestVersion.FilePath;


            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { message = "File not found on server." });
            }
            if (document.FileType.ToLower() == ".ifc")
            {
                var relaPath = filePath.Replace(_env.WebRootPath, "").Replace("\\", "/").TrimStart('/');


                return Ok(new
                {
                    projectId = document.ProjectId,
                    fileUrl = $"/{relaPath}", // e.g., /copied-ifc-files/filename.ifc
                    fileType = document.FileType.ToLower()
                });
            }

            if (document.FileType.ToLower() == ".dwg")
            {
                try
                {
                    // Ensure directories exist
                    Directory.CreateDirectory(_uploadsPath);
                    Directory.CreateDirectory(_convertedPath);

                    // Clear old files to avoid conflicts (optional, consider optimizing)
                    foreach (var oldFile in Directory.GetFiles(_uploadsPath))
                    {
                        System.IO.File.Delete(oldFile);
                    }
                    foreach (var oldFile in Directory.GetFiles(_convertedPath))
                    {
                        System.IO.File.Delete(oldFile);
                    }

                    // Generate unique filenames
                    var baseName = Path.GetFileNameWithoutExtension(filePath);
                    var uniqueName = $"{baseName}_{Guid.NewGuid():N}";
                    var dwgFile = Path.Combine(_uploadsPath, uniqueName + ".dwg");

                    // Copy DWG to uploads folder
                    using (var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (var destStream = new FileStream(dwgFile, FileMode.Create, FileAccess.Write))
                    {
                        await sourceStream.CopyToAsync(destStream);
                    }

                    // External tools paths
                    var odaPath = @"C:\Tools\ODAFileConverter\ODAFileConverter.exe";
                    var inkscapePath = @"C:\Program Files\Inkscape\bin\inkscape.exe";

                    if (!System.IO.File.Exists(odaPath))
                    {
                        return StatusCode(500, new { message = $"ODA converter not found at: {odaPath}" });
                    }
                    if (!System.IO.File.Exists(inkscapePath))
                    {
                        return StatusCode(500, new { message = $"Inkscape not found at: {inkscapePath}" });
                    }

                    // Step 1: Convert DWG to DXF
                    var dxfFile = Path.Combine(_convertedPath, uniqueName + ".dxf");
                    var odaArgs = $"\"{Path.GetFullPath(_uploadsPath)}\" \"{Path.GetFullPath(_convertedPath)}\" ACAD2018 DXF 0 1";

                    var odaProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = odaPath,
                        Arguments = odaArgs,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                    odaProcess?.WaitForExit();

                    if (!System.IO.File.Exists(dxfFile))
                    {
                        return StatusCode(500, new { message = "DXF file not generated. ODA conversion failed." });
                    }

                    // Step 2: Convert DXF to PDF
                    var pdfFile = Path.Combine(_convertedPath, uniqueName + ".pdf");
                    var inkscapeArgs = $"\"{dxfFile}\" --export-filename=\"{pdfFile}\" --export-area-drawing --export-type=pdf";

                    var inkProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = inkscapePath,
                        Arguments = inkscapeArgs,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    });
                    inkProcess?.WaitForExit();

                    if (!System.IO.File.Exists(pdfFile))
                    {
                        return StatusCode(500, new { message = "PDF file not created. Inkscape conversion failed." });
                    }

                    // Construct relative URL for the PDF
                    var relativePath = pdfFile.Replace(_env.WebRootPath, "").Replace("\\", "/").TrimStart('/');
                    return Ok(new
                    {
                        fileUrl = $"/{relativePath}", // e.g., /converted/uniqueName.pdf
                        fileType = ".pdf" // Return as PDF since DWG is converted
                    });
                }
                catch (Exception ex)
                {
                    System.IO.File.AppendAllText("log.txt", $"[{DateTime.Now}] Error: {ex}\n");
                    return StatusCode(500, new { message = $"Unexpected error: {ex.Message}" });
                }
            }

            // Handle PDFs and images
            var relPath = filePath.Replace(_env.WebRootPath, "").Replace("\\", "/").TrimStart('/');
            return Ok(new
            {
                fileUrl = $"/{relPath}", // e.g., /uploads/1/2/document.pdf
                fileType = document.FileType.ToLower()
            });
        }
        // Helper method to sanitize folder names
        private string CleanFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Replace(" ", "_").Trim();
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var issue = issueRepository.GetById(id);
            if (issue == null)
                return NotFound();

            var currentUser = await userManager.GetUserAsync(User);

            var model = new ProjectIssueVM
            {
                Id = issue.Id,
                Title = issue.Title,
                Description = issue.Description,
                Category = issue.Category,
                Type = issue.Type,
                Priority = issue.Priority,
                Status = issue.Status,
                ProjectId = issue.ProjectId,
                DocumentId = issue.DocumentId,
                applicationUsers = userManager.Users
                    .Where(u => u.Id != currentUser.Id)
                    .ToList()
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(ProjectIssueVM model)
        {


            var issue = issueRepository.GetById(model.Id);
            if (issue == null)
                return NotFound();

            var previousStatus = issue.Status;

            issue.Title = model.Title;
            issue.Description = model.Description;
            issue.Category = model.Category;
            issue.Type = model.Type;
            issue.Priority = model.Priority;
            issue.Status = model.Status;
            issue.ProjectId = model.ProjectId;
            issue.DocumentId = model.DocumentId;

            var extension = model.Attachment != null ? Path.GetExtension(model.Attachment.FileName).ToLower() : null;

            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                var projectId = model.ProjectId;

                // Get or create Work In Progress > Issues
                var wipFolder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.ProjectId == projectId && f.Name == "Work In Progress" && f.ParentFolderId == null);

                if (wipFolder == null)
                {
                    wipFolder = new Folder { Name = "Work In Progress", ProjectId = projectId, CreatedAt = DateTime.UtcNow, CreatedBy = User.Identity.Name ?? "System" };
                    _context.Folders.Add(wipFolder);
                    await _context.SaveChangesAsync();
                }

                var issuesFolder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.ProjectId == projectId && f.Name == "Issues" && f.ParentFolderId == wipFolder.Id);

                if (issuesFolder == null)
                {
                    issuesFolder = new Folder { Name = "Issues", ProjectId = projectId, ParentFolderId = wipFolder.Id, CreatedAt = DateTime.UtcNow, CreatedBy = User.Identity.Name ?? "System" };
                    _context.Folders.Add(issuesFolder);
                    await _context.SaveChangesAsync();
                }

                // Upload to: uploads/{ProjectId}/{WIP}/{Issues}/{IssueId}_{Title}
                var folderName = $"{issue.Id}_{CleanFileName(issue.Title)}";
                var issueFolderPath = Path.Combine(_env.WebRootPath, "uploads", projectId.ToString(), wipFolder.Id.ToString(), issuesFolder.Id.ToString(), folderName);
                Directory.CreateDirectory(issueFolderPath);

                var fileName = $"{Guid.NewGuid()}_{model.Attachment.FileName}";
                var filePath = Path.Combine(issueFolderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Attachment.CopyToAsync(stream);
                }

                var document = new Document
                {
                    Name = Path.GetFileNameWithoutExtension(model.Attachment.FileName),
                    FileType = extension,
                    FolderId = issuesFolder.Id,
                    ProjectId = projectId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity.Name ?? "System",
                    Versions = new List<DocumentVersion>()
                };

                var version = new DocumentVersion
                {
                    FilePath = filePath,
                    UploadedAt = DateTime.UtcNow,
                    UploadedBy = User.Identity.Name ?? "System",
                    VersionNumber = 1
                };

                document.Versions.Add(version);
                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                issue.DocumentId = document.Id;
            }

            // ✅ Move to Archive if status changed to Closed
            if (previousStatus != IssueStatus.Closed && model.Status == IssueStatus.Closed && issue.DocumentId != null)
            {
                var document = await _context.Documents
                    .Include(d => d.Versions)
                    .FirstOrDefaultAsync(d => d.Id == issue.DocumentId);

                var oldVersion = document?.Versions?.FirstOrDefault();
                if (oldVersion != null && System.IO.File.Exists(oldVersion.FilePath))
                {
                    // Get or create Archive > Issues
                    var archiveFolder = await _context.Folders
                        .FirstOrDefaultAsync(f => f.ProjectId == issue.ProjectId && f.Name == "Archive" && f.ParentFolderId == null);
                    if (archiveFolder == null)
                    {
                        archiveFolder = new Folder { Name = "Archive", ProjectId = issue.ProjectId, CreatedAt = DateTime.UtcNow, CreatedBy = User.Identity.Name ?? "System" };
                        _context.Folders.Add(archiveFolder);
                        await _context.SaveChangesAsync();
                    }

                    var archiveIssuesFolder = await _context.Folders
                        .FirstOrDefaultAsync(f => f.Name == "Issues" && f.ParentFolderId == archiveFolder.Id && f.ProjectId == issue.ProjectId);
                    if (archiveIssuesFolder == null)
                    {
                        archiveIssuesFolder = new Folder { Name = "Issues", ParentFolderId = archiveFolder.Id, ProjectId = issue.ProjectId, CreatedAt = DateTime.UtcNow, CreatedBy = User.Identity.Name ?? "System" };
                        _context.Folders.Add(archiveIssuesFolder);
                        await _context.SaveChangesAsync();
                    }

                
                    var archiveIssueFolder = Path.Combine(_env.WebRootPath, "uploads", issue.ProjectId.ToString(), archiveFolder.Id.ToString(), archiveIssuesFolder.Id.ToString(), $"{issue.Id}_{CleanFileName(issue.Title)}");
                    Directory.CreateDirectory(archiveIssueFolder);

                    var oldPath = oldVersion.FilePath;
                    var newFilePath = Path.Combine(archiveIssueFolder, Path.GetFileName(oldPath));
                    System.IO.File.Copy(oldPath, newFilePath, true);

                    
                    oldVersion.FilePath = newFilePath;

                  
                    var oldIssueFolder = Path.GetDirectoryName(oldPath);
                    if (Directory.Exists(oldIssueFolder) && !Directory.EnumerateFileSystemEntries(oldIssueFolder).Any())
                    {
                        Directory.Delete(oldIssueFolder, true);
                    }

                    await _context.SaveChangesAsync();
                }
            }

            issueRepository.Update(issue);
            issueRepository.Save();

            // For notifications
            //////////
            try
            {
                if (previousStatus != model.Status)
                {
                    await _notificationService.NotifyIssueStatusChangedAsync(issue, previousStatus.ToString());
                }
                else
                {
                    await _notificationService.NotifyIssueUpdatedAsync(issue, "Issue details have been updated.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception but don't fail the issue update
                // You can add logging here if needed
            }

            /////////////
            

            return RedirectToAction("Index", new { id = issue.ProjectId });
        }

      


        public IActionResult Delete(int id)
        {
            var issue = issueRepository.GetById(id);
            issueRepository.Delete(issue);
            return RedirectToAction("Index", new { id = issue.ProjectId });
        }

        //viewers snapshot
        [HttpGet]
        public async Task<IActionResult> CreateFromSnapshot(int projectId, string imagePath)
        {
            var currentUser = await userManager.GetUserAsync(User);

           
            var ifcFile = await _context.IfcFiles.FirstOrDefaultAsync(f => f.ProjectId == projectId);

            var vm = new ProjectIssueVM
            {
                ProjectId = projectId,
                ScreenshotPath = imagePath,
                FileId = ifcFile?.Id ?? 0, 
                applicationUsers = userManager.Users
                    .Where(u => u.Id != currentUser.Id)
                    .ToList()
            };

            return View("Create", vm);
        }





    }
}