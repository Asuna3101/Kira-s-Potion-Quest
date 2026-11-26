using UnityEngine;
using UnityEngine.InputSystem;
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

        [Header("Cartel completo")]
        [Tooltip("ON: el cartel de WIN es UNA sola imagen a pantalla completa (la de 'Libro Abierto'). " +
                 "No hay libro cerrado, ni letras aparte, ni texto debajo. OFF: la animacion vieja del libro.")]
        public bool soloImagenCompleta = true;

        [Tooltip("Segundos que tarda el cartel en aparecer (aparece con un fundido y un saltito).")]
        public float entradaCartel = 0.35f;

        [Header("Botones del cartel de WIN")]
        [Tooltip("Boton MENU (vuelve al selector de niveles). Es el sprite de 'Boton_menu'.")]
        public Sprite botonMenu;

        [Tooltip("Boton NEXT (pasa al siguiente nivel). Es el sprite de 'Botonnext'. " +
                 "No sale si el GameManager no tiene 'Escena Siguiente'.")]
        public Sprite botonNext;

        [Header("Posicion de los botones (pagina izquierda del libro)")]
        [Tooltip("Centro del boton MENU en el lienzo original del cartel (3840 x 2160): X desde la " +
                 "izquierda, Y desde ABAJO. Es el mismo para todos los niveles.")]
        public Vector2 posicionBotonMenu = new Vector2(806f, 604.5f);

        [Tooltip("Centro del boton NEXT en ese mismo lienzo.")]
        public Vector2 posicionBotonNext = new Vector2(1481f, 607.5f);

        [Tooltip("Ancho de cada boton en ese mismo lienzo (el alto sale de la proporcion del dibujo).")]
        public float anchoBotonesCartel = 496f;

        [Header("Cartel de derrota (Level_lose)")]
        [Tooltip("Imagen completa que sale al perder. Vacio = el cartel de texto de siempre.")]
        public Sprite cartelDerrota;

        [Tooltip("Pergamino CERRADO (Sprites/UI/Pergamino_cerrado). Sale primero y despues se abre en el cartel " +
                 "de derrota. Vacio = el cartel abierto sale directo, como antes.")]
        public Sprite pergaminoCerrado;

        [Header("Tamano de los carteles")]
        [Tooltip("Tamano del libro de VICTORIA respecto al alto de la pantalla (1 = ocupa todo el alto). " +
                 "Siempre queda centrado.")]
        [Range(0.2f, 1f)] public float escalaVictoria = 0.65f;

        [Tooltip("Tamano del pergamino de DERROTA respecto al alto de la pantalla (1 = ocupa todo el alto). " +
                 "Siempre queda centrado.")]
        [Range(0.2f, 1f)] public float escalaDerrota = 0.70f;

        [Header("Derrota: pergamino cerrado -> abierto (segundos)")]
        [Tooltip("El pergamino cerrado aparece.")]
        public float entradaPergamino = 0.25f;

        [Tooltip("Cuanto se queda CERRADO antes de abrirse.")]
        public float pausaPergamino = 0.55f;

        [Tooltip("Lo que tarda en abrirse.")]
        public float aperturaPergamino = 0.35f;

        [Tooltip("Alto del pergamino cerrado respecto al alto del pergamino abierto.")]
        [Range(0.2f, 1f)] public float altoPergaminoCerrado = 0.55f;

        bool ModoCartel => soloImagenCompleta && libroAbierto != null;

        [Header("Victoria: libro cerrado -> abierto (segundos)")]
        [Tooltip("El libro CERRADO (campo 'Libro Cerrado', sprite 'Libro') aparece primero. " +
                 "Despues se abre y deja ver el cartel de WIN. Sin 'Libro Cerrado' el cartel entra directo.")]
        public float entradaLibroWin = 0.25f;

        [Tooltip("Cuanto se queda cerrado antes de abrirse.")]
        public float pausaLibroWin = 0.25f;

        [Tooltip("Lo que tarda en abrirse.")]
        public float aperturaLibroWin = 0.4f;

        [Tooltip("Alto del libro cerrado respecto al alto del cartel de WIN.")]
        [Range(0.3f, 1.2f)] public float altoLibroCerradoWin = 0.85f;

        bool LibroCerradoWin => ModoCartel && libroCerrado != null;

        [Header("Tamano")]
        [Tooltip("Ancho del libro abierto en pixeles de canvas. Lo demas se calcula a partir de este.")]
        public float anchoLibro = 1400f;

        [Tooltip("Desplazamiento del conjunto dentro de la pantalla.")]
        public Vector2 posicion = new Vector2(0f, 20f);

        [Header("Tiempos (segundos)")]
        [Tooltip("El libro cerrado entra creciendo.")]
        public float entrada = 0.30f;

        [Tooltip("Cuanto se queda CERRADO antes de abrirse, para que de tiempo a ver la portada.")]
        public float pausaCerrado = 0.45f;

        [Tooltip("Lo que tarda en abrirse.")]
        public float apertura = 0.40f;

        [Tooltip("Lo que tardan en aparecer las letras.")]
        public float aparicionLetras = 0.40f;

        /// <summary>Lo que dura la animacion entera, para que el nivel no avance antes.</summary>
        public float DuracionTotal => ModoCartel ? DuracionCartel : entrada + pausaCerrado + apertura + aparicionLetras;

        float DuracionCartel => LibroCerradoWin ? entradaLibroWin + pausaLibroWin + aperturaLibroWin : entradaCartel;

        [Header("Pie")]
        [Tooltip("Texto pequeno bajo el libro. Vacio = no se muestra.")]
        public string pie = "haz click para continuar";

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

        public bool TieneArte => libroAbierto != null || libroCerrado != null || cartelDerrota != null;

        public bool TieneCartelDerrota => cartelDerrota != null;

        RectTransform m_BtnMenu, m_BtnNext;
        GameManager m_Juego;
        HudController m_Hud;
        bool m_ClickSuelto;

        RectTransform m_WinCerrado;
        CanvasGroup m_WinCerradoGrupo;

        RectTransform m_Derrota;
        CanvasGroup m_DerrotaGrupo;
        RectTransform m_DerrotaCerrada;
        CanvasGroup m_DerrotaCerradaGrupo;
        float m_TD = -1f;

        /// <summary>Crea los objetos dentro del canvas indicado. Idempotente.</summary>
        public void Construir(Transform canvas, Font fuente)
        {
            if (m_Construido || canvas == null) return;
            m_Construido = true;

            float altoCanvas = AltoCanvas(canvas);

            // Cartel de derrota: el pergamino abierto, centrado y a escalaDerrota del alto
            // de la pantalla. Oculto hasta perder.
            if (cartelDerrota != null)
            {
                float altoDerrota = altoCanvas * escalaDerrota;

                var d = NuevoUI(canvas, "Cartel_Derrota");
                m_Derrota = d.GetComponent<RectTransform>();
                AjustarCentrado(m_Derrota, cartelDerrota, altoDerrota);
                var dimg = d.AddComponent<Image>();
                dimg.sprite = cartelDerrota;
                dimg.preserveAspect = true;
                dimg.raycastTarget = false;
                m_DerrotaGrupo = d.AddComponent<CanvasGroup>();
                m_DerrotaGrupo.interactable = false;
                m_DerrotaGrupo.blocksRaycasts = false;
                d.SetActive(false);

                // El pergamino CERRADO va encima del abierto: sale primero y luego se abre.
                if (pergaminoCerrado != null)
                {
                    var c = NuevoUI(canvas, "Pergamino_Cerrado");
                    m_DerrotaCerrada = c.GetComponent<RectTransform>();
                    AjustarCentrado(m_DerrotaCerrada, pergaminoCerrado, altoDerrota * altoPergaminoCerrado);
                    var cimg = c.AddComponent<Image>();
                    cimg.sprite = pergaminoCerrado;
                    cimg.preserveAspect = true;
                    cimg.raycastTarget = false;
                    m_DerrotaCerradaGrupo = c.AddComponent<CanvasGroup>();
                    m_DerrotaCerradaGrupo.interactable = false;
                    m_DerrotaCerradaGrupo.blocksRaycasts = false;
                    c.SetActive(false);
                }
            }

            if (libroAbierto == null && libroCerrado == null) return;

            if (ModoCartel)
            {
                // Una sola imagen, centrada en la pantalla y a escalaVictoria de su alto
                // (conserva su proporcion).
                var cartelRaiz = NuevoUI(canvas, "Cartel_Victoria");
                m_Raiz = cartelRaiz.GetComponent<RectTransform>();
                AjustarCentrado(m_Raiz, libroAbierto, altoCanvas * escalaVictoria);

                var img = cartelRaiz.AddComponent<Image>();
                img.sprite = libroAbierto;
                img.preserveAspect = true;
                img.raycastTarget = false;
                m_LibroGrupo = cartelRaiz.AddComponent<CanvasGroup>();
                m_LibroGrupo.interactable = false;
                m_LibroGrupo.blocksRaycasts = false;

                // Botones: estan dibujados en el mismo lienzo (3840 x 2160) que el
                // cartel, asi que se colocan por el recuadro de su sprite.
                // Los botones salen SIEMPRE de Assets/Resources/BotonesCartel: asi tienen el mismo
                // diseno en todos los niveles, sin importar lo que tenga asignado cada escena.
                var spMenu = Resources.Load<Sprite>("BotonesCartel/Boton_menu");
                var spNext = Resources.Load<Sprite>("BotonesCartel/Boton_next");
                if (spMenu != null) botonMenu = spMenu;
                if (spNext != null) botonNext = spNext;

                m_BtnMenu = CrearBoton(m_Raiz, "Boton_Menu", botonMenu);
                m_BtnNext = CrearBoton(m_Raiz, "Boton_Next", botonNext);

                // El libro CERRADO va encima del cartel: sale primero y luego se abre.
                if (libroCerrado != null)
                {
                    var wc = NuevoUI(canvas, "Libro_Cerrado_Victoria");
                    m_WinCerrado = wc.GetComponent<RectTransform>();
                    AjustarCentrado(m_WinCerrado, libroCerrado, altoCanvas * escalaVictoria * altoLibroCerradoWin);
                    var wimg = wc.AddComponent<Image>();
                    wimg.sprite = libroCerrado;
                    wimg.preserveAspect = true;
                    wimg.raycastTarget = false;
                    m_WinCerradoGrupo = wc.AddComponent<CanvasGroup>();
                    m_WinCerradoGrupo.interactable = false;
                    m_WinCerradoGrupo.blocksRaycasts = false;
                    wc.SetActive(false);
                }

                cartelRaiz.SetActive(false);
                return;
            }

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

        static RectTransform CrearBoton(RectTransform padre, string nombre, Sprite sprite)
        {
            if (sprite == null) return null;
            var go = NuevoUI(padre, nombre);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            return rt;
        }

        // Pone cada boton donde esta dibujado respecto al cartel (mismo lienzo de origen).
        void ColocarBotones()
        {
            if (!ModoCartel || m_Raiz == null) return;
            Rect cr = libroAbierto.rect;
            Rect rr = m_Raiz.rect;
            if (cr.width <= 0f || cr.height <= 0f) return;
            float s = Mathf.Min(rr.width / cr.width, rr.height / cr.height);

            // Las posiciones estan medidas en el PNG original (3840 de ancho), pero Unity
            // puede importarlo mas chico (Max Size 2048): el sprite.rect viene en esos pixeles
            // reducidos. 'f' pasa de pixeles del PNG original a pixeles del sprite importado.
            float f = libroAbierto.texture != null ? libroAbierto.texture.width / 3840f : 1f;
            Poner(m_BtnMenu, botonMenu, cr, s, f, posicionBotonMenu);
            Poner(m_BtnNext, botonNext, cr, s, f, posicionBotonNext);
        }

        // La posicion y el tamano NO dependen del recorte del sprite del boton: salen de
        // los valores fijos de arriba, asi los botones quedan iguales en todos los niveles.
        void Poner(RectTransform rt, Sprite sp, Rect cr, float s, float f, Vector2 centro)
        {
            if (rt == null || sp == null || sp.rect.width <= 0f) return;
            float alto = anchoBotonesCartel * sp.rect.height / sp.rect.width;
            rt.anchoredPosition = (centro * f - cr.center) * s;
            rt.sizeDelta = new Vector2(anchoBotonesCartel, alto) * (f * s);
        }

        bool HaySiguiente => m_Juego != null && !string.IsNullOrEmpty(m_Juego.SiguienteNivel);

        void ManejarBotones()
        {
            if (!ModoCartel || m_T < DuracionCartel) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            bool nextActivo = m_BtnNext != null && HaySiguiente;
            if (m_BtnNext != null && m_BtnNext.gameObject.activeSelf != nextActivo)
                m_BtnNext.gameObject.SetActive(nextActivo);

            // El click con el que se gano (lanzar, etc.) no cuenta: hay que soltar primero.
            if (!mouse.leftButton.isPressed) m_ClickSuelto = true;

            Vector2 p = mouse.position.ReadValue();
            bool sobreMenu = m_BtnMenu != null && RectTransformUtility.RectangleContainsScreenPoint(m_BtnMenu, p, null);
            bool sobreNext = nextActivo && RectTransformUtility.RectangleContainsScreenPoint(m_BtnNext, p, null);

            Animar(m_BtnMenu, sobreMenu);
            Animar(m_BtnNext, sobreNext);

            if (!m_ClickSuelto || !mouse.leftButton.wasPressedThisFrame) return;

            if (sobreNext && m_Juego != null) { m_Juego.IrAlSiguiente(); }
            else if (sobreMenu && m_Hud != null) { m_Hud.VolverAlMenu(); }
        }

        static void Animar(RectTransform rt, bool encima)
        {
            if (rt == null) return;
            float objetivo = encima ? 1.07f : 1f;
            float s = Mathf.Lerp(rt.localScale.x, objetivo, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
            rt.localScale = new Vector3(s, s, 1f);
        }

        public void Mostrar()
        {
            if (m_Raiz == null) return;
            if (m_Juego == null) m_Juego = GetComponent<GameManager>();
            if (m_Juego == null) m_Juego = Object.FindFirstObjectByType<GameManager>();
            if (m_Hud == null) m_Hud = GetComponent<HudController>();
            m_ClickSuelto = false;
            if (m_BtnMenu != null) m_BtnMenu.localScale = Vector3.one;
            if (m_BtnNext != null) m_BtnNext.localScale = Vector3.one;
            m_T = 0f;
            m_Raiz.gameObject.SetActive(true);
            if (m_WinCerrado != null) m_WinCerrado.gameObject.SetActive(true);
            Aplicar();
        }

        public void Ocultar()
        {
            m_T = -1f;
            if (m_Raiz != null) m_Raiz.gameObject.SetActive(false);
            if (m_WinCerrado != null) m_WinCerrado.gameObject.SetActive(false);
            OcultarDerrota();
        }

        public void MostrarDerrota()
        {
            if (m_Derrota == null) return;
            m_TD = 0f;
            m_Derrota.gameObject.SetActive(true);
            if (m_DerrotaCerrada != null) m_DerrotaCerrada.gameObject.SetActive(true);
            AplicarDerrota();
        }

        public void OcultarDerrota()
        {
            m_TD = -1f;
            if (m_Derrota != null) m_Derrota.gameObject.SetActive(false);
            if (m_DerrotaCerrada != null) m_DerrotaCerrada.gameObject.SetActive(false);
        }

        void AplicarDerrota()
        {
            if (m_DerrotaCerrada != null)
            {
                AplicarDerrotaConPergamino();
                return;
            }

            // Sin pergamino cerrado: el cartel abierto entra directo (fundido + saltito).
            float c = entradaCartel <= 0f ? 1f : Mathf.Clamp01(m_TD / entradaCartel);
            if (m_DerrotaGrupo != null) m_DerrotaGrupo.alpha = c;
            float u = c - 1f;
            float esc = Mathf.LerpUnclamped(0.9f, 1f, 1f + 2.70158f * u * u * u + 1.70158f * u * u);
            if (m_Derrota != null) m_Derrota.localScale = new Vector3(esc, esc, 1f);
        }

        /// <summary>
        /// Derrota en dos tiempos: 1) el pergamino CERRADO aparece y se queda un momento,
        /// 2) se abre: el cerrado se desvanece mientras el abierto se despliega a lo ancho.
        /// </summary>
        void AplicarDerrotaConPergamino()
        {
            float t = m_TD;

            // 1. el cerrado entra con un saltito
            float e = entradaPergamino <= 0f ? 1f : Mathf.Clamp01(t / entradaPergamino);
            float escCerrado = Mathf.LerpUnclamped(0.6f, 1f, EaseOutBack(e));

            // 2. se abre, despues de la pausa
            float inicioApertura = entradaPergamino + pausaPergamino;
            float a = aperturaPergamino <= 0f
                ? (t >= inicioApertura ? 1f : 0f)
                : Mathf.Clamp01((t - inicioApertura) / aperturaPergamino);
            float sa = Smooth(a);

            if (m_DerrotaCerradaGrupo != null) m_DerrotaCerradaGrupo.alpha = e * (1f - sa);
            if (m_DerrotaCerrada != null)
                m_DerrotaCerrada.localScale = new Vector3(escCerrado * (1f - 0.35f * sa), escCerrado, 1f);

            if (m_DerrotaGrupo != null) m_DerrotaGrupo.alpha = sa;
            if (m_Derrota != null)
                m_Derrota.localScale = new Vector3(Mathf.Lerp(0.35f, 1f, sa), Mathf.Lerp(0.92f, 1f, sa), 1f);
        }

        void Update()
        {
            if (m_TD >= 0f)
            {
                m_TD += Time.unscaledDeltaTime;
                AplicarDerrota();
            }
            if (m_T < 0f) return;
            m_T += Time.unscaledDeltaTime;
            Aplicar();
            ColocarBotones();
            ManejarBotones();
        }

        void Aplicar()
        {
            float t = m_T;

            if (ModoCartel && m_WinCerrado != null)
            {
                // 1) el libro cerrado aparece y se queda un momento, 2) se abre y deja ver el cartel.
                float e0 = entradaLibroWin <= 0f ? 1f : Mathf.Clamp01(t / entradaLibroWin);
                float u0 = e0 - 1f;
                float escC = Mathf.LerpUnclamped(0.6f, 1f, 1f + 2.70158f * u0 * u0 * u0 + 1.70158f * u0 * u0);

                float inicio = entradaLibroWin + pausaLibroWin;
                float a0 = aperturaLibroWin <= 0f ? (t >= inicio ? 1f : 0f) : Mathf.Clamp01((t - inicio) / aperturaLibroWin);
                float sa = a0 * a0 * (3f - 2f * a0);

                if (m_WinCerradoGrupo != null) m_WinCerradoGrupo.alpha = e0 * (1f - sa);
                m_WinCerrado.localScale = new Vector3(escC * (1f - 0.35f * sa), escC, 1f);

                if (m_LibroGrupo != null) m_LibroGrupo.alpha = sa;
                if (m_Raiz != null)
                    m_Raiz.localScale = new Vector3(Mathf.Lerp(0.35f, 1f, sa), Mathf.Lerp(0.92f, 1f, sa), 1f);
                return;
            }

            if (ModoCartel)
            {
                float c = entradaCartel <= 0f ? 1f : Mathf.Clamp01(t / entradaCartel);
                if (m_LibroGrupo != null) m_LibroGrupo.alpha = c;
                float u = c - 1f;
                float esc = Mathf.LerpUnclamped(0.9f, 1f, 1f + 2.70158f * u * u * u + 1.70158f * u * u);
                if (m_Raiz != null) m_Raiz.localScale = new Vector3(esc, esc, 1f);
                return;
            }

            // 1. el libro cerrado entra creciendo
            float e = entrada <= 0f ? 1f : Mathf.Clamp01(t / entrada);
            float pop = 1f - Mathf.Pow(1f - e, 3f);           // frena al final
            if (m_LibroGrupo != null) m_LibroGrupo.alpha = e;

            float k = anchoLibro / UAnchoAbierto;

            // 2. se abre
            float a = apertura <= 0f ? 1f : Mathf.Clamp01((t - entrada - pausaCerrado) / apertura);
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
            float l = aparicionLetras <= 0f ? 1f : Mathf.Clamp01((t - entrada - pausaCerrado - apertura) / aparicionLetras);
            if (m_LetrasGrupo != null) m_LetrasGrupo.alpha = l;
            if (m_LetrasRect != null)
            {
                m_LetrasRect.anchoredPosition = new Vector2(
                    ULetrasOffsetX * k,
                    ULetrasOffsetY * k - (1f - l) * 24f);
                m_LetrasRect.localScale = new Vector3(pop, pop, 1f);
            }

            if (m_PieGrupo != null)
                m_PieGrupo.alpha = Mathf.Clamp01((t - DuracionTotal) / 0.3f);
        }

        static float Smooth(float x) => x * x * (3f - 2f * x);

        /// <summary>Curva con rebote al final (el "saltito" de los carteles).</summary>
        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        /// <summary>Alto del canvas en pixeles (el HUD usa MatchWidthOrHeight = 1: manda el alto).</summary>
        static float AltoCanvas(Transform canvas)
        {
            var scaler = canvas != null ? canvas.GetComponent<CanvasScaler>() : null;
            return scaler != null && scaler.referenceResolution.y > 0f
                ? scaler.referenceResolution.y
                : GridSettings.ReferenceHeightPx;
        }

        /// <summary>
        /// Coloca el dibujo en el CENTRO de la pantalla con el alto indicado y la proporcion de
        /// su sprite. El pivote es el centro, asi que tambien escala desde el centro.
        /// </summary>
        static void AjustarCentrado(RectTransform rt, Sprite sprite, float alto)
        {
            float aspecto = sprite != null && sprite.rect.height > 0f
                ? sprite.rect.width / sprite.rect.height
                : 16f / 9f;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(alto * aspecto, alto);
            rt.anchoredPosition = Vector2.zero;
        }

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
