using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Sprites suaves generados por codigo para los efectos (chispas, cascarones,
    /// brillo de las velas). No dependen de arte: cuando Melanie entregue sus
    /// propios efectos basta con pasarlos a FxParticle.Spawn en lugar de estos.
    /// Tamano en el mundo con escala 1: Glow y Disc miden 1 u; Shard mide 0.5 u.
    /// </summary>
    public static class FxSprites
    {
        static Sprite s_Glow, s_Disc, s_Shard, s_RoundedBox;

        /// <summary>Circulo con borde muy difuso (brillos, chispas, humo).</summary>
        public static Sprite Glow
        {
            get { if (s_Glow == null) s_Glow = MakeDot(false); return s_Glow; }
        }

        /// <summary>Circulo de borde casi nitido (gotas de yema, brasas).</summary>
        public static Sprite Disc
        {
            get { if (s_Disc == null) s_Disc = MakeDot(true); return s_Disc; }
        }

        /// <summary>Trozo de cascaron: media luna.</summary>
        public static Sprite Shard
        {
            get { if (s_Shard == null) s_Shard = MakeShard(); return s_Shard; }
        }

        /// <summary>
        /// Caja de esquinas redondeadas lista para 9-slice (Image Type = Sliced).
        /// Se usa en el HUD para el globo de dialogo y el panel de controles.
        /// </summary>
        public static Sprite RoundedBox
        {
            get { if (s_RoundedBox == null) s_RoundedBox = MakeRoundedBox(); return s_RoundedBox; }
        }

        static Texture2D NewTexture(int n)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            return tex;
        }

        static Sprite MakeDot(bool crisp)
        {
            const int n = 32;
            Texture2D tex = NewTexture(n);
            var px = new Color[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / (n * 0.5f);
                    float a = crisp
                        ? Mathf.Clamp01((1f - d) * 6f)
                        : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - d));
                    px[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        static Sprite MakeRoundedBox()
        {
            const int n = 64;
            const float radius = 24f;
            Texture2D tex = NewTexture(n);
            var px = new Color[n * n];
            float half = n * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    // distancia al rectangulo interior; dentro del radio = caja redondeada
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(radius - d + 0.5f));
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            // 100 px por unidad = 1:1 con los pixeles del canvas; borde 26 px para el 9-slice.
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(26f, 26f, 26f, 26f));
        }

        static Sprite MakeShard()
        {
            const int n = 32;
            Texture2D tex = NewTexture(n);
            var px = new Color[n * n];
            float c = (n - 1) * 0.5f;
            var centerA = new Vector2(c, c);
            var centerB = new Vector2(c + n * 0.22f, c + n * 0.12f);
            float radiusA = n * 0.48f;
            float radiusB = n * 0.40f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    var p = new Vector2(x, y);
                    float inA = Mathf.Clamp01(radiusA - Vector2.Distance(p, centerA));
                    float outB = Mathf.Clamp01(Vector2.Distance(p, centerB) - radiusB);
                    float a = Mathf.Min(inA, outB);
                    float shade = Mathf.Lerp(0.82f, 1f, y / (float)(n - 1));
                    px[y * n + x] = new Color(shade, shade, shade, a);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            // 64 px por unidad: el chip mide 0.5 u con escala 1.
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 64f);
        }
    }

    /// <summary>
    /// Particula minima hecha por codigo: se mueve, gira, cambia de tamano, se
    /// desvanece y se destruye sola. Sirve para chispas al levitar, cascarones al
    /// romperse el huevo y brasas de las velas, sin ParticleSystem ni materiales.
    /// </summary>
    public class FxParticle : MonoBehaviour
    {
        Vector2 m_Velocity;
        float m_Gravity;
        float m_Drag;
        float m_Life;
        float m_Age;
        float m_StartScale;
        float m_EndScale;
        float m_Spin;
        float m_FadeStart;
        float m_SwayAmplitude;
        float m_SwayPhase;
        Color m_Color;
        SpriteRenderer m_Sr;

        /// <param name="scale">Escala inicial/final del sprite (Glow y Disc: diametro en unidades).</param>
        /// <param name="gravity">Aceleracion hacia abajo en u/s^2 (negativa = sube).</param>
        /// <param name="drag">Frenado por segundo (0 = nada, 2 = frena rapido).</param>
        /// <param name="fadeStart">Fraccion de vida desde la que empieza a desvanecerse (0 = desde el inicio).</param>
        /// <param name="sway">Vaiven lateral en u/s (humo que ondula). 0 = sube derecho.</param>
        public static FxParticle Spawn(Transform parent, Sprite sprite, Vector2 position, Vector2 velocity,
            float gravity, float drag, float life, float startScale, float endScale, Color color,
            float spinDegPerSecond, float fadeStart, string sortingLayer = "FX", int sortingOrder = 5,
            float sway = 0f)
        {
            var go = new GameObject("Fx");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            go.transform.localScale = new Vector3(startScale, startScale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = sortingOrder;
            Material mat = PlaceholderArt.UnlitSpriteMaterial();
            if (mat != null) sr.sharedMaterial = mat;

            var p = go.AddComponent<FxParticle>();
            p.m_Sr = sr;
            p.m_Velocity = velocity;
            p.m_Gravity = gravity;
            p.m_Drag = drag;
            p.m_Life = Mathf.Max(0.05f, life);
            p.m_StartScale = startScale;
            p.m_EndScale = endScale;
            p.m_Spin = spinDegPerSecond;
            p.m_FadeStart = Mathf.Clamp01(fadeStart);
            p.m_SwayAmplitude = sway;
            p.m_SwayPhase = Random.Range(0f, Mathf.PI * 2f);
            p.m_Color = color;
            return p;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            m_Age += dt;
            if (m_Age >= m_Life)
            {
                Destroy(gameObject);
                return;
            }

            float k = m_Age / m_Life;

            m_Velocity.y -= m_Gravity * dt;
            if (m_Drag > 0f) m_Velocity *= Mathf.Max(0f, 1f - m_Drag * dt);
            float swayX = m_SwayAmplitude != 0f ? Mathf.Cos(m_Age * 2.5f + m_SwayPhase) * m_SwayAmplitude : 0f;
            transform.position += new Vector3((m_Velocity.x + swayX) * dt, m_Velocity.y * dt, 0f);
            if (m_Spin != 0f) transform.Rotate(0f, 0f, m_Spin * dt);

            float s = Mathf.Lerp(m_StartScale, m_EndScale, k);
            transform.localScale = new Vector3(s, s, 1f);

            float fade = m_FadeStart >= 1f ? 1f : 1f - Mathf.Clamp01((k - m_FadeStart) / (1f - m_FadeStart));
            m_Sr.color = new Color(m_Color.r, m_Color.g, m_Color.b, m_Color.a * fade);
        }
    }
}
