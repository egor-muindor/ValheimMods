using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using OreFinder.Detection;
using OreFinder.Highlight;
using OreFinder.Hud;
using OreFinder.Map;
using UnityEngine;

namespace OreFinder
{
    /// <summary>
    /// The finder itself: scans around the local player on a timer, highlights every vein the
    /// first time it comes within the radius, handles the toggle key and draws the markers.
    /// Lives on the plugin's game object, so it survives world changes.
    /// </summary>
    public sealed class Finder : MonoBehaviour
    {
        private readonly List<VeinHighlight> _highlights = new List<VeinHighlight>();

        private readonly HashSet<ZDOID> _seen = new HashSet<ZDOID>();

        /// <summary>
        /// Where the seen veins stand. The first hit on an intact deposit replaces it with a new
        /// object (new id) at the same position; that one must not count as a new vein.
        /// </summary>
        private readonly List<Vector3> _seenPositions = new List<Vector3>();

        /// <summary>Two objects closer than this are the same vein.</summary>
        private const float SameVeinDistance = 0.5f;

        /// <summary>Hidden veins already reported in the log as waiting for the Wishbone.</summary>
        private readonly HashSet<ZDOID> _waitingForWishbone = new HashSet<ZDOID>();

        private readonly List<ZNetView> _nearby = new List<ZNetView>();

        private readonly List<Teleport> _entrances = new List<Teleport>();

        /// <summary>
        /// How much further than <c>TargetRadius</c> a location proxy is looked at: the proxy
        /// stands at the centre of its location, the door can be this far from the centre.
        /// </summary>
        private const float LocationExtent = 50f;

        private TargetCatalog? _catalog;

        private string? _catalogSource;

        private float _nextScan;

        private bool _inWorld;

        private bool _overlayFailed;

        /// <summary>Veins currently highlighted.</summary>
        public int HighlightCount => _highlights.Count;

        /// <summary>Veins highlighted since entering the world.</summary>
        public int SeenCount => _seenPositions.Count;

        /// <summary>Turns the finder on or off, saves the setting and tells the player.</summary>
        public void SetEnabled(bool enabled)
        {
            Plugin.Settings.Enabled.Value = enabled;
            if (!enabled)
            {
                RemoveHighlights();
            }

            ShowMessage(MessageHud.MessageType.Center, enabled ? $"{MyPluginInfo.PLUGIN_NAME}: on" : $"{MyPluginInfo.PLUGIN_NAME}: off");
            Plugin.Log.LogInfo(enabled ? "Enabled" : "Disabled");
        }

