using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum LoanStatus
    {
        Active,     // loan is running
        Closed,     // fully repaid
        Adjusted    // deducted from maturity payout
    }
}
