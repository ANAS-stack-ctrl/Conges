using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace CongesApi.Model
{
    [Table("LeaveBalance")]
    public class LeaveBalance
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int LeaveTypeId { get; set; }
        public LeaveType LeaveType { get; set; }

         // ← Ajoute ceci (5 chiffres dont 2 après la virgule, ex: 99.99)
        public decimal CurrentBalance { get; set; }
    }
}
