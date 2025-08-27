using CongesApi.Model;
using Microsoft.Playwright;
using System.Net;
using System.Text;

namespace CongesApi.Services
{
    public class PdfRenderer : IPdfRenderer
    {
        private readonly IBrowser _browser;
        public PdfRenderer(IBrowser browser) { _browser = browser; }

        private static string H(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

        public string BuildHtml(
            LeaveRequest req,
            IEnumerable<Approval> approvals,
            string logoUrl,
            string? signatureUrl
        )
        {
            var sb = new StringBuilder();

            sb.Append("""
<!doctype html>
<html lang="fr">
<head>
<meta charset="utf-8">
<title>Demande de congé</title>
<style>
  body { font-family: Arial, Helvetica, sans-serif; color:#222; }
  .wrap { max-width: 800px; margin: 0 auto; }
  header { display:flex; justify-content:space-between; align-items:center; margin-bottom:24px; }
  .brand { display:flex; align-items:center; gap:12px; }
  .brand img { max-height:80px; max-width:200px; object-fit:contain; }
  h1 { margin: 0 0 8px 0; font-size: 20px; }
  table { width:100%; border-collapse:collapse; margin-top:8px; }
  th, td { border:1px solid #ddd; padding:8px; text-align:left; }
  .muted { color:#666; font-size:12px; }
</style>
</head>
<body>
<div class="wrap">
""");

            sb.Append($"""
  <header>
    <div class="brand">
      {(string.IsNullOrWhiteSpace(logoUrl) ? "" : $"<img src=\"{logoUrl}\" alt=\"logo\"/>")}
      
    </div>
    <div class="muted">Généré le {DateTime.Now:dd/MM/yyyy HH:mm}</div>
  </header>

  <h1>Demande de congé</h1>

  <table>
    <tbody>
      <tr><th>Employé</th><td>{H(req.User?.FirstName + " " + req.User?.LastName)}</td></tr>
      <tr><th>Type</th><td>{H(req.LeaveType?.Name)}</td></tr>
      <tr><th>Du</th><td>{req.StartDate:dd/MM/yyyy}</td></tr>
      <tr><th>Au</th><td>{req.EndDate:dd/MM/yyyy}</td></tr>
      <tr><th>Jours</th><td>{req.RequestedDays}</td></tr>
      <tr><th>Demi-journée</th><td>{(req.IsHalfDay ? "Oui" : "Non")}</td></tr>
      <tr><th>Statut</th><td>{H(req.Status)}</td></tr>
    </tbody>
  </table>

  <h3 style="margin-top:24px;">Historique des validations</h3>
  <table>
    <thead>
      <tr><th>Niveau</th><th>Statut</th><th>Par</th><th>Date</th><th>Commentaires</th></tr>
    </thead>
    <tbody>
""");

            foreach (var a in approvals ?? Enumerable.Empty<Approval>())
            {
                var who = a.User != null ? $"{a.User.FirstName} {a.User.LastName}" : "";
                var when = a.ActionDate.HasValue ? a.ActionDate.Value.ToString("dd/MM/yyyy HH:mm") : "—";
                sb.Append($"""
      <tr>
        <td>{a.Level}</td>
        <td>{H(a.Status)}</td>
        <td>{H(who)}</td>
        <td>{when}</td>
        <td>{H(a.Comments)}</td>
      </tr>
""");
            }

            sb.Append("""
    </tbody>
  </table>
""");

            if (!string.IsNullOrWhiteSpace(signatureUrl))
            {
                sb.Append($"""
  <div style="margin-top:24px;">
    <strong>Signature employé :</strong><br/>
    <img src="{signatureUrl}" alt="signature" style="max-height:120px;"/>
  </div>
""");
            }

            sb.Append("""
</div>
</body>
</html>
""");
            return sb.ToString();
        }

        public async Task<byte[]> RenderPdfAsync(string html)
        {
            var context = await _browser.NewContextAsync();
            var page = await context.NewPageAsync();

            await page.SetContentAsync(html, new() { WaitUntil = WaitUntilState.NetworkIdle });

            var pdf = await page.PdfAsync(new()
            {
                Format = "A4",
                PrintBackground = true,
                Margin = new()
                {
                    Top = "20mm",
                    Bottom = "20mm",
                    Left = "15mm",
                    Right = "15mm"
                }
            });

            await context.CloseAsync();
            return pdf;
        }
    }
}
