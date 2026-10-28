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
    /// Como crece: el relleno cuelga de un pivote colocado en la BASE interior del
    /// marco, y lo que se anima es la escala en Y de ese pivote. Asi la barra sube
    /// desde abajo en vez de crecer desde el centro.
    ///
    /// Al quedarse sin mana el relleno se tine de un color mas oscuro y se queda
    /// asi hasta que se recupera, para que se vea que el hechizo esta agotado.
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

        [Tooltip("Trocito de barra que se deja visible al estar a cero, para que se vea el color oscuro.")]
        [Range(0f, 0.1f)]
        public float minimoVisible = 0.03f;

        float m_Mostrado = 1f;
        bool m_Agotada;

        public bool Agotada => m_Agotada;

        void Start()
        {
            m_Mostrado = ManaActual();
            Aplicar(true);
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

            if (pivoteRelleno != null)
            {
                float alto = Mathf.Max(minimoVisible, m_Mostrado);
                Vector3 s = pivoteRelleno.localScale;
                pivoteRelleno.localScale = new Vector3(s.x, alto, s.z);
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
