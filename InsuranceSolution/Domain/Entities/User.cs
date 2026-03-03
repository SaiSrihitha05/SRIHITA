using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Enums;
namespace Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        // The full name of the user, used for display and logic
        public string Name { get; set; } = string.Empty;

        // Unique identifier for login and communication
        public string Email { get; set; } = string.Empty;

        // Securely stored password credentials
        public string PasswordHash { get; set; } = string.Empty;

        // Contact number for secondary verification or alerts
        public string Phone { get; set; } = string.Empty;

        // Defines what this user is allowed to do (Admin, Customer, etc.)
        public UserRole Role { get; set; }

        // Audit timestamp for when the account was first created
        public DateTime CreatedAt { get; set; }

        // Toggle to disable access without deleting the record
        public bool IsActive { get; set; }
        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }
}
