using System.Collections.Generic;
using UnityEngine;

namespace OreFinder.Detection
{
    /// <summary>
    /// Classifies loaded objects as ore or not, once per prefab. An object qualifies through
    /// its drop table: <c>MineRock5</c> (copper, silver, flametal), <c>MineRock</c> (tin,
    /// obsidian) and <c>DropOnDestroyed</c> (scrap piles) are inspected. An intact deposit is
    /// a plain <c>Destructible</c> that spawns the fractured <c>MineRock5</c> on its first
    /// destroy, so the spawned prefab is inspected as well.
    /// </summary>
    public sealed class OreCatalog
    {
        private readonly Dictionary<int, OreKind?> _byPrefab = new Dictionary<int, OreKind?>();

        private readonly List<string> _dropNames = new List<string>();

        private readonly List<KeyValuePair<string, string>> _drops = new List<KeyValuePair<string, string>>();

        public OreCatalog(OreFilter filter)
        {
            Filter = filter;
        }

        public OreFilter Filter { get; }

        /// <summary>The ore kind of a loaded object, or null when it is not ore. Cached per prefab.</summary>
        public OreKind? Classify(ZNetView view)
        {
            int prefab = view.GetZDO().GetPrefab();
            if (_byPrefab.TryGetValue(prefab, out OreKind? known))
            {
                return known;
            }

            OreKind? kind = Inspect(view.gameObject);
            _byPrefab[prefab] = kind;
            return kind;
        }

        private OreKind? Inspect(GameObject go)
        {
            _drops.Clear();
            _dropNames.Clear();
            string objectName = Collect(go, 0);

            if (_dropNames.Count == 0)
            {
                return null;
            }

            string prefabName = Utils.GetPrefabName(go);
            if (!Filter.TryMatch(prefabName, _dropNames, out string oreItem))
            {
                return null;
            }

            return new OreKind(oreItem, DisplayNameFor(objectName, oreItem), OreKind.ColorFor(oreItem));
        }

        /// <summary>
        /// Gathers the drops of the object and, through <c>Destructible.m_spawnWhenDestroyed</c>,
        /// of what it turns into when destroyed. Returns the object's hover name, or the name of
        /// what it turns into, or an empty string.
        /// </summary>
        private string Collect(GameObject go, int depth)
        {
            string name = string.Empty;

            MineRock5 rock5 = go.GetComponent<MineRock5>();
            if (rock5 != null)
            {
                AddDrops(rock5.m_dropItems);
                name = rock5.m_name;
            }

            MineRock rock = go.GetComponent<MineRock>();
            if (rock != null)
            {
                AddDrops(rock.m_dropItems);
                if (name.Length == 0)
                {
                    name = rock.m_name;
                }
            }

            DropOnDestroyed dropOnDestroyed = go.GetComponent<DropOnDestroyed>();
            if (dropOnDestroyed != null)
            {
                AddDrops(dropOnDestroyed.m_dropWhenDestroyed);
            }

            HoverText hover = go.GetComponent<HoverText>();
            if (name.Length == 0 && hover != null)
            {
                name = hover.m_text;
            }

            Destructible destructible = go.GetComponent<Destructible>();
            if (destructible != null && destructible.m_spawnWhenDestroyed != null && depth < 2)
            {
                string spawnedName = Collect(destructible.m_spawnWhenDestroyed, depth + 1);
                if (name.Length == 0)
                {
                    name = spawnedName;
                }
            }

            return name;
        }

        private void AddDrops(DropTable? table)
        {
            if (table == null)
            {
                return;
            }

            foreach (DropTable.DropData drop in table.m_drops)
            {
                if (drop.m_item == null)
                {
                    continue;
                }

                ItemDrop item = drop.m_item.GetComponent<ItemDrop>();
                string sharedName = item != null && item.m_itemData?.m_shared?.m_name != null
                    ? item.m_itemData.m_shared.m_name
                    : string.Empty;

                _drops.Add(new KeyValuePair<string, string>(drop.m_item.name, sharedName));
                _dropNames.Add(drop.m_item.name);
                if (sharedName.Length > 0)
                {
                    _dropNames.Add(sharedName);
                }
            }
        }

        /// <summary>The object's own name (deposit, vein, pile), else the ore item's name, else the raw name.</summary>
        private string DisplayNameFor(string objectName, string oreItem)
        {
            if (objectName.Length > 0)
            {
                return Localize(objectName);
            }

            foreach (KeyValuePair<string, string> drop in _drops)
            {
                if ((drop.Key == oreItem || drop.Value == oreItem) && drop.Value.Length > 0)
                {
                    return Localize(drop.Value);
                }
            }

            return oreItem;
        }

        private static string Localize(string text)
        {
            Localization localization = Localization.instance;
            string localized = localization != null ? localization.Localize(text) : text;
            return string.IsNullOrEmpty(localized) ? text : localized;
        }
    }
}
