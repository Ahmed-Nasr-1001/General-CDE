using System.Security.Claims;
using ACC.ViewModels;
using ACC.ViewModels.IFCViewerVM;
using BusinessLogic.Services;
using Helpers;
using Microsoft.AspNetCore.Mvc;

namespace ACC.Controllers.Viewer
{
    public class IfcViewerController : Controller
    {
        private readonly IfcFileService _ifcFileService;

        public IfcViewerController(IfcFileService ifcFileService)
        {
            _ifcFileService = ifcFileService;
        }

        public async Task<IActionResult> Index(int? fileId, int? projectId  , string? filePath)
        {
            var model = new IfcViewerModel
            {
                FileId = fileId ?? 0,
                ProjectId = projectId ?? 1,
                FilePath = filePath,
                AvailableFiles = await _ifcFileService.GetIfcFilesByProjectIdAsync(projectId?? 1),
                ViewerJsFile = GetBuiltViewerJsFileName()
            };
            return View(model);
        }

        private string GetBuiltViewerJsFileName()
        {
            var distFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "dist", "assets");

            var files = Directory.GetFiles(distFolder, "index-*.js");

            var biggestFile = files.OrderByDescending(f => new FileInfo(f).Length).FirstOrDefault();

            return biggestFile is not null ? "/dist/assets/" + Path.GetFileName(biggestFile) : "";
        }


        [HttpPost]
        public async Task<IActionResult> UploadIfcFile(IFormFile file, int projectId)
        {
            if (!Helpers.FileHelper.IsValidIfcFile(file))
                return BadRequest("Invalid IFC file.");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ifcFile = await _ifcFileService.UploadIfcFileAsync(file, projectId, userId);
            return Json(new { success = true, fileId = ifcFile.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetIfcFile(int id)
        {
            var ifcFile = await _ifcFileService.GetIfcFileByIdAsync(id);
            if (ifcFile == null)
                return NotFound();
            var fileStream = System.IO.File.OpenRead(ifcFile.FilePath);
            return File(fileStream, "application/octet-stream", ifcFile.FileName);
        }
    }

   
}
