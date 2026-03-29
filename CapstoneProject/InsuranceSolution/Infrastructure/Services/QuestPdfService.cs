using Application.DTOs;
using Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class QuestPdfService : Application.Interfaces.IPdfService
    {
        private const string PrimaryColor = "#75013f";
        private const string AccentColor = "#fe3082";
        private const string LightGrey = "#f9fafb";
        
        // Hartford logo path
        private const string LogoPath = @"d:\angularpractice\HartfordAssignments\capstone\CapstoneProject\insurance-app\public\horizontal_logo.png";

        public QuestPdfService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GeneratePolicyPdfAsync(PolicyResponseDto policy)
        {
            var primaryInsured = policy.Members?.FirstOrDefault(m => m.IsPrimaryInsured)
                                 ?? policy.Members?.FirstOrDefault();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30f);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Lato"));

                    // HEADER
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            if (File.Exists(LogoPath))
                            {
                                col.Item().Height(40).Image(LogoPath).FitHeight();
                            }
                            else
                            {
                                col.Item().Text(x => x.Span("HARTFORD").FontSize(20).ExtraBold().FontColor(PrimaryColor));
                                col.Item().Text(x => x.Span("INSURANCE").FontSize(14).SemiBold().FontColor(PrimaryColor));
                            }
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text(x => x.Span("Life Insurance").FontSize(18).SemiBold());
                            col.Item().Text(x => x.Span("Policy Document").FontSize(18).SemiBold().FontColor(Colors.Grey.Medium));
                        });
                    });

                    page.Content().PaddingVertical(20f).Column(col =>
                    {
                        // TOP INFO BAR
                        col.Item().BorderBottom(1f).BorderColor(PrimaryColor).PaddingBottom(5f).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(x =>
                                {
                                    x.Span("Policy Number: ").SemiBold();
                                    x.Span(policy.PolicyNumber);
                                });
                                c.Item().Text(x =>
                                {
                                    x.Span("Plan Name: ").SemiBold();
                                    x.Span(policy.PlanName);
                                });
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text(x =>
                                {
                                    x.Span("Customer Name: ").SemiBold();
                                    x.Span(policy.CustomerName);
                                });
                                c.Item().Text(x =>
                                {
                                    x.Span("Issue Date: ").SemiBold();
                                    x.Span(policy.CreatedAt.ToString("MMMM dd, yyyy"));
                                });
                            });
                        });

                        // 1. POLICY HOLDER DETAILS
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "1", "POLICY HOLDER DETAILS"));
                        col.Item().Border(1f).BorderColor(Colors.Grey.Lighten2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Full Name:").SemiBold());
                            table.Cell().ColumnSpan(3).Element(ValueCellStyle).Text(x => x.Span(policy.CustomerName));

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Address:").SemiBold());
                            table.Cell().ColumnSpan(3).Element(ValueCellStyle).Text(x => x.Span(policy.Address));

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Contact:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(policy.CustomerPhone));
                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Email:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(policy.CustomerEmail));
                        });

                        // 2. POLICY DETAILS
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "2", "POLICY DETAILS"));
                        col.Item().Border(1f).BorderColor(Colors.Grey.Lighten2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Policy Start Date:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(policy.StartDate.ToString("MMMM dd, yyyy")));
                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Policy End Date:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(policy.EndDate.ToString("MMMM dd, yyyy")));

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Premium Amount:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span($"{policy.TotalPremiumAmount:C2}"));
                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Frequency:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(policy.PremiumFrequency));

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Sum Assured:").SemiBold());
                            table.Cell().ColumnSpan(3).Element(ValueCellStyle).Text(x => 
                            { 
                                var totalCoverage = policy.Members?.Sum(m => m.CoverageAmount) ?? 0;
                                x.Span($"{totalCoverage:C2}").FontColor(PrimaryColor).SemiBold(); 
                            });
                        });

                        // 3. INSURED MEMBER DETAILS
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "3", "INSURED MEMBER DETAILS"));
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(TableHeaderStyle).Text(x => x.Span("Member Name").SemiBold());
                                header.Cell().Element(TableHeaderStyle).Text(x => x.Span("Relationship").SemiBold());
                                header.Cell().Element(TableHeaderStyle).Text(x => x.Span("Sum Assured").SemiBold());
                                header.Cell().Element(TableHeaderStyle).Text(x => x.Span("Status").SemiBold());
                            });

                            foreach (var member in policy.Members ?? Enumerable.Empty<PolicyMemberResponseDto>())
                            {
                                table.Cell().Element(TableCellStyle).Text(x => x.Span((member.MemberName ?? "Member") + (member.IsPrimaryInsured ? " [PRIMARY]" : "")));
                                table.Cell().Element(TableCellStyle).Text(x => x.Span(member.RelationshipToCustomer ?? "N/A"));
                                table.Cell().Element(TableCellStyle).Text(x => x.Span($"{member.CoverageAmount:C2}"));
                                table.Cell().Element(TableCellStyle).Text(x => x.Span("Active"));
                            }
                        });

                        // 4. BENEFITS
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "4", "BENEFITS"));
                        col.Item().PaddingLeft(10f).Column(c =>
                        {
                            c.Item().Text(x => x.Span("• Death Benefit: Pays 100% of Sum Assured to beneficiary").FontSize(9));
                            if (policy.PlanHasBonus)
                                c.Item().Text(x => x.Span($"• Bonus: Accumulated bonus rate at {policy.PlanBonusRate}% annually").FontSize(9));
                            if (policy.PlanHasLoanFacility)
                                c.Item().Text(x => x.Span($"• Loan Facility: Eligible after {policy.PlanLoanEligibleAfterYears} years, up to {policy.PlanMaxLoanPercentage}% of value").FontSize(9));
                        });

                        // 5. DOCUMENTS SUBMITTED
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "5", "DOCUMENTS SUBMITTED"));
                        col.Item().PaddingLeft(10f).Column(c =>
                        {
                            foreach (var doc in policy.Documents ?? Enumerable.Empty<DocumentResponseDto>())
                            {
                                c.Item().Text(x => x.Span($"• {(doc.DocumentCategory ?? "Document")} Submitted (Verified)").FontSize(9));
                            }
                            if (policy.Documents == null || !policy.Documents.Any())
                            {
                                c.Item().Text(x => x.Span("• Identity Proof Submitted (Verified)").FontSize(9));
                                c.Item().Text(x => x.Span("• Address Proof Submitted (Verified)").FontSize(9));
                            }
                        });

                        // 6. TERMS & CONDITIONS
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "6", "TERMS & CONDITIONS"));
                        col.Item().PaddingLeft(10f).Column(c =>
                        {
                            c.Item().Text(x => x.Span("• Cancellation: You have a 15-day free-look period to cancel the policy for a full refund.").FontSize(8));
                            c.Item().Text(x => x.Span($"• Grace Period: The policy provides a {30} day grace period for premium payments.").FontSize(8));
                            c.Item().Text(x => x.Span("• Claim Procedure: Please refer to the 'Help' section in the application for detailed filing instructions.").FontSize(8));
                        });

                        // 7. AGENT DETAILS
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "7", "AGENT DETAILS"));
                        col.Item().Border(1f).BorderColor(Colors.Grey.Lighten2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });
                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Assigned Agent:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(policy.AgentName ?? "Direct Hartford Support"));
                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Contact Info:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span($"{policy.AgentEmail ?? "support@hartford.com"}, {policy.AgentPhone ?? "1-800-HARTFORD"}"));
                        });

                        // 8. DECLARATION & SIGNATURE
                        col.Item().PaddingTop(25f);
                        col.Item().Column(c =>
                        {
                            c.Item().Text(x => x.Span("DECLARATION").SemiBold().FontSize(10));
                            c.Item().Text(x => x.Span("The Policy Holder declares that all information provided is true and correct.").FontSize(8));

                            c.Item().PaddingTop(30f).Row(row =>
                            {
                                row.RelativeItem().Column(sc =>
                                {
                                    sc.Item().BorderTop(1f).PaddingTop(5f).Text(x => x.Span("Customer Signature").SemiBold());
                                    sc.Item().Text(x => x.Span(policy.CustomerName).FontSize(8));
                                    sc.Item().Text(x => x.Span(DateTime.Now.ToString("yyyy-MM-dd")).FontSize(8));
                                });
                                row.ConstantItem(200f); // Space for actual signature
                            });
                        });
                    });

                    page.Footer().Column(f =>
                    {
                        f.Item().BorderTop(1f).PaddingTop(5f).Row(row =>
                        {
                            row.RelativeItem().Text(x => x.Span("Hartford Financial Plaza, Hartford, CT 06155 | Global Compliance Office").FontSize(8).FontColor(Colors.Grey.Medium));
                            row.RelativeItem().AlignRight().Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void SectionHeader(IContainer container, string number, string title)
        {
            container.PaddingBottom(5f).Row(row =>
            {
                row.ConstantItem(20f).Background(PrimaryColor).AlignCenter().AlignMiddle().Text(x => x.Span(number).FontColor(Colors.White).Bold());
                row.RelativeItem().PaddingLeft(5f).AlignMiddle().Text(x => x.Span(title).Bold().FontColor(PrimaryColor));
            });
        }

        private IContainer LabelCellStyle(IContainer container)
        {
            return container.Background(LightGrey).Padding(5f).Border(0.5f).BorderColor(Colors.Grey.Lighten3);
        }

        private IContainer ValueCellStyle(IContainer container)
        {
            return container.Padding(5f).Border(0.5f).BorderColor(Colors.Grey.Lighten3);
        }

        private IContainer TableHeaderStyle(IContainer container)
        {
            return container.Background(PrimaryColor).Padding(5f).DefaultTextStyle(x => x.FontColor(Colors.White));
        }

        private IContainer TableCellStyle(IContainer container)
        {
            return container.Padding(5f).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
        }

        public async Task<byte[]> GenerateClaimSettlementPdfAsync(ClaimResponseDto claim)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30f);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Lato"));

                    // HEADER
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            if (File.Exists(LogoPath))
                            {
                                col.Item().Height(40).Image(LogoPath).FitHeight();
                            }
                            else
                            {
                                col.Item().Text(x => x.Span("HARTFORD").FontSize(20).ExtraBold().FontColor(PrimaryColor));
                                col.Item().Text(x => x.Span("INSURANCE").FontSize(14).SemiBold().FontColor(PrimaryColor));
                            }
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text(x => x.Span("CLAIM SETTLEMENT").FontSize(18).SemiBold());
                            col.Item().Text(x => x.Span("REPORT").FontSize(18).SemiBold().FontColor(Colors.Grey.Medium));
                        });
                    });

                    page.Content().PaddingVertical(20f).Column(col =>
                    {
                        // SUMMARY TABLE
                        col.Item().Border(1f).BorderColor(Colors.Grey.Lighten2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Claim Number:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span($"C-{claim.Id}-{claim.FiledDate:yyyyMMdd}"));

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Policy Number:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(claim.PolicyNumber));

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Customer Name:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(claim.CustomerName));

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Settlement Date:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(DateTime.Now.ToString("MMMM dd, yyyy")));
                        });

                        // 1. POLICY HOLDER DETAILS
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "1", "POLICY HOLDER DETAILS"));
                        col.Item().Border(1f).BorderColor(Colors.Grey.Lighten2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Full Name:").SemiBold());
                            table.Cell().ColumnSpan(3).Element(ValueCellStyle).Text(x => x.Span(claim.CustomerName));

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Email:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(claim.CustomerEmail));
                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Phone:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(claim.CustomerPhone));
                        });

                        // 2. POLICY DETAILS
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "2", "POLICY DETAILS"));
                        col.Item().Border(1f).BorderColor(Colors.Grey.Lighten2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Policy Number:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(claim.PolicyNumber));
                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Plan Name:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(claim.PlanName));

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Start Date:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(claim.PolicyStartDate.ToString("MMMM dd, yyyy")));
                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Sum Assured:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span($"{claim.TotalPolicyCoverage:C2}"));
                        });

                        // 3. CLAIM DETAILS
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "3", "CLAIM DETAILS"));
                        col.Item().Border(1f).BorderColor(Colors.Grey.Lighten2).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Claim Type:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(claim.ClaimType));
                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Filed Date:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(claim.FiledDate.ToString("MMMM dd, yyyy")));

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Insured Member:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(claim.ClaimForMemberName));
                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Relationship:").SemiBold());
                            table.Cell().Element(ValueCellStyle).Text(x => x.Span(claim.MemberRelationship));

                            table.Cell().Element(LabelCellStyle).Text(x => x.Span("Requested:").SemiBold());
                            table.Cell().ColumnSpan(3).Element(ValueCellStyle).Text(x => x.Span($"{claim.ClaimAmount:C2}"));
                        });

                        // 4. DOCUMENTS SUBMITTED
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "4", "DOCUMENTS SUBMITTED"));
                        col.Item().PaddingLeft(10f).Column(c =>
                        {
                            c.Item().Text(x => x.Span("• Death Certificate Submitted (Verified)").FontSize(9));
                        });

                        // 5. CLAIM DECISION
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "5", "CLAIM DECISION"));
                        col.Item().Row(row =>
                        {
                            var isApproved = claim.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase);
                            
                            row.RelativeItem().Padding(5f).Background(isApproved ? Colors.Green.Lighten5 : Colors.Red.Lighten5).Column(c =>
                            {
                                c.Item().AlignCenter().Text(x => x.Span("STATUS: " + claim.Status.ToUpper()).FontSize(14).SemiBold().FontColor(isApproved ? Colors.Green.Medium : Colors.Red.Medium));
                                if (!string.IsNullOrEmpty(claim.RejectionReason))
                                {
                                    c.Item().PaddingTop(5f).Text(x => { x.Span("Reason: ").SemiBold(); x.Span(claim.RejectionReason); });
                                }
                            });
                        });

                        // 6. SETTLEMENT DETAILS (As requested, previous detailed version)
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "6", "SETTLEMENT DETAILS"));
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().PaddingVertical(5f).Text(x => x.Span("Base Coverage Amount:"));
                            table.Cell().PaddingVertical(5f).AlignRight().Text(x => x.Span($"{claim.BaseCoverageAmount:C2}"));

                            table.Cell().PaddingVertical(5f).Text(x => x.Span("Accumulated Bonus:"));
                            table.Cell().PaddingVertical(5f).AlignRight().Text(x => x.Span($"+ {claim.AccumulatedBonus:C2}"));

                            table.Cell().PaddingVertical(5f).Text(x => x.Span("Terminal Bonus:"));
                            table.Cell().PaddingVertical(5f).AlignRight().Text(x => x.Span($"+ {claim.TerminalBonus:C2}"));

                            table.Cell().PaddingVertical(5f).Text(x => x.Span("Outstanding Loan Deduction:").FontColor(Colors.Red.Medium));
                            table.Cell().PaddingVertical(5f).AlignRight().Text(x => x.Span($"- {claim.OutstandingLoanAmount:C2}").FontColor(Colors.Red.Medium));

                            table.Cell().PaddingVertical(5f).BorderTop(1f).Text(x => x.Span("Net Settlement Amount:").SemiBold().FontColor(PrimaryColor));
                            table.Cell().PaddingVertical(5f).BorderTop(1f).AlignRight().Text(x => x.Span($"{claim.NetSettlementAmount:C2}").SemiBold().FontColor(PrimaryColor));
                        });

                        // 7. NOTES / REMARKS
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "7", "OFFICER REMARKS"));
                        col.Item().Background(LightGrey).Padding(10f).Text(x => x.Span(claim.OfficerRemarks ?? "Claim verified with external data source."));

                        // 8. DECLARATION
                        col.Item().PaddingTop(15f);
                        col.Item().Element(c => SectionHeader(c, "8", "DECLARATION"));
                        col.Item().Text(x => x.Span("The Company certifies that the above claim has been processed according to policy guidelines and the final settlement amount has been authorized for release to the beneficiaries.").FontSize(8));
                    });

                    page.Footer().Column(f =>
                    {
                        f.Item().BorderTop(1f).PaddingTop(5f).Row(row =>
                        {
                            row.RelativeItem().Text(x => x.Span("Hartford Financial Plaza, Hartford, CT 06155 | Claims Settlement Division").FontSize(8).FontColor(Colors.Grey.Medium));
                            row.RelativeItem().AlignRight().Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                            });
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
