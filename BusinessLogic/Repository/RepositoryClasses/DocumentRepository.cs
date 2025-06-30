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
    public class DocumentRepository : GenericRepository<Document> , IDocumentRepository
    {
        public DocumentRepository(AppDbContext context) : base(context)
        {

        }

       
    }
}
