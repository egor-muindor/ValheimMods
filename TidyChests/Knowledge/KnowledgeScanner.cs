using System;
using System.Collections.Generic;
using TidyChests.Index;
using UnityEngine;

namespace TidyChests.Knowledge
{
    /// <summary>
    /// Teaches the player the items lying in the chests around them, so a recipe whose
    /// ingredients a team mate gathered stops being locked. Vanilla only unlocks a recipe once
    /// the material has been in your own inventory (<c>Player.m_knownMaterial</c>), which in a
    /// party means hunting down every new stack and touching it.
    ///
    /// Costs nothing on the wire: the chest contents are already replicated to this client (see
    /// <see cref="ChestIndex"/>) and the known materials are local player data, saved in the
    /// character file rather than in a ZDO.
    ///
    /// Items go through the public <see cref="Player.AddKnownItem"/>, so trophies, the unlock
    /// popups and the recipe list stay vanilla's business. That call rebuilds the whole recipe
    /// list, so only a couple of items are handed over per frame: a base full of unknown
    /// materials unlocks over a second instead of freezing a frame, and the popup queue plays
    /// at its own pace anyway.
    /// </summary>
    public sealed class KnowledgeScanner : MonoBehaviour
    {
        /// <summary>Items handed to the game per frame while the queue is not empty.</summary>
        private const int LearnsPerFrame = 2;

        /// <summary>How many unknown items one scan may queue up.</summary>
        private const int QueueLimit = 64;

        private readonly Queue<string> _pending = new Queue<string>();

        /// <summary>Names already queued, plus the ones the game refused; neither is queued again.</summary>
        private readonly HashSet<string> _skip = new HashSet<string>(StringComparer.Ordinal);

        private float _nextScan;

        private Player? _player;

        /// <summary>Items learned from chests since the world was entered, for the console command.</summary>
        public int Learned { get; private set; }

        /// <summary>Items found but not handed to the game yet.</summary>
        public int Pending => _pending.Count;

        /// <summary>Scans now, whatever the timer says, and returns how many new items were queued.</summary>
        public int ScanNow()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                return 0;
            }

            _nextScan = Time.time + Plugin.Settings.ScanInterval.Value;
            return Scan(player);
        }

        private void Update()
        {
            try
            {
                Player player = Player.m_localPlayer;
                if (player != _player)
                {
                    Forget();
                    _player = player;
                }

                if (player == null || !Plugin.Enabled || !Plugin.Settings.LearnFromChests.Value)
                {
                    if (_pending.Count > 0)
                    {
                        Forget();
                    }

                    return;
                }

                Learn(player);

                if (Time.time >= _nextScan)
                {
                    _nextScan = Time.time + Plugin.Settings.ScanInterval.Value;
                    Scan(player);
                }
            }
            catch (Exception exception)
            {
                Plugin.Log.LogError($"Knowledge scan failed: {exception}");
                _nextScan = Time.time + 30f;
            }
        }

        /// <summary>Looks at the chests in range and queues the item kinds the player does not know.</summary>
        private int Scan(Player player)
        {
            ChestIndex index = Plugin.Index;
            index.Refresh(player.transform.position, Plugin.Settings.ScanRadius.Value);

            int room = QueueLimit - _pending.Count;
            List<string> batch = KnowledgePlan.NextBatch(index.Names, player.m_knownMaterial, _skip, room);
            foreach (string name in batch)
            {
                _skip.Add(name);
                _pending.Enqueue(name);
            }

            if (batch.Count > 0)
            {
                Plugin.Debug($"Knowledge scan: {batch.Count} unknown items in {index.Chests.Count} chests within " +
                             $"{Plugin.Settings.ScanRadius.Value:0.#} m");
            }

            return batch.Count;
        }

        /// <summary>Hands the next few queued items to the game.</summary>
        private void Learn(Player player)
        {
            for (int i = 0; i < LearnsPerFrame && _pending.Count > 0; i++)
            {
                string name = _pending.Dequeue();
                if (player.m_knownMaterial.Contains(name))
                {
                    continue;
                }

                if (!Plugin.Index.TryGetSample(name, out ItemDrop.ItemData item) || item == null)
                {
                    // The chest is gone; a later scan queues it again if it is still around.
                    _skip.Remove(name);
                    continue;
                }

                try
                {
                    player.AddKnownItem(item);
                    Learned++;
                    Plugin.Debug($"Learned {name} from a nearby chest");
                }
                catch (Exception exception)
                {
                    // Keep it in _skip so a broken item is not retried every scan.
                    Plugin.Log.LogWarning($"Could not learn {name} from a nearby chest: {exception.Message}");
                }
            }
        }

        private void Forget()
        {
            _pending.Clear();
            _skip.Clear();
            Learned = 0;
        }
    }
}
