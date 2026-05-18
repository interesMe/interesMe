namespace InteresMe.API.Configuration;

public static class EnvLoader
{
    public static void LoadDotEnv()
    {
        foreach (var path in GetCandidatePaths())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                {
                    continue;
                }

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = trimmed[..separatorIndex].Trim();
                var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');

                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }

            return;
        }
    }

    private static IEnumerable<string> GetCandidatePaths()
    {
        var current = Directory.GetCurrentDirectory();

        yield return Path.Combine(current, ".env");
        yield return Path.GetFullPath(Path.Combine(current, "..", ".env"));
        yield return Path.GetFullPath(Path.Combine(current, "..", "..", ".env"));
    }
}
