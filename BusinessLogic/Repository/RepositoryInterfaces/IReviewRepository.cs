using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.Models;

namespace BusinessLogic.Repository.RepositoryInterfaces
{
    public interface IReviewRepository : IGenericRepository<Review>
    {
        WorkflowTemplate GetWorkflowTemp(int WorkflowId);

        List<Review> GetCurrentStepUserReviews(string UserId , int proId);
       
        Review GetReviewById(int Id);

        List<Document> GetDocuments(List<int> documentsIds);
        List<Folder> GetFolders(List<int> foldersIds);


        //For Notification 
        Review GetReviewByIdForNotification(int Id);
    }
}
