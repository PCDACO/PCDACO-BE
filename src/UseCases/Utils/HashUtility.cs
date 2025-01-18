using System.Security.Cryptography;
using System.Text;

namespace UseCases.Utils;

public static class HashUtility
{
    public static string HashString(this string input)
    {
        if (string.IsNullOrEmpty(input))
            throw new ArgumentException("Input cannot be null or empty.", nameof(input));
        // Chuyển chuỗi đầu vào thành byte array
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);

        // Hash các byte
        byte[] hashedBytes = SHA256.HashData(inputBytes);
        // Chuyển đổi hash thành chuỗi hex
        StringBuilder sb = new();
        foreach (byte b in hashedBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
