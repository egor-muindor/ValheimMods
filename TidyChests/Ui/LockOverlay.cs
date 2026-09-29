using System;
using System.Collections.Generic;
using HarmonyLib;
using TidyChests.Restock;
using TidyChests.Stash;
using UnityEngine;
using UnityEngine.UI;

namespace TidyChests.Ui
{
    /// <summary>
    /// The locks and restock marks in the player's inventory grid, shown while the reveal key is
    /// held: a padlock in the top-left corner of every locked item (grey) and every locked slot
    /// (red), an arrow into a tray in the top-right corner of every slot the Restock button fills
    /// (green for a full stack, red for less); a tooltip with the keys on the slot under the
    /// pointer, and the keys themselves. Runs after vanilla has redrawn the grid, so the badges
    /// follow the items as they move.
    /// </summary>
    internal static class LockOverlay
    {
        private const string BadgeName = "TidyChests Lock";

        private const string RestockBadgeName = "TidyChests Restock";

        private const float BadgeSize = 16f;

        private const float BadgeInset = 3f;

        private static readonly Color ItemLockColor = new Color(0.85f, 0.85f, 0.85f, 0.6f);

        private static readonly Color SlotLockColor = new Color(1f, 0.3f, 0.3f, 0.7f);

        private static readonly Color RestockFullColor = new Color(0.35f, 0.9f, 0.35f, 0.9f);

        private static readonly Color RestockPartColor = new Color(1f, 0.3f, 0.3f, 0.9f);

        private static readonly Dictionary<InventoryElement, Image> Badges = new Dictionary<InventoryElement, Image>();

        private static readonly Dictionary<InventoryElement, Image> RestockBadges = new Dictionary<InventoryElement, Image>();

        private static StashLocks _locks = StashLocks.Parse("", "");

        private static string _locksSource = "";

        private static RestockMarks _restock = RestockMarks.Parse("");

        private static string _restockSource = "";

        private static Sprite? _sprite;

        private static Sprite? _restockSprite;

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

        /// <summary>Re-reads the config lines when they changed, by hand, through a toggle or a restock.</summary>
        private static void SyncLocks(ModConfig settings)
        {
            string source = (settings.LockedItems.Value ?? "") + "\n" + (settings.LockedSlots.Value ?? "");
            if (source != _locksSource)
            {
                _locksSource = source;
                _locks = settings.BuildLocks();
            }

            string restock = settings.RestockSlots.Value ?? "";
            if (restock != _restockSource)
            {
                _restockSource = restock;
                _restock = settings.BuildRestockMarks();
            }
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

                Image? badge = GetBadge(Badges, element, create: slotLocked || itemLocked, BadgeName, left: true);
                if (badge != null)
                {
                    badge.enabled = slotLocked || itemLocked;
                    if (badge.enabled)
                    {
                        badge.color = slotLocked ? SlotLockColor : ItemLockColor;
                    }
                }

                int percent = reveal ? _restock.PercentAt(position.x, position.y) : 0;
                Image? restockBadge = GetBadge(RestockBadges, element, create: percent > 0, RestockBadgeName, left: false);
                if (restockBadge != null)
                {
                    restockBadge.enabled = percent > 0;
                    if (restockBadge.enabled)
                    {
                        restockBadge.color = percent >= RestockMarks.FullPercent ? RestockFullColor : RestockPartColor;
                    }
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
            else if (Shortcut.IsPressed(settings.RestockSlotKey.Value))
            {
                string? stackable = item != null && item.m_shared.m_maxStackSize > 1 ? item.m_shared.m_name : null;
                int percent = _restock.Cycle(position.x, position.y, settings.HalfPercent.Value, stackable);
                settings.SaveRestockMarks(_restock);
                _restockSource = settings.RestockSlots.Value ?? "";
                player.Message(MessageHud.MessageType.TopLeft, Translations.Get(Translations.RestockSlot, RestockState(percent)));
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
                string restockState = RestockState(_restock.PercentAt(position.x, position.y));
                string hint = Translations.Get(Translations.LockHint, itemState, settings.LockItemKey.Value, slotState, settings.LockSlotKey.Value, restockState, settings.RestockSlotKey.Value);
                element.m_tooltip.Set(Translations.Get(Translations.LockTopic), hint, grid.m_tooltipAnchor);
            }
            else if (item == null && UITooltip.m_current == element.m_tooltip)
            {
                // Vanilla blanks an empty slot's tooltip without hiding the one already on screen.
                UITooltip.HideTooltip();
            }
        }

        private static string RestockState(int percent)
        {
            if (percent <= 0)
            {
                return Translations.Get(Translations.RestockStateOff);
            }

            return percent >= RestockMarks.FullPercent ? Translations.Get(Translations.RestockStateFull) : Translations.Get(Translations.RestockStatePart, percent);
        }

        private static void HideBadges(InventoryGrid grid)
        {
            foreach (InventoryElement element in grid.m_elements)
            {
                foreach (Dictionary<InventoryElement, Image> badges in new[] { Badges, RestockBadges })
                {
                    if (badges.TryGetValue(element, out Image? badge) && badge != null)
                    {
                        badge.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// The element's badge from <paramref name="badges"/>, made on first use in the top-left
        /// (<paramref name="left"/>) or top-right corner; null when there is none and
        /// <paramref name="create"/> is off.
        /// </summary>
        private static Image? GetBadge(Dictionary<InventoryElement, Image> badges, InventoryElement element, bool create, string name, bool left)
        {
            if (badges.TryGetValue(element, out Image? badge) && badge != null)
            {
                return badge;
            }

            if (!create)
            {
                return null;
            }

            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(element.transform, false);
            var corner = new Vector2(left ? 0f : 1f, 1f);
            rect.anchorMin = corner;
            rect.anchorMax = corner;
            rect.pivot = corner;
            rect.anchoredPosition = new Vector2(left ? BadgeInset : -BadgeInset, -BadgeInset);
            rect.sizeDelta = new Vector2(BadgeSize, BadgeSize);

            badge = go.AddComponent<Image>();
            badge.sprite = left ? (_sprite ??= LockSprite.Create()) : (_restockSprite ??= RestockSprite.Create());
            badge.raycastTarget = false;
            badge.preserveAspect = true;
            badges[element] = badge;
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

    /// <summary>An arrow pointing down into an open tray, drawn pixel by pixel: white, so the badge's colour does the tinting.</summary>
    internal static class RestockSprite
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
                    pixels[y * Size + x] = IsRestockPixel(x + 0.5f, y + 0.5f) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>A tray with no top (floor and two walls) and an arrow coming down into it. y grows upwards.</summary>
        private static bool IsRestockPixel(float x, float y)
        {
            const float centreX = 16f;
            bool floor = x >= 3f && x < 29f && y >= 2f && y < 6f;
            bool walls = (x >= 3f && x < 7f || x >= 25f && x < 29f) && y >= 2f && y < 16f;
            if (floor || walls)
            {
                return true;
            }

            float dx = Mathf.Abs(x - centreX);
            bool shaft = dx < 2.5f && y >= 15f && y < 31f;
            bool head = y >= 8f && y < 17f && dx < (y - 8f) * 0.8f;
            return shaft || head;
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
