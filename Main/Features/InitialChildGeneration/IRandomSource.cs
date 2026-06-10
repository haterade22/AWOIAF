namespace DOTS.Features.InitialChildGeneration;

public interface IRandomSource
{
    double NextDouble();
    int Next(int minInclusive, int maxExclusive);
}
