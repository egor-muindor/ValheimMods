using System;
using System.Collections.Generic;
using System.Text;

namespace DeathTweaks.Rules
{
    /// <summary>
    /// Immutable, pre-parsed item rules. Decides the <see cref="ItemFate"/> of every item
    /// in the dying player's inventory.
    ///
    /// Precedence, first match wins:
    /// <list type="number">
    /// <item>world <c>DeathKeepInventory</c> or <c>KeepAllItems</c>: keep</item>
    /// <item>quest item: keep</item>
    /// <item>equipped and world <c>DeathKeepEquip</c>: keep</item>
    /// <item><c>DestroyAllItems</c>: destroy</item>
    /// <item><c>KeepEquippedItems</c>, <c>KeepHotbarItems</c>, <c>KeepQuickSlotItems</c>: keep</item>
    /// <item>destroy lists: destroy</item>
    /// <item>keep lists: keep</item>
    /// <item>drop lists: drop</item>
    /// <item><c>KeepTeleportableItems</c>: keep</item>
    /// <item>otherwise: drop</item>
    /// </list>
    /// A resulting drop is turned into destroy by world <c>DeathDeleteItems</c>, or by
    /// <c>DeathDeleteUnequipped</c> when the item is not equipped.
    /// </summary>
    public sealed class DeathRules
    {
        private static readonly char[] Separators = { ',', ';' };

        private readonly DeathRuleSettings _settings;
        private readonly HashSet<string> _keepTypes;
        private readonly HashSet<string> _dropTypes;
        private readonly HashSet<string> _destroyTypes;
        private readonly HashSet<string> _keepNames;
        private readonly HashSet<string> _dropNames;
        private readonly HashSet<string> _destroyNames;

        private DeathRules(
            DeathRuleSettings settings,
            HashSet<string> keepTypes,
            HashSet<string> dropTypes,
            HashSet<string> destroyTypes,
            HashSet<string> keepNames,
            HashSet<string> dropNames,
            HashSet<string> destroyNames)
        {
            _settings = settings;
            _keepTypes = keepTypes;
            _dropTypes = dropTypes;
            _destroyTypes = destroyTypes;
            _keepNames = keepNames;
            _dropNames = dropNames;
            _destroyNames = destroyNames;
        }

        /// <summary>
        /// Parses the raw settings. <paramref name="knownTypeNames"/> is the list of valid
        /// item type names; unknown entries in the type lists are reported through
        /// <paramref name="warn"/> and ignored.
        /// </summary>
        public static DeathRules Parse(DeathRuleSettings settings, ICollection<string> knownTypeNames, Action<string>? warn = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (knownTypeNames == null) throw new ArgumentNullException(nameof(knownTypeNames));

            var known = new HashSet<string>(knownTypeNames, StringComparer.OrdinalIgnoreCase);

            return new DeathRules(
                settings,
                ParseTypes("KeepItemTypes", settings.KeepItemTypes, known, warn),
                ParseTypes("DropItemTypes", settings.DropItemTypes, known, warn),
                ParseTypes("DestroyItemTypes", settings.DestroyItemTypes, known, warn),
                ParseList(settings.KeepItemNames),
                ParseList(settings.DropItemNames),
                ParseList(settings.DestroyItemNames));
        }

