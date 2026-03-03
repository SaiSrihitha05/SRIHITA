using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Document
    {
        public int Id { get; set; }

        // Original name of the file when uploaded
        public string FileName { get; set; } = string.Empty;

        // Physical path or identifier on the server storage
        public string FilePath { get; set; } = string.Empty;

        // Categorization (e.g., IdentityProof, IncomeProof, DeathCertificate)
        public string DocumentCategory { get; set; } = string.Empty;

        // Audit timestamp
        public DateTime UploadedAt { get; set; }

        // The user who performed the upload
        public int UploadedByUserId { get; set; }

        // Links to the relevant business context
        public int? PolicyAssignmentId { get; set; }
        public int? ClaimId { get; set; }

        // Navigation properties
        public User? UploadedByUser { get; set; }
        public PolicyAssignment? PolicyAssignment { get; set; }
        public InsuranceClaim? Claim { get; set; }
    }
}
