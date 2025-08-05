using System.ComponentModel.DataAnnotations;

namespace CongesApi.Model
{
    public class UserRole
    {
        [Key]
        public string Role { get; set; }  // clé primaire

        // Si tu veux, tu peux ajouter une collection de Users
        // public ICollection<User> Users { get; set; }
    }
}
