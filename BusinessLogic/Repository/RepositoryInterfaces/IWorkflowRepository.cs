using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataLayer.Models;

namespace BusinessLogic.Repository.RepositoryInterfaces
{
    public interface IWorkflowRepository : IGenericRepository<WorkflowTemplate>
    {

         List<WorkflowTemplate> GetAllWithSteps(int proId);
        WorkflowTemplate GetById(int Id);

        // for notification
        WorkflowStepTemplate GetFirstStepByTemplateId(int templateId);
        int? GetFirstStepIdByTemplateId(int templateId);
    }
}
