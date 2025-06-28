using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Models
{
    public class ApplicationUserRole : IdentityUserRole<string>
    {
        public int Id { get; set; }
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        public int? TeamId { get; set; }
        public Team? Team { get; set; }
        public ApplicationUser User { get; set; }

        public ApplicationRole Role { get; set; }
    }

}
