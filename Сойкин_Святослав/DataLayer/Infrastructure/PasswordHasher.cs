using System.Security.Cryptography;
using System.Text;

namespace DataLayer.Infrastructure;

// Хеш логина и пароля. UTF-16LE сохраняет совместимость с исходными хешами базы
public static class PasswordHasher
{
    public static byte[]? Compute(string? login, string? password) =>
        string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password)
            ? null
            : SHA256.HashData(Encoding.Unicode.GetBytes(login + password));

} // class PasswordHasher
