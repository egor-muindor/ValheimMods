using System.Collections.Generic;
using UnityEngine;

namespace OreFinder.Detection
{
    /// <summary>Finds the loaded net objects around a point.</summary>
    public static class OreScanner
    {
        /// <summary>
        /// Adds every loaded object within <paramref name="radius"/> of <paramref name="center"/>
        /// to <paramref name="results"/>. Walks <c>ZNetScene</c>'s instance table: rocks have no
        /// registry of their own and a physics query would return every collider of every rock.
        /// </summary>
        public static void CollectNearby(Vector3 center, float radius, List<ZNetView> results)
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null)
            {
                return;
            }

            float radiusSquared = radius * radius;
            foreach (KeyValuePair<ZDO, ZNetView> instance in scene.m_instances)
            {
                // The ZDO position is a plain field; the transform would be a native call per object.
                if ((instance.Key.GetPosition() - center).sqrMagnitude > radiusSquared)
                {
                    continue;
                }

                ZNetView view = instance.Value;
                if (view != null && view.IsValid())
                {
                    results.Add(view);
                }
            }
        }
    }
}
