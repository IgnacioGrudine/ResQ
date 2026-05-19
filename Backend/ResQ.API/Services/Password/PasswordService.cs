namespace ResQ.API.Services.Password;

public class PasswordService : IPasswordService
{
    private const int WorkFactor = 12;

    public string Hash(string plainPassword)
        => BCrypt.Net.BCrypt.HashPassword(plainPassword, WorkFactor);

    public bool Verify(string plainPassword, string hash)
        => BCrypt.Net.BCrypt.Verify(plainPassword, hash);
}
