namespace API.Utils;

public static class DictionaryHelper
{
    public static string JoinReadOnlyDictionary(this IReadOnlyDictionary<string, string[]> dictionary)
    {
        if (dictionary == null || dictionary.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(". ", dictionary.Select(kv =>
            $"{string.Join(". ", kv.Value)}"));
    }
}
