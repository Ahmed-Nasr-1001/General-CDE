using DataLayer;
using DataLayer.Models;
using DataLayer.Models.ClassHelper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ACC.Services
{
    public class UserRoleService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserRoleService(AppDbContext context , UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
        public void Delete(ApplicationUserRole obj)
        {
            var model = _context.Set<ApplicationUserRole>().Remove(obj);
        }

        public IList<ApplicationUserRole> GetAll()
        {
            return _context.Set<ApplicationUserRole>().Include(i=>i.Role).Include(i=>i.User).ToList();
        }

        public ApplicationUserRole GetByUserId(string UserId , int? projId = null)
        {
            return _context.Set<ApplicationUserRole>().Include(u=>u.Role).FirstOrDefault(u=>u.UserId == UserId && u.ProjectId == projId);
        }

        public void Insert(ApplicationUserRole obj)
        {
            _context.Set<ApplicationUserRole>().Add(obj);
        }

        public void Update(ApplicationUserRole obj)
        {
            _context.Set<ApplicationUserRole>().Update(obj);
        }
        public void Save()
        {
            _context.SaveChanges();
        }

        public List<string> GetMembers(int projId)
        {
           return _context.ApplicationUserRoles
                .Where(pm => pm.ProjectId == projId)
                .Select(pm => pm.UserId)
                .ToList();
        }

        public List<ApplicationRole> AllGlobalAccessLevels()
        {
            return _context.Set<ApplicationRole>().Where(i => i.GloblaAccesLevel == true).ToList();
        }
        public ApplicationRole GetGlobalAccessLevel(string Userid)
        {
            return _context.Set<ApplicationUserRole>().Include(i=>i.Role).FirstOrDefault(i=>i.UserId == Userid && i.Role.GloblaAccesLevel == true).Role;
        }

        public async Task<ApplicationUser?> GetUserWithGAL(string name)
        {
            var userId = _context.Set<ApplicationUserRole>()
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .Where(ur => ur.User.UserName == name && ur.Role.GloblaAccesLevel == true)
                .Select(ur => ur.UserId)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(userId))
                return null;

            return await _userManager.FindByIdAsync(userId);
        }

        //Project Access Level
        public List<ApplicationRole> AllProjectAccessLevels()
        {
            return _context.Set<ApplicationRole>().Where(i => i.ProjectAccessLevel == true).ToList();
        }
        public ApplicationRole GetProjectAccessLevel(string Userid, int ProjId)
        {
            return _context.Set<ApplicationUserRole>().Include(i => i.Role).FirstOrDefault(i => i.UserId == Userid && i.ProjectId == ProjId && i.Role.ProjectAccessLevel == true)?.Role;
        }

        public async Task<ApplicationUser> GetUserWithProjectAccessLevel(string Name, int ProjId)
        {
            string userId = _context.Set<ApplicationUserRole>().Include(u => u.User).Include(u => u.Role).FirstOrDefault(i => i.User.UserName == Name && i.ProjectId == ProjId && i.Role.ProjectAccessLevel == true).UserId;
            return await _userManager.FindByIdAsync(userId);
        }

        //Position
        public List<ApplicationRole> AllProjectPositions()
        {
            return _context.Set<ApplicationRole>().Where(i => i.ProjectPosition == true).ToList();
        }
        public ApplicationRole GetPosition(string Userid , int ProjId)
        {
            return _context.Set<ApplicationUserRole>().Include(i => i.Role).FirstOrDefault(i => i.UserId == Userid && i.ProjectId==ProjId && i.Role.ProjectPosition == true).Role;
        }

        public async Task<ApplicationUser> GetUserWithPosition(string Name , int ProjId)
        {
            string userId = _context.Set<ApplicationUserRole>().Include(u => u.User).Include(u => u.Role).FirstOrDefault(i => i.User.UserName == Name && i.ProjectId==ProjId && i.Role.ProjectPosition == true).UserId;
            return await _userManager.FindByIdAsync(userId);
        }

        public bool HasGlobalAccess(string UserId, string GlobalAccessLevel)
        {
          return _context.Set<ApplicationUserRole>().Any(i => i.UserId == UserId && i.Role.Name == GlobalAccessLevel);

        }
        public bool HasProjectAccess(string UserId,int projectId, string GlobalAccessLevel)
        {
            return _context.Set<ApplicationUserRole>().Any(i => i.UserId == UserId && i.Role.Name == GlobalAccessLevel && i.ProjectId==projectId);

        }

    }
}

