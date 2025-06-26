using DataLayer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Repository.RepositoryInterfaces
{
    public interface IProjectActivityRepository : IGenericRepository<ProjectActivities>
    {
        void AddNewActivity(ApplicationUser user, int? ProjectId, string ActivityType, string ActivityDetail);

        public List<ProjectActivities> GetByProjectId(int Proid);





    }
}
