using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Barra de mana de levitacion, con el arte de Melanie
    /// (UI/Marco_barra.png + UI/Barra_Mana.png).
    ///
    /// Es una barra VERTICAL del lado izquierdo. No es UI de Canvas: son sprites
    /// colocados en el mundo, en la posicion exacta en la que se dibujaron.
    ///
    /// COMO SE VACIA
    /// -------------
    /// El dibujo NO se escala ni se aplasta. Cada vez que cambia el mana se
    /// construye por codigo un sprite nuevo que es un RECORTE de la textura
    /// original: se toma solo la franja de abajo, de la altura que toque. El
    /// trozo que se ve conserva sus proporciones exactas, asi que la barra
    /// parece llenarse y vaciarse de verdad.
    ///
    /// El pivote del recorte es el borde inferior, asi que crece hacia arriba
    /// desde la base del marco sin recolocar nada.
    ///
    /// Antes esto se hacia con un SpriteMask. Se cambio porque la mascara
    /// depende del stencil y no funciona de forma fiable con el material unlit
    /// que usan los sprites del nivel.
    /// </summary>
    public class ManaBar : MonoBehaviour
    {
        [Header("Piezas")]
        [Tooltip("Marco de la barra (el hueco oscuro).")]
        public SpriteRenderer marco;

        [Tooltip("Relleno de color.")]
        public SpriteRenderer relleno;

        [Tooltip("Heredado de escenas anteriores. Ya no se escala; se deja en 1.")]
        public Transform pivoteRelleno;

        [Header("Colores")]
        [Tooltip("Tinte del relleno con mana disponible. Blanco = el sprite tal cual lo dibujo Melanie.")]
        public Color colorNormal = Color.white;

        [Tooltip("Tinte del relleno cuando el mana se agota.")]
        public Color colorAgotado = new Color(0.42f, 0.38f, 0.24f, 1f);

        [Tooltip("Por debajo de este valor (0 a 1) la barra se considera agotada.")]
        [Range(0f, 0.3f)]
        public float umbralAgotado = 0.02f;

        [Tooltip("Hasta donde tiene que recuperarse para volver al color normal. Evita el parpadeo justo en el limite.")]
        [Range(0f, 0.5f)]
        public float umbralRecuperado = 0.12f;

        [Header("Suavizado")]
        [Tooltip("Velocidad con la que la barra persigue el valor real. 0 = instantaneo.")]
        public float suavizado = 12f;

        [Tooltip("Pasos de recorte. 200 = la barra se mueve en doscientos escalones, imperceptible.")]
        [Range(32, 512)]
        public int pasos = 200;

        float m_Mostrado = 1f;
        bool m_Agotada;

        // Datos del sprite original, para recortarlo sin perderlo.
        Sprite m_Original;
        Texture2D m_Textura;
        Rect m_RectOriginal;
        float m_Ppu = 100f;
        Vector3 m_Base;          // borde inferior del relleno, en el mundo
        Sprite m_Recorte;        // sprite generado, se reemplaza al cambiar
        int m_PasoActual = -1;

        public bool Agotada => m_Agotada;

        void Start()
        {
            Preparar();
            m_Mostrado = ManaActual();
            Aplicar(true);
        }

        void Preparar()
        {
            if (relleno == null || relleno.sprite == null) return;

            // Las escenas viejas traen el pivote escalado por el sistema anterior.
            if (pivoteRelleno != null) pivoteRelleno.localScale = Vector3.one;

            // Sin mascara: el recorte lo hace el propio sprite.
            relleno.maskInteraction = SpriteMaskInteraction.None;
            var mascaraVieja = transform.Find("Mascara_Relleno_Mana");
            if (mascaraVieja != null) Destroy(mascaraVieja.gameObject);

            m_Original = relleno.sprite;
            m_Textura = m_Original.texture;
            m_RectOriginal = m_Original.rect;
            m_Ppu = m_Original.pixelsPerUnit;

            // La base: borde inferior del dibujo tal como esta colocado ahora.
            Bounds b = relleno.bounds;
            m_Base = new Vector3(b.center.x, b.min.y, relleno.transform.position.z);
            relleno.transform.position = m_Base;
        }

        void OnDestroy()
        {
            if (m_Recorte != null) Destroy(m_Recorte);
        }

        void Update()
        {
            float objetivo = ManaActual();

            // Histeresis: se agota al bajar del umbral y solo se "cura" al pasar el
            // umbral de recuperacion, mas alto. Si no, al regenerar parpadearia.
            if (!m_Agotada && objetivo <= umbralAgotado) m_Agotada = true;
            else if (m_Agotada && objetivo >= umbralRecuperado) m_Agotada = false;

            m_Mostrado = suavizado <= 0f
                ? objetivo
                : Mathf.Lerp(m_Mostrado, objetivo, 1f - Mathf.Exp(-suavizado * Time.unscaledDeltaTime));

            Aplicar(false);
        }

        static float ManaActual()
        {
            var gm = GameManager.Instance;
            return gm != null ? Mathf.Clamp01(gm.ManaNormalized) : 1f;
        }

        void Aplicar(bool instantaneo)
        {
            if (instantaneo) m_Mostrado = ManaActual();
            if (relleno == null) return;

            ActualizarRecorte(Mathf.Clamp01(m_Mostrado));

            Color objetivo = m_Agotada ? colorAgotado : colorNormal;
            relleno.color = instantaneo || suavizado <= 0f
                ? objetivo
                : Color.Lerp(relleno.color, objetivo, 1f - Mathf.Exp(-suavizado * Time.unscaledDeltaTime));
        }

        /// <summary>
        /// Rehace el sprite con solo la franja de abajo. Se cuantiza a 'pasos'
        /// para no construir un sprite nuevo en cada frame.
        /// </summary>
        void ActualizarRecorte(float porcentaje)
        {
            if (m_Textura == null) return;

            int paso = Mathf.Clamp(Mathf.RoundToInt(porcentaje * pasos), 0, pasos);
            if (paso == m_PasoActual) return;
            m_PasoActual = paso;

            if (paso <= 0)
            {
                relleno.enabled = false;
                return;
            }
            relleno.enabled = true;

            float fraccion = paso / (float)pasos;
            float altoPx = Mathf.Max(1f, Mathf.Round(m_RectOriginal.height * fraccion));

            // La franja de ABAJO del dibujo original.
            var rect = new Rect(m_RectOriginal.x, m_RectOriginal.y, m_RectOriginal.width, altoPx);

            Sprite anterior = m_Recorte;
            // Pivote en el borde inferior: al crecer, sube desde la base.
            m_Recorte = Sprite.Create(m_Textura, rect, new Vector2(0.5f, 0f), m_Ppu, 0, SpriteMeshType.FullRect);
            m_Recorte.name = "Barra_Mana_Recorte";
            relleno.sprite = m_Recorte;
            relleno.transform.position = m_Base;

            if (anterior != null) Destroy(anterior);
        }
    }
}
