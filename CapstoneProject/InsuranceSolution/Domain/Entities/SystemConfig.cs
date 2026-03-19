using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class SystemConfig
    {
        public int Id { get; set; }

        // Stores the index of the last Claims Officer assigned for Round Robin
        public int LastClaimsOfficerIndex { get; set; } = -1;

        // Stores the index of the last Agent assigned for Round Robin
        public int LastAgentAssignmentIndex { get; set; } = -1;

        // Audit timestamp
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
