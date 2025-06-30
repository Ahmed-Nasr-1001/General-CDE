using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer.Models.Enums;
using DataLayer.Models;
using DataLayer;
using Microsoft.EntityFrameworkCore;


namespace BusinessLogic.Repository.RepositoryClasses
{
    public class CompanyRepository : GenericRepository<Company>, ICompanyRepository
    {

        AppDbContext Context;
        public CompanyRepository(AppDbContext context) : base(context)
        {
            Context = context; 
        }

        public IEnumerable<Company> SearchCompanies(string searchTerm, CompanyType? companyType)
        {
            var query = Context.Companies.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c => c.Name.Contains(searchTerm) || c.Description.Contains(searchTerm));
            }

            if (companyType.HasValue)
            {
                query = query.Where(c => c.CompanyType == companyType);
            }

            return query.ToList();
        }


        public Company GetCompanyByName(string companyName)
        {
            return Context.Companies.FirstOrDefault(c => c.Name.ToLower() == companyName.ToLower());
        }

        public Company GetCompanyByEmail(string website)
        {
            return Context.Companies.FirstOrDefault(c => c.Website.ToLower() == website.ToLower());
        }


       


        //added for ProjectCompany Controller/////////
        public IEnumerable<Company> GetCompaniesInEacProjectWithPrpjectId(int projectId)
        {
            return Context.ProjectCompany
                .Where(pc => pc.ProjectId == projectId)
                .Select(pc => pc.Company)
                .ToList();
        }

        public void AddCompanyToProject(int companyId, int projectId)
        {
            var projectCompany = new ProjectCompany
            {
                CompanyId = companyId,
                ProjectId = projectId
            };
            Context.ProjectCompany.Add(projectCompany);
            Context.SaveChanges();
        }

        public void RemoveCompanyFromProject(int companyId, int projectId)
        {
            var projectCompany = Context.ProjectCompany
                .FirstOrDefault(pc => pc.CompanyId == companyId && pc.ProjectId == projectId);

            if (projectCompany != null)
            {
                Context.ProjectCompany.Remove(projectCompany);
                Context.SaveChanges();
            }
        }

    }
}

