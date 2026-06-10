using System.Collections.Generic;

namespace DOTS.Features.Execution;

public interface IAlignmentConfigProvider
{
    Dictionary<string, string> LoadAlignments();
}
