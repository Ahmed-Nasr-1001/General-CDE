using DataLayer.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Models
{
    public class Team : BaseEntity
    {
        public string? Name { get; set; }
        public int ProjectId { get; set; }
        public Project Project { get; set; }

        public int ParentFolderId { get; set; }

        public Folder? ParentFolder { get; set; }
        public List<Folder>? Folders { get; set; }
        public List<TeamMember>? Members { get; set; }
    }
}
