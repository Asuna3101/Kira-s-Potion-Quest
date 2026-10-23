using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Hace que el caldero luzca: aura verde que late alrededor de la olla, pocion
    /// brillante en la apertura, humo verde que sube ondulando, burbujas y chispas.
    /// Al entrar un ingrediente suelta una explosion de humo y chispas, y al ganar
    /// el nivel brilla mas fuerte.
    ///
    /// Se coloca en el centro de la apertura de la olla (LevelBuilder.BuildArtCauldron).
    /// Todo es por codigo: si Melanie dibuja efectos propios, se reemplazan los sprites.
    /// </summary>
    public class CauldronFx : MonoBehaviour
    {
        [Header("Apertura de la olla")]
        public float mouthWidth = Level01Art.CalderoAperturaAncho;
        public float mouthHeight = Level01Art.CalderoAperturaAlto;

        [Header("Colores")]
        public Color potionColor = new Color(0.35f, 0.95f, 0.45f, 1f);
        public Color smokeColor = new Color(0.5f, 1f, 0.55f, 1f);
        public Color sparkColor = new Color(0.85f, 1f, 0.55f, 1f);

        [Header("Aura")]
        [Tooltip("Tamano del halo que rodea la olla (u).")]
        public Vector2 auraSize = new Vector2(3.6f, 2.8f);
        [Range(0f, 1f)] public float auraAlphaMin = 0.14f;
        [Range(0f, 1f)] public float auraAlphaMax = 0.28f;

        [Header("Humo y chispas")]
        [Tooltip("Segundos entre bocanadas de humo. Menor = mas humo.")]
        public float smokeInterval = 0.16f;
        public float bubbleInterval = 0.3f;
        public float sparkInterval = 0.28f;

        [Header("Orden de dibujo")]
        public string sortingLayer = "Tablero";
        // la olla esta en OrdenObstaculos (20): el halo va justo detras y la pocion justo delante
        public int auraOrder = Level01Art.OrdenObstaculos - 1;
        public int surfaceOrder = Level01Art.OrdenObstaculos + 1;

        SpriteRenderer m_Aura;
        SpriteRenderer m_Surface;
        SpriteRenderer m_Halo;

        float m_Time;
        float m_SmokeTimer;
        float m_BubbleTimer;
        float m_SparkTimer;
        float m_Boost;           // pico de intensidad tras entregar un ingrediente
        float m_Intensity = 1f;
        int m_LastDelivered;

        void Awake()
        {
            // halo grande detras de la olla
            m_Aura = MakeLayer("Aura", FxSprites.Glow, potionColor, auraOrder);
            m_Aura.transform.localPosition = new Vector3(0f, -0.25f, 0f);

            // la pocion que se ve dentro de la apertura (no tapa el borde negro)
            m_Surface = MakeLayer("Pocion", FxSprites.Disc, potionColor, surfaceOrder);
            m_Surface.transform.localScale = new Vector3(mouthWidth * 0.88f, mouthHeight * 0.75f, 1f);

            // vaho luminoso justo encima de la apertura
            m_Halo = MakeLayer("Vaho", FxSprites.Glow, smokeColor, surfaceOrder + 1);
            m_Halo.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            m_Time = Random.Range(0f, 10f);
        }

        SpriteRenderer MakeLayer(string layerName, Sprite sprite, Color color, int order)
        {
            var go = new GameObject(layerName);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            Material mat = PlaceholderArt.UnlitSpriteMaterial();
            if (mat != null) sr.sharedMaterial = mat;
            return sr;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            m_Time += dt;

            // --- reacciona al juego: entrega de ingredientes y victoria
            bool won = false;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (gm.DeliveredIngredients > m_LastDelivered) Burst();
                m_LastDelivered = gm.DeliveredIngredients;   // tambien baja al reiniciar
                won = gm.State == LevelState.Won;
            }

            m_Boost = Mathf.MoveTowards(m_Boost, 0f, dt * 1.2f);
            float target = 1f + m_Boost * 1.2f + (won ? 0.8f : 0f);
            m_Intensity = Mathf.Lerp(m_Intensity, target, 1f - Mathf.Exp(-4f * dt));

            // --- brillos que laten
            float pulse = 0.5f + 0.5f * Mathf.Sin(m_Time * 1.7f);
            float shimmer = 0.5f + 0.5f * Mathf.Sin(m_Time * 6.3f + 1.1f);

            float auraScale = 0.95f + 0.08f * pulse + 0.06f * (m_Intensity - 1f);
            m_Aura.transform.localScale = new Vector3(auraSize.x * auraScale, auraSize.y * auraScale, 1f);
            SetAlpha(m_Aura, Mathf.Clamp01(Mathf.Lerp(auraAlphaMin, auraAlphaMax, pulse) * m_Intensity));

            SetAlpha(m_Surface, Mathf.Clamp01(Mathf.Lerp(0.5f, 0.75f, shimmer) * Mathf.Min(m_Intensity, 1.4f)));

            float haloScale = 1f + 0.12f * pulse;
            m_Halo.transform.localScale = new Vector3(mouthWidth * 1.15f * haloScale, mouthHeight * 3.2f * haloScale, 1f);
            SetAlpha(m_Halo, Mathf.Clamp01(Mathf.Lerp(0.1f, 0.22f, pulse) * m_Intensity));

            // --- emision continua (mas rapida cuando la intensidad sube)
            m_SmokeTimer -= dt * m_Intensity;
            if (m_SmokeTimer <= 0f)
            {
                m_SmokeTimer = smokeInterval * Random.Range(0.7f, 1.3f);
                EmitSmoke(0.32f, 1f);
            }

            m_BubbleTimer -= dt * m_Intensity;
            if (m_BubbleTimer <= 0f)
            {
                m_BubbleTimer = bubbleInterval * Random.Range(0.6f, 1.6f);
                EmitBubble();
            }

            m_SparkTimer -= dt * m_Intensity;
            if (m_SparkTimer <= 0f)
            {
                m_SparkTimer = sparkInterval * Random.Range(0.6f, 1.5f);
                EmitSpark();
            }
        }

        static void SetAlpha(SpriteRenderer sr, float alpha)
        {
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, alpha);
        }

        // ---------------- emisores ------------------------------------------

        Vector2 MouthPoint(float spread)
        {
            return (Vector2)transform.position +
                   new Vector2(Random.Range(-mouthWidth, mouthWidth) * 0.5f * spread,
                               Random.Range(-0.02f, 0.05f));
        }

        void EmitSmoke(float alpha, float speedMultiplier)
        {
            Vector2 velocity = new Vector2(Random.Range(-0.08f, 0.08f), Random.Range(0.45f, 0.8f) * speedMultiplier);
            Color c = new Color(smokeColor.r, smokeColor.g, smokeColor.b, alpha);
            // sube con un poco de aceleracion hacia arriba, se ensancha y ondula
            FxParticle.Spawn(transform.parent, FxSprites.Glow, MouthPoint(0.7f), velocity,
                -0.15f, 0.2f, Random.Range(1.8f, 2.7f),
                Random.Range(0.3f, 0.45f), Random.Range(1f, 1.5f), c, 0f, 0.25f, "FX", 3, 0.12f);
        }

        void EmitBubble()
        {
            Vector2 velocity = new Vector2(Random.Range(-0.05f, 0.05f), Random.Range(0.2f, 0.45f));
            float size = Random.Range(0.06f, 0.12f);
            FxParticle.Spawn(transform.parent, FxSprites.Disc, MouthPoint(0.6f), velocity,
                0f, 0f, Random.Range(0.5f, 0.85f), size, size * 1.35f,
                new Color(0.75f, 1f, 0.8f, 0.9f), 0f, 0.5f, "FX", 4);
        }

        void EmitSpark()
        {
            Vector2 velocity = new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(0.7f, 1.4f));
            FxParticle.Spawn(transform.parent, FxSprites.Glow, MouthPoint(0.8f), velocity,
                0f, 0.3f, Random.Range(1f, 1.6f), Random.Range(0.08f, 0.14f), 0f,
                sparkColor, 0f, 0.3f, "FX", 5, 0.1f);
        }

        /// <summary>Explosion de humo y chispas cuando un ingrediente cae al caldero.</summary>
        void Burst()
        {
            m_Boost = 1f;

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = new Vector2(Random.Range(-1.1f, 1.1f), Random.Range(0.8f, 1.9f));
                Color c = new Color(smokeColor.r, smokeColor.g, smokeColor.b, 0.45f);
                FxParticle.Spawn(transform.parent, FxSprites.Glow, MouthPoint(0.5f), velocity,
                    0f, 0.9f, Random.Range(1.4f, 2.2f), Random.Range(0.35f, 0.5f), Random.Range(1.1f, 1.6f),
                    c, 0f, 0.2f, "FX", 3, 0.15f);
            }

            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = new Vector2(Random.Range(-1.4f, 1.4f), Random.Range(1.2f, 3f));
                FxParticle.Spawn(transform.parent, FxSprites.Glow, MouthPoint(0.5f), velocity,
                    2.5f, 0.2f, Random.Range(0.9f, 1.5f), Random.Range(0.08f, 0.14f), 0f,
                    sparkColor, 0f, 0.4f, "FX", 5);
            }

            // destello del golpe
            FxParticle.Spawn(transform.parent, FxSprites.Glow, transform.position, Vector2.zero,
                0f, 0f, 0.35f, 0.8f, 3f, new Color(0.8f, 1f, 0.7f, 0.8f), 0f, 0f, "FX", 6);
        }
    }
}
