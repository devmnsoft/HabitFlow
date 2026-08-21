using System.Globalization;
using System.Text;

namespace HabitFlow.Application;

public sealed class ReportDocumentExporter
{
    public byte[] ToPdf(PersonalReport report, DateTimeOffset generatedAt)
    {
        var lines = new[] { "HabitFlow - Relatorio de evolucao", $"Periodo: {report.PeriodStart:yyyy-MM-dd} a {report.PeriodEnd:yyyy-MM-dd}",
            $"Planejado: {report.Planned}   Concluido: {report.TotalCompletions}", $"Consistencia: {report.CompletionRate:0.0}%   Dias ativos: {report.ActiveDays}",
            report.Insight, $"Gerado em: {generatedAt:yyyy-MM-dd HH:mm} UTC" };
        var stream = string.Join(" ", lines.Select((x, i) => $"BT /F1 {(i == 0 ? 18 : 11)} Tf 52 {790 - i * 36} Td ({EscapePdf(x)}) Tj ET"));
        var objects = new[] { "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream", "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" };
        var body = new StringBuilder("%PDF-1.4\n"); var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Length; i++) { offsets.Add(Encoding.ASCII.GetByteCount(body.ToString())); body.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        var xref = Encoding.ASCII.GetByteCount(body.ToString()); body.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) body.Append(CultureInfo.InvariantCulture, $"{offset:0000000000} 00000 n \n");
        body.Append(CultureInfo.InvariantCulture, $"trailer << /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return Encoding.ASCII.GetBytes(body.ToString());
    }

    public byte[] ToCsv(WeeklyReviewResult review)
    {
        var csv = new StringBuilder("Habito;Categoria;PeriodoInicio;PeriodoFim;Esperado;Concluido;MelhorStreak;Insight\r\n");
        foreach (var habit in review.Habits)
            csv.Append(Csv(habit.Name)).Append(';').Append(Csv(habit.Category)).Append(';').Append(review.PeriodStart.ToString("yyyy-MM-dd")).Append(';')
                .Append(review.PeriodEnd.ToString("yyyy-MM-dd")).Append(';').Append(habit.Scheduled).Append(';').Append(habit.Completed).Append(';')
                .Append(habit.CurrentStreak).Append(';').Append(Csv(habit.Insight)).Append("\r\n");
        return new UTF8Encoding(true).GetBytes(csv.ToString());
    }

    private static string Csv(string value) { var safe = string.IsNullOrEmpty(value) ? "" : "=+-@".Contains(value[0]) ? "'" + value : value; return $"\"{safe.Replace("\"", "\"\"")}\""; }
    private static string EscapePdf(string value) => string.Concat(value.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && c <= 127)).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
