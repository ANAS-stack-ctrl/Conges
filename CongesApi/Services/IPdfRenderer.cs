namespace CongesApi.Services
{
    using CongesApi.Model;

    public interface IPdfRenderer
    {
        // Construit le HTML (on passe aussi la signatureUrl)
        string BuildHtml(
            LeaveRequest req,
            IEnumerable<Approval> approvals,
            string logoUrl,
            string? signatureUrl
        );

        // Rend le PDF
        Task<byte[]> RenderPdfAsync(string html);
    }
}
