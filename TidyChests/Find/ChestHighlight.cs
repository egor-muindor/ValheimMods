using System.Collections.Generic;
using UnityEngine;

namespace TidyChests.Find
{
    /// <summary>
    /// One highlighted chest: a pulsing point light above it and an emissive tint on its own
    /// materials, for a fixed time. Nothing is attached to the chest's game object; the tint is
    /// a material property block that is cleared when the highlight ends.
    /// </summary>
    public sealed class ChestHighlight
    {
        /// <summary>Warm gold, readable against wood and stone.</summary>
        public static readonly Color Color = new Color(1f, 0.82f, 0.3f);

        private const float LightIntensity = 4f;

        /// <summary>Last part of the duration over which everything fades out.</summary>
        private const float FadeFraction = 0.25f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Container _container;

        private readonly float _start;

        private readonly float _duration;

        private readonly List<Renderer> _glowing = new List<Renderer>();

        private GameObject? _root;

        private Light? _light;

        public ChestHighlight(Container container, string label, HighlightOptions options)
        {
            _container = container;
            Label = label;
            _start = Time.time;
            _duration = Mathf.Max(0.5f, options.Duration);

            Bounds bounds = BoundsOf(container.gameObject, out bool hasBounds);
            Position = hasBounds ? bounds.center : container.transform.position + Vector3.up * 0.5f;
            float extent = hasBounds ? Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) : 1f;
            float top = hasBounds ? bounds.max.y : Position.y + 0.5f;

            _root = new GameObject("TidyChests highlight");
            _root.transform.position = Position;

            try
            {
                Build(options, top, extent, container.gameObject);
            }
            catch
            {
                Remove();
                throw;
            }
        }

        /// <summary>Text for the screen marker, for example "40x Wood".</summary>
        public string Label { get; }

        /// <summary>Centre of the chest's visible geometry.</summary>
        public Vector3 Position { get; }

        /// <summary>True once the time is up or the chest is gone (destroyed or unloaded).</summary>
        public bool Expired => _container == null || Time.time - _start >= _duration;

        /// <summary>1 while fully shown, falling to 0 over the last quarter of the duration.</summary>
        public float Alpha
        {
            get
            {
                float remaining = _duration - (Time.time - _start);
                return Mathf.Clamp01(remaining / (_duration * FadeFraction));
            }
        }

        private void Build(HighlightOptions options, float top, float extent, GameObject target)
        {
            if (options.Light)
            {
                var lightObject = new GameObject("Light");
                lightObject.transform.SetParent(_root!.transform, false);
                // Above the chest: a light inside the mesh would light nothing.
                lightObject.transform.position = new Vector3(Position.x, top + 0.8f, Position.z);
                _light = lightObject.AddComponent<Light>();
                _light.type = LightType.Point;
                _light.color = Color;
                _light.range = Mathf.Clamp(extent * 3f + 3f, 4f, 12f);
                _light.intensity = LightIntensity;
                _light.shadows = LightShadows.None;
                _light.renderMode = LightRenderMode.ForcePixel;
            }

            if (options.Glow)
            {
                ApplyGlow(target, Color);
            }

            Update();
        }

        /// <summary>Pulses the light. Call once per frame.</summary>
        public void Update()
        {
            if (_light != null)
            {
                _light.intensity = LightIntensity * (0.7f + 0.3f * Mathf.Sin(Time.time * 5f)) * Alpha;
            }
        }

        /// <summary>Removes the light and restores the chest's materials.</summary>
        public void Remove()
        {
            foreach (Renderer renderer in _glowing)
            {
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(null);
                }
            }

            _glowing.Clear();

            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            _light = null;
        }

        private void ApplyGlow(GameObject target, Color color)
        {
            var block = new MaterialPropertyBlock();
            Color emission = color * 0.5f;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                {
                    continue;
                }

                Material shared = renderer.sharedMaterial;
                if (shared == null || !shared.HasProperty(EmissionColorId))
                {
                    continue;
                }

                renderer.GetPropertyBlock(block);
                block.SetColor(EmissionColorId, emission);
                renderer.SetPropertyBlock(block);
                _glowing.Add(renderer);
            }
        }

        /// <summary>World bounds of the enabled mesh renderers of the chest.</summary>
        private static Bounds BoundsOf(GameObject target, out bool found)
        {
            var bounds = new Bounds();
            found = false;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(false))
            {
                if (!renderer.enabled || (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)))
                {
                    continue;
                }

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }
    }
}
