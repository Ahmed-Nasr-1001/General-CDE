using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.Models.Enums
{
    public class TeamMember
    {
        public int TeamId { get; set; }
        public Team Team { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}
