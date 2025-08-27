using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class Holiday
    {
        [Key]
        public int HolidayId { get; set; }

        // Date de début du férié (AA-MM-JJ)
        public DateTime Date { get; set; }

        // Libellé (ex: "Marche Verte")
        public string Description { get; set; } = string.Empty;

        // Si vrai, le férié revient tous les ans à la même date (mois/jour)
        public bool IsRecurring { get; set; }

        // ✅ Nombre de jours de ce férié (défaut 1). Ex: "Aïd al-Adha (2e jour)" => 1, etc.
        public int DurationDays { get; set; } = 1;
    }
}
