using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer;
using DataLayer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic.Repository.RepositoryClasses
{
    public class ReviewDocumentService 
    {
        private readonly AppDbContext _context;

        public ReviewDocumentService(AppDbContext context)
        {
            _context = context;
        }
        public void Delete(ReviewDocument obj)
        {
            var model = _context.Set<ReviewDocument>().Remove(obj);
        }

        public IList<ReviewDocument> GetAll()
        {
            return _context.Set<ReviewDocument>().Include(r=>r.Review).Include(r=>r.Document).Include(r=>r.Comments).ToList();
        }

        public ReviewDocument GetById(int id)
        {
            return _context.Set<ReviewDocument>().Find(id);
        }

        public void Insert(ReviewDocument obj)
        {
            _context.Set<ReviewDocument>().Add(obj);
        }

        public void Update(ReviewDocument obj)
        {
            _context.Set<ReviewDocument>().Update(obj);
        }
        public void Save()
        {
            _context.SaveChanges();
        }

        public IList<ReviewDocumentComment> GetAllComments()
        {
            return _context.Set<ReviewDocumentComment>()
                .Include(c => c.Reviewer)
                .Include(c => c.Review)
                .Include(c => c.Document)
                .OrderByDescending(c => c.CreatedAt).ToList();
        }

        public bool WasDocRejectedByThisUser(string currentUserId , int reviewId , int documentId)
        {
            return _context.Set<ReviewDocumentComment>().Any(c => c.ReviewId == reviewId && c.ReviewerId == currentUserId && c.DocumentId == documentId);
        }
        public void InsertComment(ReviewDocumentComment obj)
        {
            _context.Set<ReviewDocumentComment>().Add(obj);
        }
    }
}
