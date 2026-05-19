namespace ResQ.API.Services.Password;

public interface IPasswordService
{
    string Hash(string plainPassword);
    bool Verify(string plainPassword, string hash);
}
