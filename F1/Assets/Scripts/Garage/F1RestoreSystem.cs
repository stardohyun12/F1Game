using System;
using System.Collections.Generic;
using UnityEngine;

namespace PitCrew.Garage
{
    public sealed class F1RestoreSystem : MonoBehaviour, IF1RestoreSystem
    {
        [SerializeField] private F1RestoreCatalog catalog;
        public F1RestoreCatalog Catalog => catalog;

        readonly HashSet<F1PartId> _restored = new HashSet<F1PartId>();

        public event Action<F1Part> OnRestored;

        public bool IsRestored(F1PartId id) => _restored.Contains(id);

        public bool CanAfford(F1Part part)
        {
            if (part == null) return false;
            var up = Services.Upgrade;
            return up != null && up.Wallet >= part.restoreCost;
        }

        public bool TryRestore(F1Part part)
        {
            if (part == null) return false;
            if (IsRestored(part.id)) return false;
            var up = Services.Upgrade;
            if (up == null || up.Wallet < part.restoreCost) return false;

            up.Spend(part.restoreCost);
            _restored.Add(part.id);
            ApplyEffect(part);
            OnRestored?.Invoke(part);
            Debug.Log($"[F1RestoreSystem] Restored {part.id} for ₩{part.restoreCost}. Wallet={up.Wallet}");
            return true;
        }

        // Task 4~8에서 채워짐. 일단 골조.
        void ApplyEffect(F1Part p)
        {
            switch (p.id)
            {
                case F1PartId.Engine:
                    Services.Car?.ApplyEngineRestore(1.20f, 1.30f);
                    break;
                case F1PartId.Gearbox:
                    Services.Gears?.SetManualUnlocked(true);
                    break;
                case F1PartId.Tires:
                    Services.Tires?.SetSelectionUnlocked(true);
                    break;
                case F1PartId.Suspension: break;
                case F1PartId.Aero:
                    Services.Boost?.SetUnlocked(true);
                    break;
                case F1PartId.Paint:      break;
            }
        }
    }
}
