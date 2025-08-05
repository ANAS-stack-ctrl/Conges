namespace CongesApi.DTOs
{
    public class RegisterRequest
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string NationalID { get; set; } = "";
        public string Role { get; set; } = "";
        public int CreatedBy { get; set; } = 0;
    }
}
