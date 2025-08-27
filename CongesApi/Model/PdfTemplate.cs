namespace CongesApi.Model
{
	public class PdfTemplate
	{
		public int PdfTemplateId { get; set; }
		public string Name { get; set; } = "Modèle par défaut";
		public string Html { get; set; } = "";          // Corps (avec placeholders)
		public string? HeaderHtml { get; set; }         // Optionnel
		public string? FooterHtml { get; set; }         // Optionnel (numéro de page…)
		public bool IsDefault { get; set; } = true;     // Un seul par défaut
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	}
}
