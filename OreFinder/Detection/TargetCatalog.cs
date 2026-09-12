using System.Collections.Generic;
using UnityEngine;

namespace OreFinder.Detection
{
    /// <summary>What to look for besides ores, mirrored from the config.</summary>
    public sealed class TargetOptions
    {
        public bool Dungeons { get; set; } = true;

        public bool Roots { get; set; } = true;

        public NameList Pickables { get; set; } = NameList.Parse(string.Empty);

        public NameList Trees { get; set; } = NameList.Parse(string.Empty);

        public string Describe()
        {
            var parts = new List<string>();
            if (Dungeons)
            {
                parts.Add("dungeon entrances");
            }

            if (Roots)
            {
                parts.Add("roots");
            }

            if (!Pickables.IsEmpty)
            {
                parts.Add($"pickables ({Pickables.Describe()})");
            }

            if (!Trees.IsEmpty)
            {
                parts.Add($"trees ({Trees.Describe()})");
            }

            return parts.Count == 0 ? "none" : string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Classifies loaded objects, once per prefab. Ores qualify through their drop table:
    /// <c>MineRock5</c> (copper, silver, flametal), <c>MineRock</c> (tin, obsidian) and
    /// <c>DropOnDestroyed</c> (scrap piles); an intact deposit is a plain <c>Destructible</c>
    /// that spawns the fractured <c>MineRock5</c> on its first destroy, so the spawned prefab
    /// is inspected as well. Dungeon entrances are <c>Teleport</c> objects with an enter text,
    /// roots are <c>ResourceRoot</c>, pickables are <c>Pickable</c> objects whose item is in
    /// the list, trees are <c>TreeBase</c> objects whose log chain drops a listed wood.
    /// </summary>
    public sealed class TargetCatalog
    {
        private readonly Dictionary<int, TargetKind?> _byPrefab = new Dictionary<int, TargetKind?>();

        private readonly List<string> _dropNames = new List<string>();

        private readonly List<KeyValuePair<string, string>> _drops = new List<KeyValuePair<string, string>>();

        private bool _hidden;

        public TargetCatalog(OreFilter ores, TargetOptions targets, OreNames? names)
        {
            Ores = ores;
            Targets = targets;
            Names = names;
        }

        public OreFilter Ores { get; }

        public TargetOptions Targets { get; }

        /// <summary>The player's own names, or null to use the game's names.</summary>
        public OreNames? Names { get; }

        /// <summary>The kind of a loaded object, or null when it is not a target. Cached per prefab.</summary>
        public TargetKind? Classify(ZNetView view)
        {
            int prefab = view.GetZDO().GetPrefab();
            if (_byPrefab.TryGetValue(prefab, out TargetKind? known))
            {
                return known;
            }

            TargetKind? kind = Inspect(view.gameObject);
            _byPrefab[prefab] = kind;
            return kind;
        }

        private TargetKind? Inspect(GameObject go)
        {
            string prefabName = Utils.GetPrefabName(go);
            return InspectOre(go, prefabName)
                   ?? InspectDungeon(go, prefabName)
                   ?? InspectRoot(go, prefabName)
                   ?? InspectPickable(go, prefabName)
                   ?? InspectTree(go, prefabName);
        }

        private TargetKind? InspectOre(GameObject go, string prefabName)
        {
            _drops.Clear();
            _dropNames.Clear();
            _hidden = false;
            string objectName = Collect(go, 0);
            if (_dropNames.Count == 0 || !Ores.TryMatch(prefabName, _dropNames, out string oreItem))
            {
                return null;
            }

            string displayName = objectName.Length > 0 ? Localize(objectName) : ItemNameFor(oreItem);
            return Make(TargetGroup.Ore, oreItem, prefabName, displayName, _hidden);
        }

        private TargetKind? InspectDungeon(GameObject go, string prefabName)
        {
            if (!Targets.Dungeons)
            {
                return null;
            }

            Teleport teleport = go.GetComponent<Teleport>();
            if (teleport == null || string.IsNullOrEmpty(teleport.m_enterText))
            {
                // Exits inside the dungeons have no enter text.
                return null;
            }

            return Make(TargetGroup.Dungeon, teleport.m_enterText, prefabName, Localize(teleport.m_enterText), false);
        }

        private TargetKind? InspectRoot(GameObject go, string prefabName)
        {
            if (!Targets.Roots)
            {
                return null;
            }

            ResourceRoot root = go.GetComponent<ResourceRoot>();
            if (root == null)
            {
                return null;
            }

            return Make(TargetGroup.Root, root.m_name, prefabName, Localize(root.m_name), false);
        }

        private TargetKind? InspectPickable(GameObject go, string prefabName)
        {
            if (Targets.Pickables.IsEmpty)
            {
                return null;
            }

            Pickable pickable = go.GetComponent<Pickable>();
            if (pickable == null || pickable.m_itemPrefab == null)
            {
                return null;
            }

            string item = pickable.m_itemPrefab.name;
            ItemDrop drop = pickable.m_itemPrefab.GetComponent<ItemDrop>();
            string shared = drop != null && drop.m_itemData?.m_shared?.m_name != null ? drop.m_itemData.m_shared.m_name : string.Empty;
            if (!Targets.Pickables.Contains(item) && !Targets.Pickables.Contains(shared) && !Targets.Pickables.Contains(prefabName))
            {
                return null;
            }

            return Make(TargetGroup.Pickable, item, prefabName, Localize(pickable.GetHoverName()), false);
        }

        private TargetKind? InspectTree(GameObject go, string prefabName)
        {
            if (Targets.Trees.IsEmpty)
            {
                return null;
            }

            TreeBase tree = go.GetComponent<TreeBase>();
            if (tree == null)
            {
                return null;
            }

            _drops.Clear();
            _dropNames.Clear();
            AddDrops(tree.m_dropWhenDestroyed);
            CollectLogs(tree.m_logPrefab, 0);

            string wood = string.Empty;
            foreach (string name in _dropNames)
            {
                if (Targets.Trees.Contains(name))
                {
                    wood = name;
                    break;
                }
            }

            if (wood.Length == 0)
            {
                if (!Targets.Trees.Contains(prefabName))
                {
                    return null;
                }

                wood = prefabName;
            }

            return Make(TargetGroup.Tree, wood, prefabName, ItemNameFor(wood), false);
        }

        /// <summary>Drops of the log the tree falls into and of the logs that one splits into.</summary>
        private void CollectLogs(GameObject? log, int depth)
        {
            if (log == null || depth > 4)
            {
                return;
            }

            TreeLog treeLog = log.GetComponent<TreeLog>();
            if (treeLog == null)
            {
                return;
            }

            AddDrops(treeLog.m_dropWhenDestroyed);
            CollectLogs(treeLog.m_subLogPrefab, depth + 1);
        }

        /// <summary>
        /// Gathers the drops of the object and, through <c>Destructible.m_spawnWhenDestroyed</c>,
        /// of what it turns into when destroyed. Notes a <c>Beacon</c> on either. Returns the
        /// object's hover name, or the name of what it turns into, or an empty string.
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

            if (go.GetComponent<Beacon>() != null)
            {
                _hidden = true;
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

        /// <summary>The localised name of a collected drop, or the raw name.</summary>
        private string ItemNameFor(string itemName)
        {
            foreach (KeyValuePair<string, string> drop in _drops)
            {
                if ((drop.Key == itemName || drop.Value == itemName) && drop.Value.Length > 0)
                {
                    return Localize(drop.Value);
                }
            }

            return itemName;
        }

        /// <summary>Applies the player's own name or, with custom names on, the initials of the game's name.</summary>
        private TargetKind Make(TargetGroup group, string key, string prefabName, string displayName, bool hidden)
        {
            string shown = displayName;
            if (Names != null)
            {
                shown = Names.TryGet(key, prefabName, out string custom) ? custom : OreNames.Initials(displayName);
            }

            return new TargetKind(group, key, shown, TargetKind.ColorFor(group, key), hidden);
        }

        private static string Localize(string text)
        {
            Localization localization = Localization.instance;
            string localized = localization != null ? localization.Localize(text) : text;
            return string.IsNullOrEmpty(localized) ? text : localized;
        }
    }
}
