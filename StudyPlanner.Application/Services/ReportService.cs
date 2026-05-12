using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StudyPlanner.Core.Entities;
using System.Globalization;

namespace StudyPlanner.Application.Services
{
    /// <summary>
    /// Static service responsible for generating PDF reports using the QuestPDF library.
    /// Creates professional-looking study reports containing tables, statistics, and metadata.
    /// </summary>
    public static class ReportService
    {
        /// <summary>
        /// Generates a weekly study report in PDF format.
        /// Includes a summary of total hours studied and a detailed table of sessions.
        /// </summary>
        /// <param name="sessions">Collection of study sessions to include in the report.</param>
        /// <param name="filePath">The full path where the PDF file will be saved.</param>
        public static void GenerateWeeklyReport(IEnumerable<StudySession> sessions, string filePath)
        {
            // Set QuestPDF license to Community (free for development/small businesses)
            QuestPDF.Settings.License = LicenseType.Community;

            // Convert IEnumerable to List to allow multiple iterations (Min, Max, Sum, Count)
            var sessionList = sessions.ToList();

            // Create the PDF document using QuestPDF's fluent API
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    // --- Page Configuration ---
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(TextStyle.Default.FontSize(12));

                    // --- Header Section ---
                    page.Header()
                        .Text("Weekly Study Report")
                        .FontSize(24)
                        .Bold()
                        .AlignCenter();

                    // --- Content Section ---
                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            if (sessionList.Count != 0)
                            {
                                // Calculate report period range
                                var startDate = sessionList.Min(s => s.Date);
                                var endDate = sessionList.Max(s => s.Date);

                                // Display Report Period
                                column.Item()
                                    .PaddingBottom(10)
                                    .Text($"Report Period: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}")
                                    .FontSize(14)
                                    .Bold();

                                // Calculate and display total hours studied
                                var totalHours = sessionList.Sum(s => s.Duration.Value.TotalHours);
                                column.Item()
                                    .PaddingBottom(20)
                                    .Text($"Total Hours Studied: {totalHours:F2} hours")
                                    .FontSize(14)
                                    .Bold();

                                // Create Data Table
                                column.Item().Table(table =>
                                {
                                    // Define column widths (Subject takes more space)
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2); // Subject ID
                                        columns.RelativeColumn(1); // Date
                                        columns.RelativeColumn(1); // Duration
                                    });

                                    // Table Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Text("Subject").Bold();
                                        header.Cell().Text("Date").Bold();
                                        header.Cell().Text("Duration (min)").Bold();

                                        // Style header cells
                                        header.Cell().Padding(5).Background(Colors.Grey.Lighten2);
                                    });

                                    // Table Rows
                                    foreach (var session in sessionList)
                                    {
                                        table.Cell().Text(session.SubjectId.ToString());
                                        table.Cell().Text(session.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                                        table.Cell().Text(session.Duration.Value.TotalMinutes.ToString("F1", CultureInfo.InvariantCulture));
                                    }
                                });
                            }
                            else
                            {
                                // Handle empty data scenario
                                column.Item()
                                    .Text("No study sessions recorded this week.")
                                    .FontSize(12)
                                    .Italic();
                            }
                        });

                    // --- Footer Section ---
                    page.Footer()
                        .Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm}")
                        .FontSize(10)
                        .AlignRight();
                });
            })
            .GeneratePdf(filePath);
        }
    }
}
