namespace WebApi;

internal static class LocalEnv
{
    public static void Load()
    {
        string? dir = Directory.GetCurrentDirectory();
        for (int i = 0; i < 6 && dir is not null; i++)
        {
            string path = Path.Combine(dir, ".env");
            if (File.Exists(path))
            {
                Apply(path);
                return;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }
    }

    private static void Apply(string path)
    {
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            int eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            string key = line[..eq].Trim();
            string value = Unquote(line[(eq + 1)..].Trim());
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                Environment.SetEnvironmentVariable(key, value);
        }
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            return value[1..^1];

        return value;
    }
}
