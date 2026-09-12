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

        private readonly List<ZNetView> _nearby = new List<ZNetView>();

        private OreCatalog? _catalog;

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

        /// <summary>Forgets the veins already shown, so they are highlighted again. Returns how many were forgotten.</summary>
        public int Reset()
        {
            int count = _seenPositions.Count;
            _seen.Clear();
            _seenPositions.Clear();
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
            OreCatalog catalog = CatalogFor(settings);
            _nearby.Clear();
            OreScanner.CollectNearby(player.transform.position, settings.Radius.Value, _nearby);

            foreach (ZNetView view in _nearby)
            {
                ZDOID id = view.GetZDO().m_uid;
                if (_seen.Contains(id))
                {
                    continue;
                }

                OreKind? kind = catalog.Classify(view);
                if (kind == null)
                {
                    continue;
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
                    Highlight(view, kind, player, settings);
                }
                catch (Exception exception)
                {
                    Plugin.Log.LogError($"Could not highlight {kind.DisplayName} ({Utils.GetPrefabName(view.gameObject)}): {exception}");
                }
            }
        }

        private void Highlight(ZNetView view, OreKind kind, Player player, ModConfig settings)
        {
            var highlight = new VeinHighlight(view, kind, settings.ToHighlightOptions());
            _highlights.Add(highlight);

            float distance = Vector3.Distance(player.transform.position, highlight.Position);
            if (settings.Message.Value)
            {
                ShowMessage(MessageHud.MessageType.TopLeft, $"{kind.DisplayName}: {distance:0} m");
            }

            if (settings.MapPin.Value && !MapPins.TryAdd(highlight.Position, kind.DisplayName, settings.MapPinSpacing.Value))
            {
                Plugin.Debug($"No map pin for {kind.DisplayName}: another pin is within {settings.MapPinSpacing.Value:0.#} m");
            }

            Plugin.Debug($"Found {kind.DisplayName} ({Utils.GetPrefabName(view.gameObject)}, ore item {kind.OreItem}) " +
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

        private void LeaveWorld()
        {
            RemoveHighlights();
            _seen.Clear();
            _seenPositions.Clear();
            _catalog = null;
            _inWorld = false;
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

        /// <summary>The catalog for the current <c>Ores</c> and <c>Names</c> settings; rebuilt when they change.</summary>
        private OreCatalog CatalogFor(ModConfig settings)
        {
            string ores = settings.Ores.Value ?? string.Empty;
            string names = settings.CustomNames.Value ? settings.Names.Value ?? string.Empty : string.Empty;
            string source = ores + "\n" + settings.CustomNames.Value + "\n" + names;
            if (_catalog == null || _catalogSource != source)
            {
                _catalog = new OreCatalog(OreFilter.Parse(ores), settings.CustomNames.Value ? OreNames.Parse(names) : null);
                _catalogSource = source;
                Plugin.Debug($"Looking for: {_catalog.Filter.Describe()}; names: {(_catalog.Names != null ? _catalog.Names.Describe() : "from the game")}");
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
