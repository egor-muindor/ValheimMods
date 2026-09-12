using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using TidyChests.Containers;
using TidyChests.Hud;
using UnityEngine;

namespace TidyChests.Find
{
    /// <summary>
    /// The find key: with the inventory open, the item under the cursor (or the gamepad
    /// selection) is looked up in every container in range; the inventory closes and the
    /// containers that hold it are highlighted. Lives on the plugin's game object, so it
    /// survives world changes; the highlights are dropped when the player is gone.
    /// </summary>
    public sealed class ChestFinder : MonoBehaviour
    {
        private readonly List<ChestHighlight> _highlights = new List<ChestHighlight>();

        private readonly List<Container> _nearby = new List<Container>();

        private bool _overlayFailed;

        /// <summary>Chests currently highlighted.</summary>
        public int HighlightCount => _highlights.Count;

        /// <summary>
        /// Highlights every container in range that holds an item named <paramref name="sharedName"/>.
        /// Returns the number of containers found; the caller decides about the inventory.
        /// </summary>
        public int Find(string sharedName, string displayName)
        {
            ModConfig settings = Plugin.Settings;
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return 0;
            }

            RemoveHighlights();
            _nearby.Clear();
            ContainerRegistry.CollectNearby(player.transform.position, settings.Radius.Value, _nearby);
            HighlightOptions options = settings.ToHighlightOptions();

            foreach (Container container in _nearby)
            {
                Inventory? inventory = container.GetInventory();
                if (inventory == null || IsCarriedByAnotherPlayer(container))
                {
                    continue;
                }

                int count = inventory.CountItems(sharedName, -1, matchWorldLevel: false);
                if (count <= 0)
                {
                    continue;
                }

                try
                {
                    _highlights.Add(new ChestHighlight(container, $"{count}x {displayName}", options));
                    Plugin.Debug($"  {ContainerAccess.NameOf(container)} at {Vector3.Distance(container.transform.position, player.transform.position).ToString("0.#", CultureInfo.InvariantCulture)} m holds {count}");
                }
                catch (Exception exception)
                {
                    Plugin.Log.LogWarning($"Could not highlight {ContainerAccess.NameOf(container)}: {exception.Message}");
                }
            }

            return _highlights.Count;
        }

        public void RemoveHighlights()
        {
            foreach (ChestHighlight highlight in _highlights)
            {
                highlight.Remove();
            }

            _highlights.Clear();
        }

        private void Update()
        {
            try
            {
                if (!Plugin.Enabled || Player.m_localPlayer == null)
                {
                    if (_highlights.Count > 0)
                    {
                        RemoveHighlights();
                    }

                    return;
                }

                UpdateHighlights();
                if (InventoryGui.IsVisible() && IsPressed(Plugin.Settings.FindKey.Value))
                {
                    FindHovered();
                }
            }
            catch (Exception exception)
            {
                Plugin.Log.LogError($"Update failed: {exception}");
            }
        }

        private void OnGUI()
        {
            if (_overlayFailed || _highlights.Count == 0)
            {
                return;
            }

            ModConfig settings = Plugin.Settings;
            if (!settings.Enabled.Value || !settings.ScreenMarker.Value || Event.current.type != EventType.Repaint)
            {
                return;
            }

            Player player = Player.m_localPlayer;
            if (player == null || global::Hud.IsUserHidden() || Minimap.IsOpen() || Menu.IsVisible())
            {
                return;
            }

            Camera camera = Utils.GetMainCamera();
            if (camera == null)
            {
                return;
            }

            try
            {
                MarkerOverlay.Draw(_highlights, camera, player.transform.position);
            }
            catch (Exception exception)
            {
                _overlayFailed = true;
                Plugin.Log.LogError($"Screen markers disabled until restart: {exception}");
            }
        }

        private void OnDestroy()
        {
            RemoveHighlights();
        }

        private void UpdateHighlights()
        {
            for (int i = _highlights.Count - 1; i >= 0; i--)
            {
                ChestHighlight highlight = _highlights[i];
                if (highlight.Expired)
                {
                    highlight.Remove();
                    _highlights.RemoveAt(i);
                }
                else
                {
                    highlight.Update();
                }
            }
        }

        private void FindHovered()
        {
            InventoryGui gui = InventoryGui.instance;
            Player player = Player.m_localPlayer;
            if (gui == null || player == null)
            {
                return;
            }

            ItemDrop.ItemData? item = HoveredItem(gui.m_playerGrid) ?? HoveredItem(gui.m_containerGrid);
            if (item == null && ZInput.IsExclusiveGamepadActive())
            {
                item = gui.m_playerGrid.GetGamepadSelectedItem() ?? gui.m_containerGrid.GetGamepadSelectedItem();
            }

            if (item == null)
            {
                player.Message(MessageHud.MessageType.TopLeft, Translations.Get(Translations.Hover, Plugin.Settings.FindKey.Value));
                return;
            }

            string displayName = Localization.instance.Localize(item.m_shared.m_name);
            Plugin.Debug($"Find {item.m_shared.m_name} within {Plugin.Settings.Radius.Value.ToString("0.#", CultureInfo.InvariantCulture)} m");
            int found = Find(item.m_shared.m_name, displayName);
            if (found == 0)
            {
                string radius = Plugin.Settings.Radius.Value.ToString("0.#", CultureInfo.InvariantCulture);
                player.Message(MessageHud.MessageType.Center, Translations.Get(Translations.NotFound, displayName, radius));
                return;
            }

            if (Plugin.Settings.CloseInventory.Value)
            {
                gui.Hide();
            }

            player.Message(MessageHud.MessageType.Center, Translations.Get(Translations.Found, displayName, found));
        }

        /// <summary>The item under the pointer in <paramref name="grid"/>, the way the tooltip finds it.</summary>
        private static ItemDrop.ItemData? HoveredItem(InventoryGrid? grid)
        {
            if (grid == null || !grid.gameObject.activeInHierarchy || grid.GetInventory() == null)
            {
                return null;
            }

            InventoryElement element = grid.GetHoveredElement();
            if (element == null)
            {
                return null;
            }

            Vector2i position = grid.GetElementPos(element);
            return grid.GetInventory().GetItemAt(position.x, position.y);
        }

        private static bool IsCarriedByAnotherPlayer(Container container)
        {
            ZNetView view = container.m_nview;
            if (view == null)
            {
                return false;
            }

            Player carrier = view.GetComponent<Player>();
            return carrier != null && carrier != Player.m_localPlayer;
        }

        /// <summary>
        /// The shortcut's main key went down this frame with its modifiers held. Unlike
        /// <c>KeyboardShortcut.IsDown</c>, other keys may be held as well. Ignored while a
        /// text field has the keyboard.
        /// </summary>
        private static bool IsPressed(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None)
            {
                return false;
            }

            if (global::Console.IsVisible() || TextInput.IsVisible() || (Chat.instance != null && Chat.instance.HasFocus()))
            {
                return false;
            }

            if (!ZInput.GetKeyDown(shortcut.MainKey, logWarning: false))
            {
                return false;
            }

            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!ZInput.GetKey(modifier, logWarning: false))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
