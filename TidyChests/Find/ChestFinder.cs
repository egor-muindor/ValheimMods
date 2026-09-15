using System;
using System.Collections.Generic;
using System.Globalization;
using TidyChests.Containers;
using TidyChests.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace TidyChests.Find
{
    /// <summary>
    /// The find key: with the inventory open, the item under the cursor (in the inventory
    /// grids, or an ingredient or recipe in the crafting panel, or the gamepad selection) is
    /// looked up in every container in range; the inventory closes and the containers that
    /// hold it are highlighted. Lives on the plugin's game object, so it
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
        /// Highlights every container within the stash radius that holds an item named
        /// <paramref name="sharedName"/>. Returns the number of containers found; the caller
        /// decides about the inventory.
        /// </summary>
        public int Find(string sharedName, string displayName)
        {
            return Find(sharedName, displayName, Plugin.Settings.Radius.Value);
        }

        /// <summary>
        /// Highlights every container within <paramref name="radius"/> metres that holds an
        /// item named <paramref name="sharedName"/>. The chest list searches a wider radius
        /// than the find key, so it passes its own.
        /// </summary>
        public int Find(string sharedName, string displayName, float radius)
        {
            ModConfig settings = Plugin.Settings;
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return 0;
            }

            RemoveHighlights();
            _nearby.Clear();
            ContainerRegistry.CollectNearby(player.transform.position, radius, _nearby);
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
                if (InventoryGui.IsVisible() && Shortcut.IsPressed(Plugin.Settings.FindKey.Value))
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

            string? sharedName = HoveredInventoryItem(gui)?.m_shared.m_name ?? HoveredCraftingItem(gui);
            if (sharedName == null)
            {
                player.Message(MessageHud.MessageType.TopLeft, Translations.Get(Translations.Hover, Plugin.Settings.FindKey.Value));
                return;
            }

            string displayName = Localization.instance.Localize(sharedName);
            Plugin.Debug($"Find {sharedName} within {Plugin.Settings.Radius.Value.ToString("0.#", CultureInfo.InvariantCulture)} m");
            int found = Find(sharedName, displayName);
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

        /// <summary>The item under the pointer (or the gamepad selection) in the player's or the open container's grid.</summary>
        private static ItemDrop.ItemData? HoveredInventoryItem(InventoryGui gui)
        {
            ItemDrop.ItemData? item = HoveredItem(gui.m_playerGrid) ?? HoveredItem(gui.m_containerGrid);
            if (item == null && ZInput.IsExclusiveGamepadActive())
            {
                item = gui.m_playerGrid.GetGamepadSelectedItem() ?? gui.m_containerGrid.GetGamepadSelectedItem();
            }

            return item;
        }

        /// <summary>
        /// The item under the pointer in the crafting panel: an ingredient of the selected
        /// recipe, the selected recipe's own icon, or an entry of the recipe list. Returns the
        /// item's shared name, or null when the pointer is over none of them.
        /// </summary>
        private static string? HoveredCraftingItem(InventoryGui gui)
        {
            Vector3 pointer = ZInput.pointerPosition;

            // Ingredients: matched by icon, because the shown subset rotates when more ingredients exist than slots.
            foreach (GameObject element in gui.m_recipeRequirementList)
            {
                if (element == null)
                {
                    continue;
                }

                Transform icon = element.transform.Find("res_icon");
                if (icon == null || !icon.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!Contains(element.transform as RectTransform, pointer) && !Contains(icon as RectTransform, pointer))
                {
                    continue;
                }

                Sprite? sprite = icon.GetComponent<Image>()?.sprite;
                foreach (Piece.Requirement requirement in gui.m_reqList)
                {
                    ItemDrop resource = requirement.m_resItem;
                    if (resource != null && sprite != null && resource.m_itemData.GetIcon() == sprite)
                    {
                        return resource.m_itemData.m_shared.m_name;
                    }
                }
            }

            // The selected recipe's icon.
            Recipe selected = gui.m_selectedRecipe.Recipe;
            if (selected != null && selected.m_item != null && gui.m_recipeIcon != null && gui.m_recipeIcon.enabled
                && Contains(gui.m_recipeIcon.rectTransform, pointer))
            {
                return selected.m_item.m_itemData.m_shared.m_name;
            }

            // The recipe list: only entries inside the scroll viewport are really under the pointer.
            RectTransform? viewport = gui.m_recipeListRoot != null ? gui.m_recipeListRoot.parent as RectTransform : null;
            if (viewport != null && Contains(viewport, pointer))
            {
                foreach (InventoryGui.RecipeDataPair pair in gui.m_availableRecipes)
                {
                    GameObject element = pair.InterfaceElement;
                    if (element != null && pair.Recipe != null && pair.Recipe.m_item != null && Contains(element.transform as RectTransform, pointer))
                    {
                        return pair.Recipe.m_item.m_itemData.m_shared.m_name;
                    }
                }
            }

            return null;
        }

        /// <summary>True when the pointer is inside a visible rectangle, the way the inventory grid tests its cells.</summary>
        private static bool Contains(RectTransform? rect, Vector3 pointer)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy)
            {
                return false;
            }

            Vector2 point = rect.InverseTransformPoint(pointer);
            return rect.rect.Contains(point);
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

    }
}
