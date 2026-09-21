using System;
using System.Collections.Generic;
using HarmonyLib;
using TidyChests.Stash;
using UnityEngine;
using UnityEngine.UI;

namespace TidyChests.Ui
{
    /// <summary>
    /// The locks in the player's inventory grid: a padlock in the top-left corner of every
    /// locked item (grey) and every locked slot (red), shown while the reveal key is held,
    /// a tooltip with the lock keys on the slot under the pointer, and the keys themselves.
    /// Runs after vanilla has redrawn the grid, so the badges follow the items as they move.
    /// </summary>
    internal static class LockOverlay
    {
        private const string BadgeName = "TidyChests Lock";

        private const float BadgeSize = 16f;

        private const float BadgeInset = 3f;

        private static readonly Color ItemLockColor = new Color(0.85f, 0.85f, 0.85f, 0.6f);

        private static readonly Color SlotLockColor = new Color(1f, 0.3f, 0.3f, 0.7f);

        private static readonly Dictionary<InventoryElement, Image> Badges = new Dictionary<InventoryElement, Image>();

        private static StashLocks _locks = StashLocks.Parse("", "");

        private static string _locksSource = "";

        private static Sprite? _sprite;

        /// <summary>Called after <c>InventoryGrid.UpdateGui</c>; <paramref name="player"/> is null for a container's grid.</summary>
        public static void AfterUpdateGui(InventoryGrid grid, Player? player)
        {
            try
            {
                if (player == null || !Plugin.Enabled || grid.GetInventory() == null)
                {
                    HideBadges(grid);
                    return;
                }

                ModConfig settings = Plugin.Settings;
                SyncLocks(settings);
                bool reveal = settings.RevealKey.Value != KeyCode.None && ZInput.GetKey(settings.RevealKey.Value, logWarning: false);
                UpdateBadges(grid, reveal);
                UpdateHovered(grid, player, settings, reveal);
            }
            catch (Exception exception)
            {
                Plugin.Log.LogError($"Lock overlay failed: {exception}");
            }
        }

        /// <summary>Re-reads the config lines when they changed, by hand or through a toggle.</summary>
        private static void SyncLocks(ModConfig settings)
        {
            string source = (settings.LockedItems.Value ?? "") + "\n" + (settings.LockedSlots.Value ?? "");
            if (source == _locksSource)
            {
                return;
            }

            _locksSource = source;
            _locks = settings.BuildLocks();
        }

        private static void UpdateBadges(InventoryGrid grid, bool reveal)
        {
            Inventory inventory = grid.GetInventory();
            foreach (InventoryElement element in grid.m_elements)
            {
                Vector2i position = element.Position;
                bool slotLocked = reveal && _locks.IsSlotLocked(position.x, position.y);
                bool itemLocked = false;
                if (reveal && !slotLocked)
                {
                    ItemDrop.ItemData? item = inventory.GetItemAt(position.x, position.y);
                    itemLocked = item != null && _locks.IsItemLocked(item.m_shared.m_name);
                }

                Image? badge = GetBadge(element, create: slotLocked || itemLocked);
                if (badge == null)
                {
                    continue;
                }

                badge.enabled = slotLocked || itemLocked;
                if (badge.enabled)
                {
                    badge.color = slotLocked ? SlotLockColor : ItemLockColor;
                }
            }
        }

