using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum PolicyStatus
    {
        Draft,
        Pending,
        Active,
        Lapsed,
        Expired,
        Cancelled,
        Rejected,
        Matured,
        Closed,
        Renewed
    }
}
