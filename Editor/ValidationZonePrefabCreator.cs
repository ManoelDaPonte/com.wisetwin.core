using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

namespace WiseTwin.Editor
{
    /// <summary>
    /// Creates a validation zone prefab: transparent green disc on the ground
    /// with a glowing ring on the perimeter and soft upward particles.
    /// Supports both URP and Built-in render pipeline.
    /// </summary>
    public static class ValidationZonePrefabCreator
    {
        [MenuItem("WiseTwin/Create Validation Zone Prefab")]
        public static void CreateValidationZonePrefab()
        {
            float radius = 1.5f;

            // ── Root ──
            var root = new GameObject("ValidationZone");

            // Trigger collider
            var collider = root.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = radius;
            collider.center = new Vector3(0f, 1f, 0f);

            // ── 1. Ground disc (flat transparent green circle) ──
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "GroundDisc";
            disc.transform.SetParent(root.transform);
            disc.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            disc.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

            var discCollider = disc.GetComponent<Collider>();
            if (discCollider != null)
                Object.DestroyImmediate(discCollider);

            var discMaterial = CreateTransparentMaterial(
                "ZoneDiscMaterial",
                new Color(0.1f, 0.8f, 0.4f, 0.12f),
                new Color(0.05f, 0.4f, 0.2f, 1f) * 0.5f
            );
            disc.GetComponent<Renderer>().sharedMaterial = discMaterial;

            // ── 2. Glowing ring (LineRenderer circle on perimeter) ──
            var ringGO = new GameObject("GlowRing");
            ringGO.transform.SetParent(root.transform);
            ringGO.transform.localPosition = Vector3.zero;

            var ringMaterial = CreateAdditiveMaterial(
                "ZoneRingMaterial",
                new Color(0.2f, 1f, 0.5f, 0.8f)
            );

            var line = ringGO.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = false;
            line.widthMultiplier = 0.12f;
            line.sharedMaterial = ringMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCornerVertices = 4;

            // Color gradient: glow brighter at bottom, fade up
            var colorGrad = new Gradient();
            colorGrad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.2f, 1f, 0.5f), 0f),
                    new GradientColorKey(new Color(0.3f, 1f, 0.6f), 0.5f),
                    new GradientColorKey(new Color(0.2f, 1f, 0.5f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.9f, 1f)
                }
            );
            line.colorGradient = colorGrad;

            // Draw circle
            int segments = 64;
            line.positionCount = segments;
            float ringRadius = radius - 0.05f; // Slightly inside the disc edge
            for (int i = 0; i < segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                float x = Mathf.Cos(angle) * ringRadius;
                float z = Mathf.Sin(angle) * ringRadius;
                line.SetPosition(i, new Vector3(x, 0.05f, z));
            }

            // ── 3. Upward particles (rotate entire GO to force upward) ──
            var particleGO = new GameObject("UpwardGlow");
            particleGO.transform.SetParent(root.transform);
            particleGO.transform.localPosition = Vector3.zero;
            // Rotate the entire particle GO so +Y of particle space points world UP
            // Cone emits along local +Y, so rotating -90 on X makes it world +Y
            particleGO.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            var ps = particleGO.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime = 1.8f;
            main.startSpeed = 1f;
            main.startSize = 0.06f;
            main.startColor = new Color(0.3f, 1f, 0.5f, 0.4f);
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.rateOverTime = 40f;

            // Cone shape: angle=0 (cylinder), emit from base edge
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0f;
            shape.radius = ringRadius;
            shape.radiusThickness = 0f; // Edge only
            shape.arc = 360f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

            // Size over lifetime: shrink
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(0.5f, 0.5f);
            sizeCurve.AddKey(1f, 0f);
            sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Color over lifetime: fade in/out
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.2f, 1f, 0.5f), 0f),
                    new GradientColorKey(new Color(0.3f, 1f, 0.6f), 0.3f),
                    new GradientColorKey(new Color(0.2f, 0.8f, 0.4f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.5f, 0.15f),
                    new GradientAlphaKey(0.3f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = grad;

            // Particle material
            var particleMat = CreateAdditiveMaterial(
                "ZoneParticleMaterial",
                new Color(0.3f, 1f, 0.5f, 0.5f)
            );
            var psRenderer = particleGO.GetComponent<ParticleSystemRenderer>();
            psRenderer.sharedMaterial = particleMat;
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;

            // ── Save as prefab ──
            string prefabDir = "Packages/com.wisetwin.core/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabDir))
                AssetDatabase.CreateFolder("Packages/com.wisetwin.core", "Prefabs");

            string materialDir = $"{prefabDir}/Materials";
            if (!AssetDatabase.IsValidFolder(materialDir))
                AssetDatabase.CreateFolder(prefabDir, "Materials");

            // Clean up old materials
            DeleteAssetIfExists($"{materialDir}/ZoneDiscMaterial.mat");
            DeleteAssetIfExists($"{materialDir}/ZoneRingMaterial.mat");
            DeleteAssetIfExists($"{materialDir}/ZoneParticleMaterial.mat");
            DeleteAssetIfExists($"{materialDir}/ZoneBeamMaterial.mat");
            DeleteAssetIfExists($"{materialDir}/ZoneMaterial.mat");

            AssetDatabase.CreateAsset(discMaterial, $"{materialDir}/ZoneDiscMaterial.mat");
            AssetDatabase.CreateAsset(ringMaterial, $"{materialDir}/ZoneRingMaterial.mat");
            AssetDatabase.CreateAsset(particleMat, $"{materialDir}/ZoneParticleMaterial.mat");

            string prefabPath = $"{prefabDir}/ValidationZone.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Validation Zone Prefab Created",
                $"Prefab saved to:\n{prefabPath}\n\nDrag it into your scene and reference it as the Zone Object in your procedure step.",
                "OK");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        static Material CreateTransparentMaterial(string name, Color color, Color emissionColor)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            bool isURP = shader != null;
            if (!isURP) shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.name = name;

            if (isURP)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 0);
                mat.SetFloat("_AlphaClip", 0);
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetFloat("_ZWrite", 0);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetColor("_BaseColor", color);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }
            else
            {
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                mat.color = color;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emissionColor);
            }

            return mat;
        }

        static Material CreateAdditiveMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            bool isURP = shader != null;
            if (!isURP) shader = Shader.Find("Particles/Standard Unlit");

            var mat = new Material(shader);
            mat.name = name;

            if (isURP)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetFloat("_Blend", 2); // Additive
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                mat.SetFloat("_ZWrite", 0);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_BLENDMODE_ADD");
                mat.SetColor("_BaseColor", color);
            }
            else
            {
                mat.color = color;
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            }

            return mat;
        }

        static void DeleteAssetIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }
    }
}

#endif
