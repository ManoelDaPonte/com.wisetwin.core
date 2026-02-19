using UnityEngine;
using System.Collections;

namespace WiseTwin
{
    /// <summary>
    /// Effet visuel de collecte joué quand le joueur entre dans une zone de validation.
    /// 3 phases : Flash (blanc) → Implosion (scale vers zéro) → Désactivation.
    /// Utilisation : ZoneCollectEffect.Play(zoneGameObject)
    /// </summary>
    public class ZoneCollectEffect : MonoBehaviour
    {
        private const float FlashDuration = 0.15f;
        private const float ImplodeDuration = 0.35f; // 0.15 → 0.5 total

        /// <summary>
        /// Lance l'effet de collecte sur un GameObject zone.
        /// Ajoute le composant qui démarre automatiquement la coroutine.
        /// </summary>
        public static void Play(GameObject zoneRoot)
        {
            if (zoneRoot == null) return;

            // Éviter les doublons
            var existing = zoneRoot.GetComponent<ZoneCollectEffect>();
            if (existing != null) return;

            zoneRoot.AddComponent<ZoneCollectEffect>();
        }

        void Start()
        {
            StartCoroutine(CollectSequence());
        }

        IEnumerator CollectSequence()
        {
            var zoneRoot = gameObject;
            Vector3 originalScale = zoneRoot.transform.localScale;

            // Récupérer tous les Renderers enfants
            var renderers = zoneRoot.GetComponentsInChildren<Renderer>(true);
            var particles = zoneRoot.GetComponentsInChildren<ParticleSystem>(true);

            // === Phase 1 : Flash blanc ===
            // Appliquer émission blanche sur tous les renderers
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", Color.white * 5f);

                // Mettre la couleur principale en blanc aussi
                if (r.material.HasProperty("_Color"))
                {
                    r.material.SetColor("_Color", Color.white);
                }
                if (r.material.HasProperty("_BaseColor"))
                {
                    r.material.SetColor("_BaseColor", Color.white);
                }
            }

            // Flash les particules en blanc
            foreach (var ps in particles)
            {
                if (ps == null) continue;
                var main = ps.main;
                main.startColor = Color.white;
            }

            yield return new WaitForSeconds(FlashDuration);

            // === Phase 2 : Implosion ===
            // Stopper et clear les particules (SimulationSpace.World = on ne peut pas les "tirer" vers le centre)
            foreach (var ps in particles)
            {
                if (ps == null) continue;
                ps.Clear();
                ps.Stop();
            }

            float elapsed = 0f;
            while (elapsed < ImplodeDuration)
            {
                if (zoneRoot == null) yield break; // Sécurité changement de scène

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ImplodeDuration);
                float easedT = t * t * t; // Ease-in cubique

                zoneRoot.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, easedT);
                yield return null;
            }

            // === Phase 3 : Désactivation ===
            if (zoneRoot != null)
            {
                zoneRoot.transform.localScale = Vector3.zero;
                zoneRoot.SetActive(false);
                Destroy(this);
            }
        }
    }
}
