using System.ComponentModel.DataAnnotations;

public class LoginRequest
{
    [Required]
    public string login { get; set; }

    [Required]
    public string password { get; set; }
}