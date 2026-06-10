namespace DOTS.Core.Infrastructure;

public interface IPathService
{
    string ModuleRootPath { get; }

    string ModuleDataPath { get; }

    string ConfigPath { get; }
}
