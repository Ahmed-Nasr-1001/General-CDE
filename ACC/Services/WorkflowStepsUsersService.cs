using DataLayer;
using DataLayer.Models;

namespace ACC.Services
{
    public class WorkflowStepsUsersService
    {
        private readonly AppDbContext _context;

        public WorkflowStepsUsersService(AppDbContext context)
        {
            _context = context;
        }
        public void Delete(WorkflowStepUser obj)
        {
            var model = _context.Set<WorkflowStepUser>().Remove(obj);
        }

        public IList<WorkflowStepUser> GetAll()
        {
            return _context.Set<WorkflowStepUser>().ToList();
        }

        public WorkflowStepUser GetById(int id)
        {
            return _context.Set<WorkflowStepUser>().Find(id);
        }
        public WorkflowStepUser Get(string UserId , int StepId)
        {
            return _context.Set<WorkflowStepUser>().FirstOrDefault(w => w.StepId == StepId && w.UserId == UserId);
        }
      


        public IList<WorkflowStepUser> GetByStepId(int StepId)
        {
          return  _context.Set<WorkflowStepUser>().Where(w=>w.StepId == StepId).ToList();
        }


        public void Insert(WorkflowStepUser obj)
        {
            _context.Set<WorkflowStepUser>().Add(obj);
        }

        public void Update(WorkflowStepUser obj)
        {
            _context.Set<WorkflowStepUser>().Update(obj);
        }
        public void Save()
        {
            _context.SaveChanges();
        }
        public List<string> GetStepUsers( int StepId)
        {
            return _context.Set<WorkflowStepUser>().Where(w => w.StepId == StepId).Select(w=>w.UserId).ToList();
        }

    }
}

