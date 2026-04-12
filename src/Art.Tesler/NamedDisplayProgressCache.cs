namespace Art.Tesler;

internal class NamedDisplayProgressCache
{
    public record struct Parameters(string Key, int Numerator, int Denominator)
    {
        public string CreateString()
        {
            return $"{Key} {Numerator}/{Denominator}";
        }
    }

    public Parameters Cached;
    public string CachedString;

    public NamedDisplayProgressCache(Parameters initialValue)
    {
        Cached = initialValue;
        CachedString = initialValue.CreateString();
    }

    public string GetString(Func<Parameters, Parameters> func)
    {
        return GetString(func(Cached));
    }

    public string GetString(Parameters current)
    {
        if (current == Cached)
        {
            return CachedString;
        }
        Cached = current;
        return current.CreateString();
    }
}
