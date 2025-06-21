using DataLayer;
using DataLayer.Models;

namespace ACC.Services
{
    public class ReviewStepUsersService
    {
        private readonly AppDbContext _context;

        public ReviewStepUsersService(AppDbContext context)
        {
            _context = context;
        }
        public void Delete(ReviewStepUser obj)
        {
            var model = _context.Set<ReviewStepUser>().Remove(obj);
        }

        public IList<ReviewStepUser> GetAll()
        {
            return _context.Set<ReviewStepUser>().ToList();
        }

        public ReviewStepUser GetById(int id)
        {
            return _context.Set<ReviewStepUser>().Find(id);
        }
        
        public ReviewStepUser Get(string UserId, int StepId, int ReviewId)
        {
            return _context.Set<ReviewStepUser>().FirstOrDefault(w => w.StepId == StepId && w.UserId == UserId && w.ReviewId == ReviewId);
        }

        public int GetUsersCountById(int StepId, int ReviewId)
        {
            var List = _context.Set<ReviewStepUser>().Where(s => s.StepId == StepId && s.ReviewId == ReviewId);
            int counter = 0;
            foreach(var item in List)
            {
                counter++;
            }
            return counter;
        }

        public IList<ReviewStepUser> GetByStepId(int StepId , int ReviewId)
        {
            return _context.Set<ReviewStepUser>().Where(w => w.StepId == StepId && w.ReviewId == ReviewId).ToList();
        }
        public void Insert(ReviewStepUser obj)
        {
            _context.Set<ReviewStepUser>().Add(obj);
        }

        public void Update(ReviewStepUser obj)
        {
            _context.Set<ReviewStepUser>().Update(obj);
        }
        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
