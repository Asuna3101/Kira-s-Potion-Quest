using UnityEngine;
using UnityEngine.UI;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Cartel de "Nivel superado": el libro de recetas de Kira se abre y aparece
    /// la frase escrita a mano.
    ///
    /// Son tres dibujos de Melanie (Sprites/UI): Libro (cerrado), Libro_abierto y
    /// Libro_letras. La animacion es por codigo, en tres tiempos:
    ///   1. el libro cerrado entra creciendo
    ///   2. se abre (cambia de sprite y se despliega a lo ancho)
    ///   3. las letras aparecen subiendo
    ///
    /// Las medidas por defecto salen de los propios PNG, para que el libro y las
    /// letras encajen sin colocarlos a mano.
    /// </summary>
    public class VictoryBook : MonoBehaviour
    {
        [Header("Dibujos (Assets/Game/Sprites/UI)")]
        public Sprite libroCerrado;
        public Sprite libroAbierto;
        public Sprite libroLetras;

        [Header("Tamano")]
        [Tooltip("Ancho del libro abierto en pixeles de canvas. Lo demas se calcula a partir de este.")]
        public float anchoLibro = 1400f;

        [Tooltip("Desplazamiento del conjunto dentro de la pantalla.")]
        public Vector2 posicion = new Vector2(0f, 20f);

        [Header("Tiempos (segundos)")]
        public float entrada = 0.25f;
        public float apertura = 0.30f;
        public float aparicionLetras = 0.40f;

        [Header("Pie")]
        [Tooltip("Texto pequeno bajo el libro. Vacio = no se muestra.")]
        public string pie = "click o ESPACIO para continuar";

        // Medidas tomadas de los PNG, en unidades de 240 px.
        const float UAnchoAbierto = 14.60f;
        const float UAltoAbierto = 8.18f;
        const float UAnchoCerrado = 7.56f;
        const float UAltoCerrado = 8.13f;
        const float UAnchoLetras = 12.98f;
        const float UAltoLetras = 5.43f;
        // Centro de las letras respecto al centro del libro abierto.
        const float ULetrasOffsetX = 0.004f;
        const float ULetrasOffsetY = 0.792f;

        RectTransform m_Raiz;
        RectTransform m_LibroRect;
        Image m_Libro;
        CanvasGroup m_LibroGrupo;
        RectTransform m_LetrasRect;
        CanvasGroup m_LetrasGrupo;
        Text m_Pie;
        CanvasGroup m_PieGrupo;

        float m_T = -1f;
        bool m_Construido;

        public bool TieneArte => libroAbierto != null || libroCerrado != null;

        /// <summary>Crea los objetos dentro del canvas indicado. Idempotente.</summary>
        public void Construir(Transform canvas, Font fuente)
        {
            if (m_Construido || canvas == null) return;
            m_Construido = true;

            float k = anchoLibro / UAnchoAbierto;   // pixeles de canvas por unidad

            var raiz = NuevoUI(canvas, "Libro_Victoria");
            m_Raiz = raiz.GetComponent<RectTransform>();
            Colocar(m_Raiz, posicion, new Vector2(anchoLibro, UAltoAbierto * k));

            // --- libro
            var libroGo = NuevoUI(m_Raiz, "Libro");
            m_Libro = libroGo.AddComponent<Image>();
            m_Libro.raycastTarget = false;
            m_Libro.sprite = libroCerrado != null ? libroCerrado : libroAbierto;
            m_LibroRect = m_Libro.rectTransform;
            m_LibroGrupo = libroGo.AddComponent<CanvasGroup>();
            Colocar(m_LibroRect, Vector2.zero, new Vector2(UAnchoCerrado * k, UAltoCerrado * k));

            // --- letras
            var letrasGo = NuevoUI(m_Raiz, "Letras");
            var letras = letrasGo.AddComponent<Image>();
            letras.raycastTarget = false;
            letras.sprite = libroLetras;
            m_LetrasRect = letras.rectTransform;
            m_LetrasGrupo = letrasGo.AddComponent<CanvasGroup>();
            Colocar(m_LetrasRect,
                new Vector2(ULetrasOffsetX * k, ULetrasOffsetY * k),
                new Vector2(UAnchoLetras * k, UAltoLetras * k));

            // --- pie
            if (!string.IsNullOrEmpty(pie))
            {
                var pieGo = NuevoUI(m_Raiz, "Pie");
                m_Pie = pieGo.AddComponent<Text>();
                m_Pie.font = fuente;
                m_Pie.fontSize = 28;
                m_Pie.alignment = TextAnchor.MiddleCenter;
                m_Pie.text = pie;
                m_Pie.color = new Color(0.96f, 0.91f, 0.82f, 1f);
                m_Pie.raycastTarget = false;
                m_PieGrupo = pieGo.AddComponent<CanvasGroup>();
                Colocar(m_Pie.rectTransform,
                    new Vector2(0f, -UAltoAbierto * k * 0.5f - 34f),
                    new Vector2(anchoLibro, 44f));
            }

            raiz.SetActive(false);
        }

        public void Mostrar()
        {
            if (m_Raiz == null) return;
            m_T = 0f;
            m_Raiz.gameObject.SetActive(true);
            Aplicar();
        }

        public void Ocultar()
        {
            m_T = -1f;
            if (m_Raiz != null) m_Raiz.gameObject.SetActive(false);
        }

        void Update()
        {
            if (m_T < 0f) return;
            m_T += Time.unscaledDeltaTime;
            Aplicar();
        }

        void Aplicar()
        {
            float t = m_T;

            // 1. el libro cerrado entra creciendo
            float e = entrada <= 0f ? 1f : Mathf.Clamp01(t / entrada);
            float pop = 1f - Mathf.Pow(1f - e, 3f);           // frena al final
            if (m_LibroGrupo != null) m_LibroGrupo.alpha = e;

            float k = anchoLibro / UAnchoAbierto;

            // 2. se abre
            float a = apertura <= 0f ? 1f : Mathf.Clamp01((t - entrada) / apertura);
            bool abierto = a > 0f && libroAbierto != null;
            if (m_Libro != null && abierto && m_Libro.sprite != libroAbierto)
                m_Libro.sprite = libroAbierto;

            if (m_LibroRect != null)
            {
                float ancho = abierto
                    ? Mathf.Lerp(UAnchoCerrado, UAnchoAbierto, Smooth(a)) * k
                    : UAnchoCerrado * k;
                float alto = (abierto ? Mathf.Lerp(UAltoCerrado, UAltoAbierto, Smooth(a)) : UAltoCerrado) * k;
                m_LibroRect.sizeDelta = new Vector2(ancho, alto);
                m_LibroRect.localScale = new Vector3(pop, pop, 1f);
            }

            // 3. las letras aparecen subiendo
            float l = aparicionLetras <= 0f ? 1f : Mathf.Clamp01((t - entrada - apertura) / aparicionLetras);
            if (m_LetrasGrupo != null) m_LetrasGrupo.alpha = l;
            if (m_LetrasRect != null)
            {
                m_LetrasRect.anchoredPosition = new Vector2(
                    ULetrasOffsetX * k,
                    ULetrasOffsetY * k - (1f - l) * 24f);
                m_LetrasRect.localScale = new Vector3(pop, pop, 1f);
            }

            if (m_PieGrupo != null)
                m_PieGrupo.alpha = Mathf.Clamp01((t - entrada - apertura - aparicionLetras) / 0.3f);
        }

        static float Smooth(float x) => x * x * (3f - 2f * x);

        static GameObject NuevoUI(Transform parent, string nombre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        static void Colocar(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }
    }
}
