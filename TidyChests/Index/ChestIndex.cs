using System.Collections.Generic;
using TidyChests.Containers;
using UnityEngine;

namespace TidyChests.Index
{
    /// <summary>
    /// What the containers in range hold. Reading costs nothing on the wire: the game already
    /// replicates every chest's inventory to this client (<c>ZDOMan</c> sends the ZDO data of
    /// everything in the active area) and re-reads it once a second
    /// (<c>Container.CheckForChanges</c>), so the index only looks at the parsed inventory that
    /// is already in memory. A container is re-read only when its ZDO data revision changed,
    /// which makes a rescan of an unchanged base one <c>uint</c> comparison per chest.
    ///
    /// Containers the local player could not open by hand are left out, so a private chest or
    /// someone else's ward hides its contents here as well.
    /// </summary>
    internal sealed class ChestIndex
    {
        private const uint NeverRead = uint.MaxValue;

        private readonly Dictionary<Container, Cached> _cache = new Dictionary<Container, Cached>();

        private readonly Dictionary<string, string> _displayNames = new Dictionary<string, string>();

        private readonly Dictionary<string, ItemDrop.ItemData> _samples = new Dictionary<string, ItemDrop.ItemData>();

        private readonly List<ChestContents> _chests = new List<ChestContents>();

        private readonly List<Container> _nearby = new List<Container>();

        private readonly List<Container> _dead = new List<Container>();

        private readonly Dictionary<string, int> _positions = new Dictionary<string, int>();

        /// <summary>The readable containers of the last refresh, nearest first.</summary>
        public IReadOnlyList<ChestContents> Chests => _chests;

        /// <summary>Every item name found in those containers.</summary>
        public ICollection<string> Names => _samples.Keys;

        /// <summary>Containers looked at by the last refresh, before the access checks.</summary>
        public int NearbyCount => _nearby.Count;

        /// <summary>Re-reads the containers within <paramref name="radius"/> of <paramref name="center"/>.</summary>
        public void Refresh(Vector3 center, float radius)
        {
            _chests.Clear();
            _samples.Clear();
            _nearby.Clear();

            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            long playerId = player.GetPlayerID();
            ContainerRegistry.CollectNearby(center, radius, _nearby);

            foreach (Container container in _nearby)
            {
                // Not logged: the browser refreshes several times a second, and the stash
                // already reports its skip reasons.
                if (!ContainerAccess.CanRead(container, playerId, out string _))
                {
                    continue;
                }

                Cached cached = Snapshot(container);
                if (cached.Items.Count == 0)
                {
                    continue;
                }

                float distance = Vector3.Distance(container.transform.position, center);
                _chests.Add(new ChestContents(_chests.Count, cached.Name, distance, cached.Items));

                for (int i = 0; i < cached.Items.Count; i++)
                {
                    string name = cached.Items[i].Name;
                    if (!_samples.ContainsKey(name))
                    {
                        _samples[name] = cached.Samples[i];
                    }
                }
            }

            Prune();
        }

        /// <summary>An item of that kind from one of the containers, for its icon and its shared data.</summary>
        public bool TryGetSample(string name, out ItemDrop.ItemData item)
        {
            return _samples.TryGetValue(name, out item);
        }

        /// <summary>Drops everything, including the localized names. Used on a language or world change.</summary>
        public void Clear()
        {
            _cache.Clear();
            _displayNames.Clear();
            _samples.Clear();
            _chests.Clear();
            _nearby.Clear();
        }

        /// <summary>The contents of one container, re-read only when its ZDO data revision moved.</summary>
        private Cached Snapshot(Container container)
        {
            uint revision = container.m_nview.GetZDO().DataRevision;
            if (!_cache.TryGetValue(container, out Cached cached))
            {
                cached = new Cached();
                _cache[container] = cached;
            }
            else if (cached.Revision == revision)
            {
                return cached;
            }

            cached.Revision = revision;
            cached.Name = ContainerAccess.NameOf(container);
            cached.Items.Clear();
            cached.Samples.Clear();
            _positions.Clear();

            foreach (ItemDrop.ItemData item in container.GetInventory().GetAllItems())
            {
                if (item == null || item.m_shared == null || string.IsNullOrEmpty(item.m_shared.m_name))
                {
                    continue;
                }

                string name = item.m_shared.m_name;
                int units = Mathf.Max(1, item.m_stack);
                if (_positions.TryGetValue(name, out int position))
                {
                    ChestItem known = cached.Items[position];
                    cached.Items[position] = new ChestItem(name, known.DisplayName, known.Count + units);
                    continue;
                }

                _positions[name] = cached.Items.Count;
                cached.Items.Add(new ChestItem(name, DisplayName(name), units));
                cached.Samples.Add(item);
            }

            _positions.Clear();
            return cached;
        }

        private string DisplayName(string sharedName)
        {
            if (_displayNames.TryGetValue(sharedName, out string displayName))
            {
                return displayName;
            }

            Localization localization = Localization.m_instance;
            displayName = localization != null ? localization.Localize(sharedName) : sharedName;
            _displayNames[sharedName] = displayName;
            return displayName;
        }

        /// <summary>Forgets containers the game destroyed without telling us (zone unloads).</summary>
        private void Prune()
        {
            foreach (KeyValuePair<Container, Cached> entry in _cache)
            {
                // Unity's == answers "destroyed" as well as "null"; the reference itself is
                // still a usable dictionary key, which is what the removal below needs.
                if (entry.Key == null)
                {
                    _dead.Add(entry.Key!);
                }
            }

            foreach (Container container in _dead)
            {
                _cache.Remove(container);
            }

            _dead.Clear();
        }

        private sealed class Cached
        {
            public uint Revision = NeverRead;

            public string Name = "";

            /// <summary>One entry per item kind, its stacks already added up.</summary>
            public readonly List<ChestItem> Items = new List<ChestItem>();

            /// <summary>An item of each kind in <see cref="Items"/>, same order.</summary>
            public readonly List<ItemDrop.ItemData> Samples = new List<ItemDrop.ItemData>();
        }
    }
}
