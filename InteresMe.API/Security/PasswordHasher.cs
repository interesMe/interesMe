namespace InteresMe.API.Security;

public static class PasswordHasher
{
    // Higher work factor = slower hashing, better protection (12 is a common default).
    private const int WorkFactor = 12;

    public static string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public static bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