        private static void UpdateHovered(InventoryGrid grid, Player player, ModConfig settings, bool reveal)
        {
            InventoryElement? element = grid.GetHoveredElement();
            if (element == null)
            {
                return;
            }

            Vector2i position = grid.GetElementPos(element);
            ItemDrop.ItemData? item = grid.GetInventory().GetItemAt(position.x, position.y);

            if (Shortcut.IsPressed(settings.LockSlotKey.Value))
            {
                bool locked = _locks.ToggleSlot(position.x, position.y);
                settings.SaveLocks(_locks);
                player.Message(MessageHud.MessageType.TopLeft, Translations.Get(locked ? Translations.SlotLocked : Translations.SlotUnlocked));
            }
            else if (item != null && Shortcut.IsPressed(settings.LockItemKey.Value))
            {
                bool locked = _locks.ToggleItem(item.m_shared.m_name);
                settings.SaveLocks(_locks);
                string name = Localization.instance.Localize(item.m_shared.m_name);
                player.Message(MessageHud.MessageType.TopLeft, Translations.Get(locked ? Translations.ItemLocked : Translations.ItemUnlocked, name));
            }

            if (reveal)
            {
                string itemState = Translations.Get(item != null && _locks.IsItemLocked(item.m_shared.m_name) ? Translations.LockStateOn : Translations.LockStateOff);
                string slotState = Translations.Get(_locks.IsSlotLocked(position.x, position.y) ? Translations.LockStateOn : Translations.LockStateOff);
                string hint = Translations.Get(Translations.LockHint, itemState, settings.LockItemKey.Value, slotState, settings.LockSlotKey.Value);
                element.m_tooltip.Set(Translations.Get(Translations.LockTopic), hint, grid.m_tooltipAnchor);
            }
            else if (item == null && UITooltip.m_current == element.m_tooltip)
            {
                // Vanilla blanks an empty slot's tooltip without hiding the one already on screen.
                UITooltip.HideTooltip();
            }
        }

        private static void HideBadges(InventoryGrid grid)
        {
            foreach (InventoryElement element in grid.m_elements)
            {
                Image? badge = GetBadge(element, create: false);
                if (badge != null)
                {
                    badge.enabled = false;
                }
            }
        }

        /// <summary>The element's badge, made on first use; null when there is none and <paramref name="create"/> is off.</summary>
        private static Image? GetBadge(InventoryElement element, bool create)
        {
            if (Badges.TryGetValue(element, out Image? badge) && badge != null)
            {
                return badge;
            }

            if (!create)
            {
                return null;
            }

            var go = new GameObject(BadgeName, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(element.transform, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(BadgeInset, -BadgeInset);
            rect.sizeDelta = new Vector2(BadgeSize, BadgeSize);

            badge = go.AddComponent<Image>();
            badge.sprite = _sprite ??= LockSprite.Create();
            badge.raycastTarget = false;
            badge.preserveAspect = true;
            Badges[element] = badge;
            return badge;
        }
    }

    /// <summary>A padlock drawn pixel by pixel: white, so the badge's colour does the tinting.</summary>
    internal static class LockSprite
    {
        private const int Size = 32;

        public static Sprite Create()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    pixels[y * Size + x] = IsLockPixel(x + 0.5f, y + 0.5f) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>Body in the lower half, a shackle over it, a keyhole in the body. y grows upwards.</summary>
        private static bool IsLockPixel(float x, float y)
        {
            const float centreX = 16f;
            bool body = x >= 5f && x < 27f && y >= 2f && y < 17f;
            if (body)
            {
                float keyholeDx = x - centreX;
                float keyholeDy = y - 11f;
                bool keyholeRing = keyholeDx * keyholeDx + keyholeDy * keyholeDy < 2.2f * 2.2f;
                bool keyholeSlit = Mathf.Abs(keyholeDx) < 1.2f && y >= 5f && y < 11f;
                return !(keyholeRing || keyholeSlit);
            }

            float dx = x - centreX;
            float dy = y - 17f;
            if (y >= 17f)
            {
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                return distance >= 5.5f && distance < 8.5f;
            }

            return false;
        }
    }

    /// <summary>Draws the locks and reads the lock keys after every redraw of an inventory grid.</summary>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    internal static class InventoryGrid_UpdateGui_Patch
    {
        private static void Postfix(InventoryGrid __instance, Player player)
        {
            LockOverlay.AfterUpdateGui(__instance, player);
        }
    }
}
