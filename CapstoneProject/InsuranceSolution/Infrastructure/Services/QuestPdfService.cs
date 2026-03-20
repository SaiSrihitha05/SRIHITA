using Application.DTOs;
using Application.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class QuestPdfService : Application.Interfaces.IPdfService
    {
        public QuestPdfService()
        {
            // Set QuestPDF License
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GeneratePolicyPdfAsync(Application.DTOs.PolicyResponseDto policy)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("POLICY DOCUMENT").FontSize(24).SemiBold().FontColor(Colors.Blue.Medium);
                            col.Item().Text($"{policy.PlanName}").FontSize(14);
                        });

                        row.ConstantItem(100).Height(50).Placeholder(); // Placeholder for Logo
                    });

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"Policy Number: {policy.PolicyNumber}").SemiBold();
                            r.RelativeItem().AlignRight().Text($"Date: {DateTime.Now:D}");
                        });

                        col.Item().PaddingTop(10).Text("Customer Details").FontSize(16).SemiBold();
                        col.Item().Text($"Name: {policy.CustomerName}");

                        col.Item().PaddingTop(20).Text("Insured Members").FontSize(16).SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("#");
                                header.Cell().Element(CellStyle).Text("Member Name");
                                header.Cell().Element(CellStyle).Text("Relation");
                                header.Cell().Element(CellStyle).Text("Coverage");

                                static IContainer CellStyle(IContainer container)
                                {
                                    return container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                                }
                            });

                            int i = 1;
                            foreach (var member in policy.Members)
                            {
                                table.Cell().Element(MemberCellStyle).Text($"{i++}");
                                table.Cell().Element(MemberCellStyle).Text(member.MemberName);
                                table.Cell().Element(MemberCellStyle).Text(member.RelationshipToCustomer);
                                table.Cell().Element(MemberCellStyle).Text($"{member.CoverageAmount:C}");

                                static IContainer MemberCellStyle(IContainer container)
                                {
                                    return container.PaddingVertical(5);
                                }
                            }
                        });

                        col.Item().PaddingTop(20).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Premium Summary").FontSize(16).SemiBold();
                                c.Item().Text($"Total Premium: {policy.TotalPremiumAmount:C}");
                                c.Item().Text($"Frequency: {policy.PremiumFrequency}");
                                c.Item().Text($"Next Due Date: {policy.NextDueDate:D}");
                                c.Item().Text($"Duration: {policy.TermYears} Years");
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateClaimSettlementPdfAsync(Application.DTOs.ClaimResponseDto claim)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Header().Text("CLAIM SETTLEMENT ADVICE").FontSize(24).SemiBold().FontColor(Colors.Green.Medium);

                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().Text($"Claim ID: {claim.Id}").SemiBold();
                        col.Item().Text($"Policy ID: {claim.PolicyAssignmentId}");
                        col.Item().Text($"Settlement Date: {DateTime.Now:D}");

                        col.Item().PaddingTop(20).Text("Claim Details").FontSize(16).SemiBold();
                        col.Item().Text($"Member: {claim.ClaimForMemberName}");
                        col.Item().Text($"Type: {claim.ClaimType}");
                        col.Item().Text($"Status: {claim.Status}");

                        col.Item().PaddingTop(20).Text("Settlement Breakdown").FontSize(16).SemiBold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().PaddingVertical(5).Text("Base Coverage Amount:");
                            table.Cell().PaddingVertical(5).AlignRight().Text($"{claim.ClaimAmount:C}");

                            table.Cell().PaddingVertical(5).Text("Accumulated Bonus:");
                            table.Cell().PaddingVertical(5).AlignRight().Text($"+ {claim.AccumulatedBonus:C}");

                            table.Cell().PaddingVertical(5).Text("Terminal Bonus:");
                            table.Cell().PaddingVertical(5).AlignRight().Text($"+ {claim.TerminalBonus:C}");

                            table.Cell().PaddingVertical(5).Text("Outstanding Loan Deduction:").FontColor(Colors.Red.Medium);
                            table.Cell().PaddingVertical(5).AlignRight().Text($"- {claim.OutstandingLoanAmount:C}").FontColor(Colors.Red.Medium);

                            table.Cell().PaddingVertical(5).BorderTop(1).Text("Net Settlement Amount:").SemiBold();
                            table.Cell().PaddingVertical(5).BorderTop(1).AlignRight().Text($"{claim.NetSettlementAmount:C}").SemiBold();
                        });

                        col.Item().PaddingTop(30).Text("Remarks:").SemiBold();
                        col.Item().Text(claim.Remarks ?? "No remarks provided.");
                    });

                    page.Footer().AlignCenter().Text("This is an electronically generated document and does not require a signature.");
                });
            });

            return document.GeneratePdf();
        }
    }
}