        /// <summary>Turns one group of targets on or off, saves the setting and tells the player. Ignored for the list groups.</summary>
        public void SetGroupEnabled(TargetGroup group, bool enabled)
        {
            ConfigEntry<bool>? entry = Plugin.Settings.SwitchFor(group);
            if (entry == null)
            {
                return;
            }

            entry.Value = enabled;
            if (!enabled)
            {
                RemoveHighlights(group);
            }

            string label = TargetGroups.Label(group);
            ShowMessage(MessageHud.MessageType.Center, $"{MyPluginInfo.PLUGIN_NAME}: {label} {(enabled ? "on" : "off")}");
            Plugin.Log.LogInfo($"{label} {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>Forgets the veins already shown, so they are highlighted again. Returns how many were forgotten.</summary>
        public int Reset()
        {
            int count = _seenPositions.Count;
            _seen.Clear();
            _seenPositions.Clear();
            _waitingForWishbone.Clear();
            RemoveHighlights();
            _nextScan = 0f;
            return count;
        }

        private void Awake()
        {
            // Only GUI.DrawTexture and GUI.Label are used; skip the layout pass.
            useGUILayout = false;
        }

        private void Update()
        {
            try
            {
                ModConfig settings = Plugin.Settings;
                if (ZNetScene.instance == null)
                {
                    if (_inWorld)
                    {
                        LeaveWorld();
                    }

                    return;
                }

                _inWorld = true;
                Player player = Player.m_localPlayer;
                if (player == null)
                {
                    // Dead and waiting to respawn: the veins already shown stay remembered.
                    if (_highlights.Count > 0)
                    {
                        RemoveHighlights();
                    }

                    return;
                }

                if (IsPressed(settings.ToggleKey.Value))
                {
                    SetEnabled(!settings.Enabled.Value);
                }

                if (IsPressed(settings.OresToggleKey.Value))
                {
                    SetGroupEnabled(TargetGroup.Ore, !settings.FindOres.Value);
                }

                if (IsPressed(settings.DungeonsToggleKey.Value))
                {
                    SetGroupEnabled(TargetGroup.Dungeon, !settings.Dungeons.Value);
                }

                if (IsPressed(settings.SpawnersToggleKey.Value))
                {
                    SetGroupEnabled(TargetGroup.Spawner, !settings.Spawners.Value);
                }

                if (!settings.Enabled.Value)
                {
                    if (_highlights.Count > 0)
                    {
                        RemoveHighlights();
                    }

                    return;
                }

                UpdateHighlights();
                if (Time.time >= _nextScan)
                {
                    _nextScan = Time.time + Mathf.Max(0.1f, settings.ScanInterval.Value);
                    Scan(player, settings);
                }
            }
            catch (Exception exception)
            {
                Plugin.Log.LogError($"Update failed, retrying in 5 s: {exception}");
                _nextScan = Time.time + 5f;
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

            Vector3 playerPosition = player!.transform.position;
            try
            {
                MarkerOverlay.Draw(_highlights, camera, playerPosition);
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

        private void Scan(Player player, ModConfig settings)
        {
            TargetCatalog catalog = CatalogFor(settings);
            _nearby.Clear();
            Vector3 playerPosition = player.transform.position;
            float radius = Mathf.Max(settings.Radius.Value, settings.TargetRadius.Value);
            OreScanner.CollectNearby(playerPosition, radius, LocationProxyPrefab, settings.TargetRadius.Value + LocationExtent, _nearby);
            bool? hasWishbone = null;

            foreach (ZNetView view in _nearby)
            {
                ZDOID id = view.GetZDO().m_uid;
                if (_seen.Contains(id))
                {
                    continue;
                }

                LocationProxy proxy = view.GetComponent<LocationProxy>();
                if (proxy != null)
                {
                    ScanLocation(view, proxy, catalog, player, playerPosition, settings);
                    continue;
                }

                TargetKind? kind = catalog.Classify(view);
                if (kind == null)
                {
                    continue;
                }

                float groupRadius = kind.Group == TargetGroup.Ore ? settings.Radius.Value : settings.TargetRadius.Value;
                if ((view.transform.position - playerPosition).sqrMagnitude > groupRadius * groupRadius)
                {
                    continue;
                }

                if (kind.Group == TargetGroup.Pickable)
                {
                    // Picked ones are left alone until they regrow; not marked as seen.
                    Pickable pickable = view.GetComponent<Pickable>();
                    if (pickable != null && pickable.m_picked)
                    {
                        continue;
                    }
                }

                if (kind.Hidden && settings.WishboneNeeded.Value != WishboneRule.NotNeeded)
                {
                    hasWishbone ??= HasWishbone(player, settings);
                    if (!hasWishbone.Value)
                    {
                        // Not seen: it is found later, once the Wishbone is carried.
                        if (_waitingForWishbone.Add(id))
                        {
                            Plugin.Debug($"{kind.DisplayName} ({Utils.GetPrefabName(view.gameObject)}) is hidden and waits for the {settings.WishboneItem.Value}");
                        }

                        continue;
                    }
                }

                _seen.Add(id);
                Vector3 origin = view.transform.position;
                if (IsSeenVein(origin))
                {
                    Plugin.Debug($"{kind.DisplayName} at {origin} is a vein already shown (new object after a hit), skipped");
                    continue;
                }

                _seenPositions.Add(origin);
                try
                {
                    Highlight(view, view.gameObject, kind, player, settings);
                }
                catch (Exception exception)
                {
                    Plugin.Log.LogError($"Could not highlight {kind.DisplayName} ({Utils.GetPrefabName(view.gameObject)}): {exception}");
                }
            }
        }

        /// <summary>
        /// The doors of a location, found through its proxy (see <see cref="LocationEntrances"/>).
        /// The proxy counts as seen once every door has been handled; a door still out of range
        /// keeps it in the running, and one already shown is told apart by its position.
        /// </summary>
        private void ScanLocation(ZNetView view, LocationProxy proxy, TargetCatalog catalog, Player player, Vector3 playerPosition, ModConfig settings)
        {
            if (!catalog.Targets.Dungeons || !LocationEntrances.IsSpawned(proxy))
            {
                // Not seen: found later, once dungeons are on and the location has spawned.
                return;
            }

            _entrances.Clear();
            LocationEntrances.Collect(proxy, _entrances);
            bool allHandled = true;
            float radius = settings.TargetRadius.Value;
            string? locationName = null;
            foreach (Teleport entrance in _entrances)
            {
                Vector3 origin = entrance.transform.position;
                if (IsSeenVein(origin))
                {
                    continue;
                }

                if ((origin - playerPosition).sqrMagnitude > radius * radius)
                {
                    allHandled = false;
                    continue;
                }

                locationName ??= LocationEntrances.LocationName(view);
                TargetKind? kind = catalog.ClassifyEntrance(entrance, locationName);
                if (kind == null)
                {
                    continue;
                }

                _seenPositions.Add(origin);
                try
                {
                    Highlight(view, entrance.gameObject, kind, player, settings);
                }
                catch (Exception exception)
                {
                    Plugin.Log.LogError($"Could not highlight {kind.DisplayName} ({locationName}): {exception}");
                }
            }

            if (allHandled)
            {
                _seen.Add(view.GetZDO().m_uid);
            }
        }

        private void Highlight(ZNetView view, GameObject target, TargetKind kind, Player player, ModConfig settings)
        {
            var highlight = new VeinHighlight(view, target, kind, settings.ToHighlightOptions());
            _highlights.Add(highlight);

            float distance = Vector3.Distance(player.transform.position, highlight.Position);
            if (settings.Message.Value)
            {
                ShowMessage(MessageHud.MessageType.TopLeft, $"{kind.DisplayName}: {distance:0} m");
            }

            if (settings.MapPin.Value)
            {
                if (Character.InInterior(highlight.Position))
                {
                    // Inside a dungeon the coordinates are the interior's, far above the zone centre: a pin there points at nothing.
                    Plugin.Debug($"No map pin for {kind.DisplayName}: found inside a dungeon");
                }
                else if (!MapPins.TryAdd(highlight.Position, kind.DisplayName, settings.MapPinSpacing.Value, PinIcons.ToPinType(settings.PinFor(kind.Group))))
                {
                    Plugin.Debug($"No map pin for {kind.DisplayName}: another pin is within {settings.MapPinSpacing.Value:0.#} m");
                }
            }

            Plugin.Debug($"Found {kind.DisplayName} ({Utils.GetPrefabName(target)}, key {kind.Key}) " +
                         $"at {highlight.Position}, {distance:0.#} m away");
        }

        private void UpdateHighlights()
        {
            for (int i = _highlights.Count - 1; i >= 0; i--)
            {
                VeinHighlight highlight = _highlights[i];
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

        private void RemoveHighlights()
        {
            foreach (VeinHighlight highlight in _highlights)
            {
                highlight.Remove();
            }

            _highlights.Clear();
        }

        private void RemoveHighlights(TargetGroup group)
        {
            for (int i = _highlights.Count - 1; i >= 0; i--)
            {
                if (_highlights[i].Kind.Group == group)
                {
                    _highlights[i].Remove();
                    _highlights.RemoveAt(i);
                }
            }
        }

        /// <summary>Prefab hash of the game's location proxy, the net object every location is found through.</summary>
        private static int LocationProxyPrefab
        {
            get
            {
                ZoneSystem zoneSystem = ZoneSystem.instance;
                return zoneSystem != null && zoneSystem.m_locationProxyPrefab != null
                    ? zoneSystem.m_locationProxyPrefab.name.GetStableHashCode()
                    : 0;
            }
        }

        private void LeaveWorld()
        {
            RemoveHighlights();
            _seen.Clear();
            _seenPositions.Clear();
            _waitingForWishbone.Clear();
            _catalog = null;
            _inWorld = false;
        }

        /// <summary>The Wishbone (or the configured item) is in the inventory, or equipped when the rule says so.</summary>
        private static bool HasWishbone(Player player, ModConfig settings)
        {
            Inventory inventory = player.GetInventory();
            if (inventory == null)
            {
                return false;
            }

            string wanted = OreFilter.Normalize(settings.WishboneItem.Value ?? string.Empty);
            if (wanted.Length == 0)
            {
                return false;
            }

            bool mustBeEquipped = settings.WishboneNeeded.Value == WishboneRule.Equipped;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (mustBeEquipped && !item.m_equipped)
                {
                    continue;
                }

                string prefab = item.m_dropPrefab != null ? item.m_dropPrefab.name : string.Empty;
                string shared = item.m_shared != null ? item.m_shared.m_name : string.Empty;
                if (OreFilter.Normalize(prefab) == wanted || OreFilter.Normalize(shared) == wanted)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSeenVein(Vector3 origin)
        {
            foreach (Vector3 seen in _seenPositions)
            {
                if ((seen - origin).sqrMagnitude < SameVeinDistance * SameVeinDistance)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The catalog for the current target and name settings; rebuilt when any of them changes.</summary>
        private TargetCatalog CatalogFor(ModConfig settings)
        {
            string source = settings.CatalogSource();
            if (_catalog == null || _catalogSource != source)
            {
                OreNames? names = settings.CustomNames.Value ? OreNames.Parse(settings.Names.Value) : null;
                _catalog = new TargetCatalog(OreFilter.Parse(settings.Ores.Value), settings.ToTargetOptions(), names);
                _catalogSource = source;
                Plugin.Debug($"Looking for ores: {_catalog.Ores.Describe()}; targets: {_catalog.Targets.Describe()}; " +
                             $"names: {(names != null ? names.Describe() : "from the game")}");
            }

            return _catalog;
        }

        /// <summary>
        /// The shortcut's main key went down this frame with its modifiers held. Unlike
        /// <c>KeyboardShortcut.IsDown</c>, other keys (walking, running) may be held as well.
        /// Ignored while a text field has the keyboard.
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

        private static void ShowMessage(MessageHud.MessageType type, string text)
        {
            MessageHud hud = MessageHud.instance;
            if (hud != null)
            {
                hud.ShowMessage(type, text);
            }
        }
    }
}
