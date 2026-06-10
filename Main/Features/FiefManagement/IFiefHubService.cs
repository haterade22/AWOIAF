using System.Collections.Generic;
using DOTS.Features.FiefManagement.Models;

namespace DOTS.Features.FiefManagement;

public interface IFiefHubService
{
    IReadOnlyList<FiefSummary> GetOrderedFiefs();
    int Count { get; }
    int Clamp(int index);
    int Next(int index);
    int Previous(int index);
    FiefSummary GetAt(int index);
    bool PlayerIsAt(FiefSummary fief);
}