        public ItemFate Resolve(in ItemFacts item, WorldDeathModifiers world)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));

            if (world.KeepInventory || _settings.KeepAllItems)
            {
                return ItemFate.Keep;
            }

            if (item.QuestItem)
            {
                return ItemFate.Keep;
            }

            if (item.Equipped && world.KeepEquip)
            {
                return ItemFate.Keep;
            }

            ItemFate fate = ResolveFromSettings(item);

            if (fate == ItemFate.Drop)
            {
                if (world.DeleteItems || (world.DeleteUnequipped && !item.Equipped))
                {
                    return ItemFate.Destroy;
                }
            }

            return fate;
        }

        /// <summary>One-line summary of the active rules, for logs and the console command.</summary>
        public string Describe()
        {
            var sb = new StringBuilder();

            AppendFlag(sb, "KeepAllItems", _settings.KeepAllItems);
            AppendFlag(sb, "DestroyAllItems", _settings.DestroyAllItems);
            AppendFlag(sb, "KeepEquippedItems", _settings.KeepEquippedItems);
            AppendFlag(sb, "KeepHotbarItems", _settings.KeepHotbarItems);
            AppendFlag(sb, "KeepQuickSlotItems", _settings.KeepQuickSlotItems);
            AppendFlag(sb, "KeepTeleportableItems", _settings.KeepTeleportableItems);
            AppendList(sb, "KeepItemTypes", _keepTypes);
            AppendList(sb, "DropItemTypes", _dropTypes);
            AppendList(sb, "DestroyItemTypes", _destroyTypes);
            AppendList(sb, "KeepItems", _keepNames);
            AppendList(sb, "DropItems", _dropNames);
            AppendList(sb, "DestroyItems", _destroyNames);

            return sb.Length == 0 ? "no item rules active (everything is dropped)" : sb.ToString();
        }

        private ItemFate ResolveFromSettings(in ItemFacts item)
        {
            if (_settings.DestroyAllItems)
            {
                return ItemFate.Destroy;
            }

            if (_settings.KeepEquippedItems && item.Equipped)
            {
                return ItemFate.Keep;
            }

            if (_settings.KeepHotbarItems && item.Hotbar)
            {
                return ItemFate.Keep;
            }

            if (_settings.KeepQuickSlotItems && item.QuickSlot)
            {
                return ItemFate.Keep;
            }

            if (_destroyTypes.Contains(item.TypeName) || MatchesName(_destroyNames, item))
            {
                return ItemFate.Destroy;
            }

            if (_keepTypes.Contains(item.TypeName) || MatchesName(_keepNames, item))
            {
                return ItemFate.Keep;
            }

            if (_dropTypes.Contains(item.TypeName) || MatchesName(_dropNames, item))
            {
                return ItemFate.Drop;
            }

            if (_settings.KeepTeleportableItems && item.Teleportable)
            {
                return ItemFate.Keep;
            }

            return ItemFate.Drop;
        }

        private static bool MatchesName(HashSet<string> names, in ItemFacts item)
        {
            if (names.Count == 0)
            {
                return false;
            }

            return (item.PrefabName.Length > 0 && names.Contains(item.PrefabName))
                || (item.SharedName.Length > 0 && names.Contains(item.SharedName));
        }

        private static HashSet<string> ParseTypes(string settingName, string raw, HashSet<string> known, Action<string>? warn)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string entry in ParseList(raw))
            {
                if (known.Contains(entry))
                {
                    result.Add(entry);
                }
                else
                {
                    warn?.Invoke($"{settingName}: unknown item type '{entry}' ignored. Valid types: {string.Join(", ", ToSortedArray(known))}");
                }
            }

            return result;
        }

        private static HashSet<string> ParseList(string raw)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(raw))
            {
                return result;
            }

            foreach (string part in raw.Split(Separators))
            {
                string entry = part.Trim();
                if (entry.Length > 0)
                {
                    result.Add(entry);
                }
            }

            return result;
        }

        private static string[] ToSortedArray(HashSet<string> set)
        {
            var array = new string[set.Count];
            set.CopyTo(array);
            Array.Sort(array, StringComparer.OrdinalIgnoreCase);
            return array;
        }

        private static void AppendFlag(StringBuilder sb, string name, bool value)
        {
            if (!value)
            {
                return;
            }

            if (sb.Length > 0) sb.Append("; ");
            sb.Append(name);
        }

        private static void AppendList(StringBuilder sb, string name, HashSet<string> values)
        {
            if (values.Count == 0)
            {
                return;
            }

            if (sb.Length > 0) sb.Append("; ");
            sb.Append(name).Append('=').Append(string.Join(",", ToSortedArray(values)));
        }
    }
}
