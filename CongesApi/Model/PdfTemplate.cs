namespace CongesApi.Model
{
	public class PdfTemplate
	{
		public int PdfTemplateId { get; set; }
		public string Name { get; set; } = "Mod�le par d�faut";
		public string Html { get; set; } = "";          // Corps (avec placeholders)
		public string? HeaderHtml { get; set; }         // Optionnel
		public string? FooterHtml { get; set; }         // Optionnel (num�ro de page�)
		public bool IsDefault { get; set; } = true;     // Un seul par d�faut
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	}
}
