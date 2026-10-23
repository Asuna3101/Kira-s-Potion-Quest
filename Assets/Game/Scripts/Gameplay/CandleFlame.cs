using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Anima SOLO la llama de una vela: un resplandor calido que parpadea detras de
    /// la llama pintada, un nucleo mas claro y brasas que suben de vez en cuando.
    /// La cera no se toca (antes Flicker oscurecia la vela entera).
    ///
    /// Se coloca en el centro de la llama (LevelBuilder.BuildCandle). Las velas de
    /// Melanie son un solo sprite sin frames; si algun dia entrega la llama en
    /// frames, esta animacion se reemplaza por un Animator.
    /// </summary>
    public class CandleFlame : MonoBehaviour
    {
        [Header("Resplandor")]
        public float glowSize = 1.1f;
        public Color glowColor = new Color(1f, 0.62f, 0.2f, 1f);
        [Range(0f, 1f)] public float glowAlphaMin = 0.16f;
        [Range(0f, 1f)] public float glowAlphaMax = 0.4f;

        [Header("Nucleo")]
        public float coreSize = 0.42f;
        public Color coreColor = new Color(1f, 0.93f, 0.62f, 1f);

        [Header("Parpadeo")]
        public float flickerSpeed = 1f;

        [Header("Brasas")]
        public float emberIntervalMin = 0.6f;
        public float emberIntervalMax = 1.6f;

        [Header("Orden de dibujo")]
        public string sortingLayer = "Tablero";
        // un punto por debajo de las velas (20) para que la llama pintada quede encima
        public int glowOrder = Level01Art.OrdenObstaculos - 1;

        SpriteRenderer m_Glow;
        SpriteRenderer m_Core;
        float m_Time;
        float m_EmberTimer;

        void Awake()
        {
            m_Glow = MakeLayer("Glow", glowColor, glowOrder);
            m_Core = MakeLayer("Core", coreColor, glowOrder);
            m_Time = Random.Range(0f, 20f);   // que las velas no parpadeen sincronizadas
            m_EmberTimer = Random.Range(emberIntervalMin, emberIntervalMax);
        }

        SpriteRenderer MakeLayer(string layerName, Color color, int order)
        {
            var go = new GameObject(layerName);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FxSprites.Glow;
            sr.color = color;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            Material mat = PlaceholderArt.UnlitSpriteMaterial();
            if (mat != null) sr.sharedMaterial = mat;
            return sr;
        }

        void Update()
        {
            m_Time += Time.deltaTime * flickerSpeed;

            // Ruido suave: suma de ondas que no se repiten juntas.
            float n = 0.5f
                      + 0.25f * Mathf.Sin(m_Time * 5.3f)
                      + 0.15f * Mathf.Sin(m_Time * 9.7f + 1.3f)
                      + 0.10f * Mathf.Sin(m_Time * 17.1f + 0.4f);
            n = Mathf.Clamp01(n);

            float glowScale = glowSize * Mathf.Lerp(0.85f, 1.2f, n);
            m_Glow.transform.localScale = new Vector3(glowScale, glowScale * 1.15f, 1f);
            m_Glow.color = new Color(glowColor.r, glowColor.g, glowColor.b,
                Mathf.Lerp(glowAlphaMin, glowAlphaMax, n));

            // el nucleo se balancea un poco, como la punta de una llama
            float coreScale = coreSize * Mathf.Lerp(0.9f, 1.15f, n);
            m_Core.transform.localScale = new Vector3(coreScale * 0.8f, coreScale * 1.2f, 1f);
            m_Core.transform.localPosition = new Vector3(Mathf.Sin(m_Time * 3.1f) * 0.015f, -0.03f, 0f);
            m_Core.color = new Color(coreColor.r, coreColor.g, coreColor.b,
                Mathf.Lerp(0.12f, 0.3f, n));

            // brasas
            m_EmberTimer -= Time.deltaTime;
            if (m_EmberTimer <= 0f)
            {
                m_EmberTimer = Random.Range(emberIntervalMin, emberIntervalMax);
                Vector2 origin = (Vector2)transform.position + new Vector2(Random.Range(-0.05f, 0.05f), 0.12f);
                Vector2 velocity = new Vector2(Random.Range(-0.12f, 0.12f), Random.Range(0.4f, 0.8f));
                FxParticle.Spawn(transform.parent, FxSprites.Disc, origin, velocity, 0f, 0.4f,
                    Random.Range(0.9f, 1.4f), Random.Range(0.045f, 0.07f), 0.01f,
                    new Color(1f, 0.7f, 0.25f, 1f), 0f, 0.3f, "FX", 4);
            }
        }
    }
}
