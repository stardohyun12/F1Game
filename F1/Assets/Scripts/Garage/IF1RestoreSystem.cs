using System;

namespace PitCrew.Garage
{
    public interface IF1RestoreSystem
    {
        bool IsRestored(F1PartId id);
        bool CanAfford(F1Part part);
        bool TryRestore(F1Part part);
        event Action<F1Part> OnRestored;
        F1RestoreCatalog Catalog { get; }
    }
}
