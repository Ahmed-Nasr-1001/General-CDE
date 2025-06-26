using BusinessLogic.Repository.RepositoryClasses;
using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer.Models;
using DataLayer;

public class ProjectActivityRepository : GenericRepository<ProjectActivities>, IProjectActivityRepository
{
    AppDbContext context;

    public ProjectActivityRepository(AppDbContext context) : base(context)
    {
        this.context = context;
    }


    public List<ProjectActivities> GetByProjectId(int Proid)
    {
        return context.ProjectActivities.Where(p=>p.projectId == Proid).ToList();   
    }
    public void AddNewActivity(ApplicationUser user , int? ProjectId , string ActivityType , string ActivityDetail)
    {
     
      
          var  newActivity = new ProjectActivities
            {
                UserEmail = user.UserName,
                projectId = ProjectId,
                Date = DateTime.Now,
                ActivityType = ActivityType,
                ActivityDetail = ActivityDetail,
            };

            context.Add(newActivity);
            context.SaveChanges();
       
    }

   

    
}
