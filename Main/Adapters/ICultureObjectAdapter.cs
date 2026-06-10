namespace DOTS.Adapters;

public interface ICultureObjectAdapter
{
    object? ResolveCulture(string cultureId);
}
