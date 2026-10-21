namespace HoardFinder.Search
{
    /// <summary>Coarse item groups for the type filter and sort, derived from Valheim's ItemType.</summary>
    internal enum ItemCategory
    {
        Materials,
        Food,
        Potions,
        Ammo,
        Equipment,
        Trophies,
        Other,
    }

    internal static class ItemCategories
    {
        public static ItemCategory Of(ItemDrop.ItemData.SharedData shared)
        {
            if (shared == null)
            {
                return ItemCategory.Other;
            }
            switch (shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.Material:
                    return ItemCategory.Materials;
                case ItemDrop.ItemData.ItemType.Consumable:
                    // Valheim food restores health/stamina/eitr; meads and other consumables do not.
                    return shared.m_food > 0f || shared.m_foodStamina > 0f || shared.m_foodEitr > 0f
                        ? ItemCategory.Food
                        : ItemCategory.Potions;
                case ItemDrop.ItemData.ItemType.Ammo:
                case ItemDrop.ItemData.ItemType.AmmoNonEquipable:
                    return ItemCategory.Ammo;
                case ItemDrop.ItemData.ItemType.Trophy:
                    return ItemCategory.Trophies;
                default:
                    return IsEquipment(shared.m_itemType) ? ItemCategory.Equipment : ItemCategory.Other;
            }
        }

        private static bool IsEquipment(ItemDrop.ItemData.ItemType type)
        {
            switch (type)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Hands:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Utility:
                case ItemDrop.ItemData.ItemType.Trinket:
                case ItemDrop.ItemData.ItemType.Tool:
                case ItemDrop.ItemData.ItemType.Torch:
                case ItemDrop.ItemData.ItemType.Attach_Atgeir:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Translation key of the category name.</summary>
        public static string Key(ItemCategory category) => "cat_" + category.ToString().ToLowerInvariant();
    }
}
