using ACC.ViewModels.ReviewsVM;
using ACC.ViewModels.WorkflowVM;
using DataLayer;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace ACC.Services
{
    public class FolderService
    {
        private readonly AppDbContext _context;

        public FolderService(AppDbContext context)
        {
            _context = context;
        }

        public  List<FolderWithDocumentsVM> GetFolderWithDocumentTree()
        {
            var allFolders = _context.Folders
                .Include(f => f.Documents)
                .ToList();

            return BuildFolderWithDocumentTree(allFolders, null);
        }

        private  List<FolderWithDocumentsVM> BuildFolderWithDocumentTree(List<Folder> allFolders, int? parentId)
        {
            return allFolders
                .Where(f => f.ParentFolderId == parentId)
                .Select(f => new FolderWithDocumentsVM
                {
                    Id = f.Id,
                    Name = f.Name,
                    Documents = f.Documents?.Select(d => new DocumentVM
                    {
                        Id = d.Id,
                        Name = d.Name
                    }).ToList() ?? new List<DocumentVM>(),
                    Children = BuildFolderWithDocumentTree(allFolders, f.Id)
                })
                .ToList();
        }


        public List<FolderVM> GetFolderTree()
        {
            var allFolders = _context.Folders
                .ToList();

            return BuildFolderTree(allFolders, null);
        }

        private List<FolderVM> BuildFolderTree(List<Folder> allFolders, int? parentId)
        {
            return allFolders
                .Where(f => f.ParentFolderId == parentId)
                .Select(f => new FolderVM
                {
                    Id = f.Id,
                    Name = f.Name,
                    Children = BuildFolderTree(allFolders, f.Id)
                })
                .ToList();
        }

        public string FolderName(int folderId)
        {
            return _context.Folders.FirstOrDefault(f => f.Id == folderId).Name;
                
        }
    }
}
