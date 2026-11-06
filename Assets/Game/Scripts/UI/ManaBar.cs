using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Barra de mana de levitacion, con el arte de Melanie
    /// (UI/Marco_barra.png + UI/Barra_Mana.png).
    ///
    /// Es una barra VERTICAL del lado izquierdo de la pantalla. No es UI de Canvas:
    /// son sprites colocados en el mundo, igual que el resto del arte del nivel, y
    /// en la posicion exacta en la que Melanie los dibujo sobre el lienzo.
    ///
    /// El relleno conserva su escala original: una mascara rectangular recorta
    /// la imagen desde arriba al consumir mana y la descubre desde abajo al
    /// recuperarse, sin comprimir el dibujo de Melanie.
    ///
    /// Al agotarse el mana se prepara un tinte mas oscuro que se mantiene
    /// hasta superar el umbral de recuperacion. El recorte al 0 % es completo.
    /// </summary>
    public class ManaBar : MonoBehaviour
    {
        [Header("Piezas")]
        [Tooltip("Marco de la barra (el hueco oscuro).")]
        public SpriteRenderer marco;

        [Tooltip("Relleno de color.")]
        public SpriteRenderer relleno;

        [Tooltip("Pivote del relleno, colocado en la base interior del marco.")]
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

        [Tooltip("Valor heredado de escenas anteriores; el nuevo recorte llega a 0 % sin deformar la imagen.")]
        [Range(0f, 0.1f)]
        public float minimoVisible = 0.03f;

        float m_Mostrado = 1f;
        bool m_Agotada;
        SpriteMask m_Mascara;
        Sprite m_SpriteMascara;
        Bounds m_LimitesRelleno;

        public bool Agotada => m_Agotada;

        void Start()
        {
            PrepararMascara();
            m_Mostrado = ManaActual();
            Aplicar(true);
        }

        void PrepararMascara()
        {
            if (relleno == null || relleno.sprite == null) return;

            // Recupera la escala del sprite: las escenas antiguas mantienen el
            // pivote serializado, pero ya no debe deformar el dibujo.
            if (pivoteRelleno != null) pivoteRelleno.localScale = Vector3.one;
            m_LimitesRelleno = relleno.bounds;

            // El sprite blanco solo actua como recorte, nunca se renderiza.
            var textura = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            textura.name = "MascaraMana_2x2";
            textura.filterMode = FilterMode.Point;
            textura.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            textura.Apply();
            m_SpriteMascara = Sprite.Create(textura, new Rect(0f, 0f, 2f, 2f),
                new Vector2(0.5f, 0.5f), 1f);
            m_SpriteMascara.name = "MascaraMana_Rectangulo";

            var go = new GameObject("Mascara_Relleno_Mana");
            go.transform.SetParent(transform, false);
            m_Mascara = go.AddComponent<SpriteMask>();
            m_Mascara.sprite = m_SpriteMascara;
            m_Mascara.alphaCutoff = 0.1f;
            m_Mascara.isCustomRangeActive = true;
            m_Mascara.frontSortingLayerID = relleno.sortingLayerID;
            m_Mascara.backSortingLayerID = relleno.sortingLayerID;
            m_Mascara.frontSortingOrder = relleno.sortingOrder + 1;
            m_Mascara.backSortingOrder = relleno.sortingOrder - 1;
            relleno.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        void OnDestroy()
        {
            if (m_SpriteMascara != null)
            {
                Destroy(m_SpriteMascara.texture);
                Destroy(m_SpriteMascara);
            }
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

            if (m_Mascara != null)
            {
                // El marco y el relleno mantienen sus proporciones originales.
                float porcentaje = Mathf.Clamp01(m_Mostrado);
                float altura = m_LimitesRelleno.size.y * porcentaje;
                Transform mascara = m_Mascara.transform;
                mascara.position = new Vector3(m_LimitesRelleno.center.x,
                    m_LimitesRelleno.min.y + altura * 0.5f, relleno.transform.position.z);
                Vector3 escalaPadre = transform.lossyScale;
                mascara.localScale = new Vector3(
                    m_LimitesRelleno.size.x / (2f * Mathf.Max(Mathf.Abs(escalaPadre.x), 0.0001f)),
                    altura / (2f * Mathf.Max(Mathf.Abs(escalaPadre.y), 0.0001f)), 1f);
            }

            if (relleno != null)
            {
                Color objetivo = m_Agotada ? colorAgotado : colorNormal;
                relleno.color = instantaneo || suavizado <= 0f
                    ? objetivo
                    : Color.Lerp(relleno.color, objetivo, 1f - Mathf.Exp(-suavizado * Time.unscaledDeltaTime));
            }
        }
    }
}
