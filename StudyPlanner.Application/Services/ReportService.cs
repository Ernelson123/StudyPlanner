using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudyPlanner.Core.Entities;
using System.Reflection.Metadata;

namespace StudyPlanner.Application.Services
{
    /// <summary>
    /// Service for generating PDF reports using QuestPDF library.
    /// Creates professional-looking study reports with tables and statistics.
    /// </summary>
    public static class ReportService
    {
        /// <summary>
        /// Generates a weekly study report PDF.
        /// Lists all study sessions with subject, date, and duration.
        /// </summary>
        public static void GenerateWeeklyReport(IEnumerable<StudySession> sessions, string filePath)
        {
            QuestPDF.Settings.License = LicenseType.Community; // Set license for QuestPDF
                                                               // QuestPDF uses a fluent API to build documents
                                                               // Document.Create() starts a new PDF document
            QuestPDF.Fluent.Document.Create(container =>
            {
                // Configure the page layout
                container.Page(page =>
                {
                    // Set page size to A4 (standard paper size)
                    page.Size(PageSizes.A4);

                    // Set margins (2 cm on all sides)
                    page.Margin(2, Unit.Centimetre);

                    // Set default font size for all text
                    page.DefaultTextStyle(TextStyle.Default.FontSize(12));

                    // ========== HEADER ==========
                    page.Header()
                        .Text("Weekly Study Report")
                        .FontSize(24).Bold().AlignCenter();

                    // ========== CONTENT ==========
                    page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        var sessionList = sessions.ToList();

                        if (sessionList.Count != 0)
                        {
                            var startDate = sessionList.Min(s => s.Date);
                            var endDate = sessionList.Max(s => s.Date);

                            column.Item()
                                .PaddingBottom(10)
                                .Text($"Report Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}")
                                .FontSize(14).Bold();

                            var totalHours = sessionList.Sum(s => s.Duration.Value.TotalHours);
                            column.Item()
                                .PaddingBottom(20)
                                .Text($"Total Hours Studied: {totalHours:F2} hours")
                                .FontSize(14).Bold();

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Subject").Bold();
                                    header.Cell().Text("Date").Bold();
                                    header.Cell().Text("Duration (min)").Bold();
                                    header.Cell().Padding(5).Background(Colors.Grey.Lighten2);
                                });

                                foreach (var session in sessionList)
                                {
                                    table.Cell().Text(session.SubjectId.ToString());
                                    table.Cell().Text(session.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                                    table.Cell().Text(session.Duration.Value.TotalMinutes.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
                                }
                            });
                        }
                        else
                        {
                            column.Item().Text("No study sessions recorded this week.")
                                .FontSize(12).Italic();
                        }
                    });

                    // ========== FOOTER ==========
                    page.Footer()
                        .Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm}")
                        .FontSize(10).AlignRight();
                });
            })
            .GeneratePdf(filePath);
        }
    }
}
