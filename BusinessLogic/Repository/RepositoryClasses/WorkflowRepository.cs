using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace BusinessLogic.Repository.RepositoryClasses
{
    public class WorkflowRepository : GenericRepository<WorkflowTemplate> , IWorkflowRepository
    {
        private AppDbContext Context;

        public WorkflowRepository(AppDbContext context) : base(context)
        {
            Context = context;
        }

        public List<WorkflowTemplate> GetAllWithSteps(int proId)
        {
            return Context.WorkflowTemplates.Include(w => w.Steps).Where(w => w.ProjectId==proId ).ToList();
        }

        public WorkflowTemplate GetById(int Id)
        {
            return Context.WorkflowTemplates.Include(w=>w.Steps).FirstOrDefault(w => w.Id == Id);    
        }


        //For notification 
        public WorkflowStepTemplate GetFirstStepByTemplateId(int templateId)
        {
            return Context.WorkflowStepTemplates
                .Where(s => s.WorkflowTemplateId == templateId)
                .OrderBy(s => s.StepOrder)
                .FirstOrDefault();
        }

        public int? GetFirstStepIdByTemplateId(int templateId)
        {
            var firstStep = Context.WorkflowStepTemplates
                .Where(s => s.WorkflowTemplateId == templateId)
                .OrderBy(s => s.StepOrder)
                .FirstOrDefault();

            return firstStep?.Id;
        }
    }
}
