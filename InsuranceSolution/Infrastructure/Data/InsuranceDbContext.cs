using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public class InsuranceDbContext : DbContext
    {
        public InsuranceDbContext(DbContextOptions<InsuranceDbContext> options) : base(options)
        {
        }
        public DbSet<InsuranceClaim> Claims { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<PolicyAssignment> PolicyAssignments { get; set; }
        public DbSet<PolicyMember> PolicyMembers { get; set; }
        public DbSet<PolicyNominee> PolicyNominees { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<PolicyLoan> PolicyLoans { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            var admin = new User
            {
                Id = 1,
                Name = "System Admin",
                Email = "admin@insurance.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Phone = "9999999999",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            builder.Entity<User>().HasData(admin);
            // Unique Constraints

            builder.Entity<PolicyAssignment>()
                .HasIndex(p => p.PolicyNumber)
                .IsUnique();

            builder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // ENUM CONVERSIONS (Store as string)

            builder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            builder.Entity<PolicyAssignment>()
                .Property(p => p.Status)
                .HasConversion<string>();

            builder.Entity<PolicyAssignment>()
                .Property(p => p.CommissionStatus)
                .HasConversion<string>();

            builder.Entity<PolicyAssignment>()
                .Property(p => p.PremiumFrequency)
                .HasConversion<string>();

            builder.Entity<InsuranceClaim>()
                .Property(c => c.Status)
                .HasConversion<string>();

            builder.Entity<InsuranceClaim>()
                .Property(c => c.ClaimType)
                .HasConversion<string>();

            builder.Entity<Payment>()
                .Property(p => p.Status)
                .HasConversion<string>();

            builder.Entity<Notification>()
                .Property(n => n.Type)
                .HasConversion<string>();

            builder.Entity<PolicyLoan>()
                .Property(l => l.Status)
                .HasConversion<string>();

            // Relationships

            builder.Entity<PolicyAssignment>()
                .HasOne(p => p.Customer)
                .WithMany()
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PolicyAssignment>()
                .HasOne(p => p.Agent)
                .WithMany()
                .HasForeignKey(p => p.AgentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PolicyAssignment>()
                .HasOne(p => p.Plan)
                .WithMany()
                .HasForeignKey(p => p.PlanId);

            builder.Entity<PolicyMember>()
                .HasOne(pm => pm.PolicyAssignment)
                .WithMany(p => p.PolicyMembers)
                .HasForeignKey(pm => pm.PolicyAssignmentId);

            builder.Entity<PolicyNominee>()
                .HasOne(pn => pn.PolicyAssignment)
                .WithMany(p => p.PolicyNominees)
                .HasForeignKey(pn => pn.PolicyAssignmentId);

            builder.Entity<InsuranceClaim>()
                .HasOne(c => c.PolicyAssignment)
                .WithMany()
                .HasForeignKey(c => c.PolicyAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InsuranceClaim>()
                .HasOne(c => c.PolicyMember)
                .WithMany()
                .HasForeignKey(c => c.PolicyMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<InsuranceClaim>()
                .HasOne(c => c.ClaimsOfficer)
                .WithMany()
                .HasForeignKey(c => c.ClaimsOfficerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<Payment>()
                .HasOne(p => p.PolicyAssignment)
                .WithMany(p => p.Payments)
                .HasForeignKey(p => p.PolicyAssignmentId);

            builder.Entity<PolicyLoan>()
                .HasOne(l => l.PolicyAssignment)
                .WithMany(p => p.Loans)
                .HasForeignKey(l => l.PolicyAssignmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PolicyLoan>()
                .HasOne(l => l.Customer)
                .WithMany()
                .HasForeignKey(l => l.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Payment>()
                .HasOne(p => p.PolicyLoan)
                .WithMany(l => l.Payments)
                .HasForeignKey(p => p.PolicyLoanId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Document>()
                .HasOne(d => d.PolicyAssignment)
                .WithMany(p => p.Documents)                    // ← tell EF which collection
                .HasForeignKey(d => d.PolicyAssignmentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Document>()
                .HasOne(d => d.Claim)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.ClaimId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Document>()
                .HasOne(d => d.UploadedByUser)
                .WithMany()
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId);

            //Set column property types

            builder.Entity<InsuranceClaim>()
                .Property(c => c.ClaimAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<InsuranceClaim>()
                .Property(c => c.SettlementAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Plan>(entity =>
            {
                entity.Property(p => p.BaseRate).HasColumnType("decimal(18,2)");
                entity.Property(p => p.MaxCoverageAmount).HasColumnType("decimal(18,2)");
                entity.Property(p => p.MinCoverageAmount).HasColumnType("decimal(18,2)");
            });

            builder.Entity<PolicyAssignment>()
                .Property(pa => pa.TotalPremiumAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<PolicyMember>()
                .Property(pm => pm.CoverageAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<PolicyNominee>()
                .Property(pn => pn.SharePercentage)
                .HasColumnType("decimal(5,2)");

            builder.Entity<Plan>()
                .Property(p => p.CommissionRate)
                .HasColumnType("decimal(5,2)");

            builder.Entity<Plan>()
                .Property(p => p.CoverageIncreaseRate)
                .HasColumnType("decimal(5,2)");

            builder.Entity<Plan>()
                .Property(p => p.MaxLoanPercentage)
                .HasColumnType("decimal(5,2)");

            builder.Entity<Plan>()
                .Property(p => p.LoanInterestRate)
                .HasColumnType("decimal(5,2)");

            builder.Entity<PolicyLoan>(entity =>
            {
                entity.Property(l => l.LoanAmount).HasColumnType("decimal(18,2)");
                entity.Property(l => l.InterestRate).HasColumnType("decimal(5,2)");
                entity.Property(l => l.OutstandingBalance).HasColumnType("decimal(18,2)");
                entity.Property(l => l.TotalInterestPaid).HasColumnType("decimal(18,2)");
            });

            builder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.PrincipalPaid).HasColumnType("decimal(18,2)");
                entity.Property(p => p.InterestPaid).HasColumnType("decimal(18,2)");
                entity.Property(p => p.BalanceAfter).HasColumnType("decimal(18,2)");
            });
        }
    }
}
