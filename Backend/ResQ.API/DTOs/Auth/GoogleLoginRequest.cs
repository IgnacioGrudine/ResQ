using System.ComponentModel.DataAnnotations;

namespace ResQ.API.DTOs.Auth;

public class GoogleLoginRequest
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
}
