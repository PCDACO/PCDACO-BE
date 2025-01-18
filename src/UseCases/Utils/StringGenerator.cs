using System.Text;

public class StringGenerator
{
    private static readonly char[] Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
    private static readonly Random Random = new();

    public static string GenerateRandomString(int length = 32)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be greater than 0.", nameof(length));

        StringBuilder result = new(length);

        for (int i = 0; i < length; i++)
        {
            int index = Random.Next(Characters.Length);
            result.Append(Characters[index]);
        }

        return result.ToString();
    }
}
