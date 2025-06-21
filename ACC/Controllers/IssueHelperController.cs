using Microsoft.AspNetCore.Mvc;

namespace ACC.Controllers
{
    [Route("[controller]/[action]")]
    public class IssueHelperController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public IssueHelperController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost]
        public async Task<IActionResult> UploadSnapshot(IFormFile snapshot, int projectId, int fileId, string? viewStateJson)
        {
            // Save snapshot image...
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(snapshot.FileName);
            var imagePath = $"/uploads/snapshots/{projectId}/{fileName}";
            var folderPath = Path.Combine(_env.WebRootPath, "uploads", "snapshots", projectId.ToString());
            Directory.CreateDirectory(folderPath);
            var imagePathFull = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(imagePathFull, FileMode.Create))
            {
                await snapshot.CopyToAsync(stream);
            }

            // Save view state
            if (!string.IsNullOrWhiteSpace(viewStateJson))
            {
                var jsonPath = Path.Combine(folderPath, Path.GetFileNameWithoutExtension(fileName) + ".json");
                await System.IO.File.WriteAllTextAsync(jsonPath, viewStateJson);
            }

            return Json(new { success = true, imagePath, projectId, fileId });
        }


    }

}
