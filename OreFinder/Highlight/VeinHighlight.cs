using System.Collections.Generic;
using OreFinder.Detection;
using UnityEngine;
using UnityEngine.Rendering;

namespace OreFinder.Highlight
{
    /// <summary>
    /// One highlighted vein: a pulsing point light above it, a vertical beam and an emissive
    /// tint on its own materials, for a fixed time. Nothing is attached to the vein's game
    /// object, so the game's own mesh handling is untouched; the tint is a material property
    /// block that is cleared when the highlight ends.
    /// </summary>
    public sealed class VeinHighlight
    {
        private const float BeamHeight = 30f;

        private const float LightIntensity = 5f;

        /// <summary>Last part of the duration over which everything fades out.</summary>
        private const float FadeFraction = 0.25f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private static readonly string[] BeamShaders =
        {
            "Sprites/Default",
            "UI/Default",
            "Hidden/Internal-Colored",
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Alpha Blended",
        };

        private static Material? _beamMaterial;

        private static bool _beamMaterialSearched;

        private readonly ZNetView _view;

        private readonly float _start;

        private readonly float _duration;

        private readonly List<Renderer> _glowing = new List<Renderer>();

        private GameObject? _root;

        private Light? _light;

        private LineRenderer? _beam;

        public VeinHighlight(ZNetView view, OreKind kind, HighlightOptions options)
        {
            _view = view;
            Kind = kind;
            Id = view.GetZDO().m_uid;
            _start = Time.time;
            _duration = Mathf.Max(0.5f, options.Duration);

            Bounds bounds = BoundsOf(view.gameObject, out bool hasBounds);
            Position = hasBounds ? bounds.center : view.transform.position;
            float extent = hasBounds ? Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) : 2f;
            float top = hasBounds ? bounds.max.y : Position.y + 2f;

            _root = new GameObject("OreFinder highlight");
            _root.transform.position = Position;

            try
            {
                Build(kind, options, top, extent, view.gameObject);
            }
            catch
            {
                Remove();
                throw;
            }
        }

        private void Build(OreKind kind, HighlightOptions options, float top, float extent, GameObject target)
        {
            if (options.Light)
            {
                var lightObject = new GameObject("Light");
                lightObject.transform.SetParent(_root!.transform, false);
                // Above the vein: a light inside the mesh would light nothing.
                lightObject.transform.position = new Vector3(Position.x, top + 1.5f, Position.z);
                _light = lightObject.AddComponent<Light>();
                _light.type = LightType.Point;
                _light.color = kind.Color;
                _light.range = Mathf.Clamp(extent * 3f + 5f, 8f, 30f);
                _light.intensity = LightIntensity;
                _light.shadows = LightShadows.None;
                _light.renderMode = LightRenderMode.ForcePixel;
            }

            if (options.Beam)
            {
                Material? material = BeamMaterial;
                if (material != null)
                {
                    var beamObject = new GameObject("Beam");
                    beamObject.transform.SetParent(_root!.transform, false);
                    _beam = beamObject.AddComponent<LineRenderer>();
                    _beam.sharedMaterial = material;
                    _beam.useWorldSpace = true;
                    _beam.positionCount = 2;
                    _beam.SetPosition(0, Position);
                    _beam.SetPosition(1, Position + Vector3.up * BeamHeight);
                    _beam.startWidth = 0.7f;
                    _beam.endWidth = 0.15f;
                    _beam.alignment = LineAlignment.View;
                    _beam.textureMode = LineTextureMode.Stretch;
                    _beam.shadowCastingMode = ShadowCastingMode.Off;
                    _beam.receiveShadows = false;
                    _beam.lightProbeUsage = LightProbeUsage.Off;
                    _beam.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }
            }

            if (options.Glow)
            {
                ApplyGlow(target, kind.Color);
            }

            Update();
        }

        public ZDOID Id { get; }

        public OreKind Kind { get; }

        /// <summary>Centre of the vein's visible geometry.</summary>
        public Vector3 Position { get; }

        /// <summary>True once the time is up or the vein is gone (mined out or unloaded).</summary>
        public bool Expired => _view == null || Time.time - _start >= _duration;

        /// <summary>1 while fully shown, falling to 0 over the last quarter of the duration.</summary>
        public float Alpha
        {
            get
            {
                float remaining = _duration - (Time.time - _start);
                return Mathf.Clamp01(remaining / (_duration * FadeFraction));
            }
        }

        /// <summary>Pulses the light and fades the beam. Call once per frame.</summary>
        public void Update()
        {
            float alpha = Alpha;
            if (_light != null)
            {
                _light.intensity = LightIntensity * (0.75f + 0.25f * Mathf.Sin(Time.time * 6f)) * alpha;
            }

            if (_beam != null)
            {
                Color bottom = Kind.Color;
                bottom.a = 0.85f * alpha;
                Color top = Kind.Color;
                top.a = 0f;
                _beam.startColor = bottom;
                _beam.endColor = top;
            }
        }

        /// <summary>Removes the light and beam and restores the vein's materials.</summary>
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
            _beam = null;
        }

        private static Material? BeamMaterial
        {
            get
            {
                if (_beamMaterialSearched)
                {
                    return _beamMaterial;
                }

                _beamMaterialSearched = true;
                foreach (string name in BeamShaders)
                {
                    Shader shader = Shader.Find(name);
                    if (shader == null)
                    {
                        continue;
                    }

                    _beamMaterial = new Material(shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                        name = "OreFinder beam",
                    };
                    ConfigureTransparent(_beamMaterial);
                    Plugin.Debug($"Beam shader: {name}");
                    return _beamMaterial;
                }

                Plugin.Log.LogWarning("No shader available for the beam; the beam is disabled. The light, glow and screen marker still work.");
                return null;
            }
        }

        /// <summary>Alpha blending for shaders that expose their blend state as properties (Hidden/Internal-Colored).</summary>
        private static void ConfigureTransparent(Material material)
        {
            if (material.HasProperty("_SrcBlend") && material.HasProperty("_DstBlend"))
            {
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetInt("_ZWrite", 0);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetInt("_Cull", (int)CullMode.Off);
            }

            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private void ApplyGlow(GameObject target, Color color)
        {
            var block = new MaterialPropertyBlock();
            Color emission = color * 0.6f;
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

        /// <summary>World bounds of the enabled renderers, which for a MineRock5 is its combined mesh.</summary>
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
