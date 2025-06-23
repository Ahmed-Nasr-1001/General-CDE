using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer;
using DataLayer.Models;
using DataLayer.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogic.Repository.RepositoryClasses
{
    public class ProjectRepository : GenericRepository<Project>, IProjetcRepository
    {
        private readonly AppDbContext context;

        public ProjectRepository(AppDbContext context) : base(context)
        {
            this.context = context;
        }

        public List<string> GetCurrencyValues()
        {
            return Enum.GetNames(typeof(Currency)).ToList();
        }

        public List<Project> GetPaginatedProjects(int page, int pageSize, string searchText = null, bool showArchived = false)
        {
            var query = context.Projects.AsQueryable();

            // Always filter by archive status
            query = query.Where(p => p.IsArchived == showArchived);

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(x => x.Name.ToLower().Contains(searchText.ToLower()) || x.ProjectNumber.Contains(searchText));
            }

            return query.OrderBy(x => x.Id)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
        }
        public List<string> GetProjectTypeValues()
        {
            return Enum.GetNames(typeof(ProjectType)).ToList();
        }

        public int GetProjectsCount(string searchText = null, bool showArchived = false)
        {
            var query = context.Projects.AsQueryable();

            query = query.Where(p => p.IsArchived == showArchived);

            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(x => x.Name.Contains(searchText) || x.ProjectNumber.Contains(searchText));
            }

            return query.Count();
        }
    }
}
