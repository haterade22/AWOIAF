using System.Collections.Generic;
using DOTS.Features.CultureMarketplace.Domain;

namespace DOTS.Adapters;

public interface IItemPoolAdapter
{
    IReadOnlyList<ItemPoolItem> GetAllItems();
    bool ItemExists(string itemId);
}
