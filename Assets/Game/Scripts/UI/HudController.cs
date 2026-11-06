using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// HUD placeholder construido por codigo con uGUI (el proyecto no tiene TextMeshPro
    /// importado). Canvas en Screen Space - Overlay a proposito: con la Pixel Perfect
    /// Camera en modo Upscale Render Texture el texto se ve borroso si el canvas
    /// renderiza a traves de la camara.
    ///
    /// Muestra: nombre del nivel, ingredientes entregados, barra de mana, controles
    /// y el panel de victoria / derrota.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [Header("Apariencia")]
        public int referenceWidth = GridSettings.ReferenceWidthPx;
        public int referenceHeight = GridSettings.ReferenceHeightPx;

        [Header("Texto de arriba")]
        [Tooltip("Nombre del nivel y contador de ingredientes en la franja superior. " +
                 "Desactivado: la franja queda limpia y solo se ven los botones.")]
        public bool mostrarTitulo = false;

        [Header("Volver al menú")]
        [Tooltip("Arte del boton (Sprites/UI/Boton_menu.PNG). Vacio = rectangulo de color con el texto MENU.")]
        public Sprite spriteBotonMenu;

        [Tooltip("Escena que carga el boton MENU y la tecla Escape.")]
        public string escenaDelMenu = "MainMenu";

        [Header("Ayuda (boton ?)")]
        [Tooltip("Arte del boton (Sprites/UI/Boton_ayuda.PNG). Vacio = el circulo dorado dibujado por codigo.")]
        public Sprite spriteBotonAyuda;

        [Tooltip("(Ya no se usa: el boton ? abre ahora la guia de como jugar.)")]
        public bool controlsStartOpen = false;

        [Header("Guia de como jugar (popup)")]
        [Tooltip("Mostrar la guia al empezar el nivel. El boton ? la vuelve a abrir cuando quieras.")]
        public bool mostrarGuiaAlInicio = true;

        [Tooltip("Nombre de la escena donde se muestra sola al iniciar. Vacio = en todos los niveles.")]
        public string guiaSoloEnEscena = "Level01";

        [Tooltip("Mostrarla solo la primera vez en cada partida (al reiniciar con R no vuelve a salir).")]
        public bool guiaSoloUnaVez = true;

        [Tooltip("El juego se detiene mientras la guia esta abierta.")]
        public bool pausarMientrasSeLee = true;

        [Header("Kira habla")]
        [Tooltip("Globo de dialogo de Kira al bajar el mana. Desactivado: la barra de mana ya cuenta eso.")]
        public bool mostrarDialogosDeKira = false;

        public string halfManaMessage = "Oh no, ¡solo te queda la mitad de maná! T.T";
        public string emptyManaMessage = "Te quedaste sin maná, espera un poco :c";

        [Tooltip("Segundos que se queda el globo de dialogo en pantalla.")]
        public float dialogueSeconds = 3.4f;

        Text m_TitleText;
        Text m_HintText;
        Text m_ControlsText;
        Text m_ResultText;
        Image m_ManaFill;
        Image m_ResultPanel;

        // boton ? y panel de controles
        Canvas m_Canvas;
        VictoryBook m_Libro;
        RectTransform m_MenuRect;
        Text m_MenuLabel;
        RectTransform m_HelpRect;
        Image m_HelpGlow;
        RectTransform m_HelpRing;
        float m_HelpClickTime = -100f;
        float m_BrilloAyudaMax = 1f;
        GameObject m_ControlsPanel;
        bool m_ControlsOpen;

        // globo de dialogo de Kira
        RectTransform m_BubbleRect;
        CanvasGroup m_BubbleGroup;
        Text m_BubbleText;
        bool m_BubbleActive;
        float m_BubbleAge;
        bool m_HalfArmed = true;     // vuelve a avisar al bajar de la mitad, tras recuperar >= 60%
        bool m_EmptyArmed = true;    // vuelve a avisar al quedarse sin mana, tras recuperar >= 25%

        // popup de la guia
        RectTransform m_GuideBox;
        RectTransform m_GuideButtonRect;
        Text m_GuideButtonLabel;
        GameObject m_GuideRoot;
        bool m_GuideOpen;
        bool m_GuidePaused;
        float m_GuideOpenTime;
        int m_GuideClosedFrame = -1;
        static bool s_GuideSeen;

        // menu de pausa (ESC)
        GameObject m_PauseRoot;
        RectTransform m_PauseBox;
        RectTransform[] m_PauseButtons;
        Text[] m_PauseLabels;
        bool m_PauseOpen;
        bool m_PausePaused;
        float m_PauseOpenTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_GuideSeen = false; }

        GameManager m_Game;
        float m_HintTimer;

        void Start()
        {
            BuildUi();
            m_Game = GameManager.Instance;
            if (m_Game != null)
            {
                m_Game.StateChanged += OnStateChanged;
                m_Game.HintChanged += OnHint;
                m_Game.IngredientsChanged += RefreshTitle;
            }
            RefreshTitle();
            ShowResult(false, "");

            bool escenaValida = string.IsNullOrEmpty(guiaSoloEnEscena) ||
                                SceneManager.GetActiveScene().name == guiaSoloEnEscena;
            if (mostrarGuiaAlInicio && escenaValida && !(guiaSoloUnaVez && s_GuideSeen))
                OpenGuide();
        }

        void OnDestroy()
        {
            // No dejar el juego en pausa si se recarga la escena con la guia abierta.
            if (m_GuidePaused || m_PausePaused) Time.timeScale = 1f;
            if (m_Game == null) return;
            m_Game.StateChanged -= OnStateChanged;
            m_Game.HintChanged -= OnHint;
            m_Game.IngredientsChanged -= RefreshTitle;
        }

        void Update()
        {
            if (m_Game == null) m_Game = GameManager.Instance;
            if (m_Game != null && m_ManaFill != null)
            {
                // La barra es vertical: se llena de abajo hacia arriba.
                float mana = m_Game.ManaNormalized;
                m_ManaFill.fillAmount = mana;
                // Con poco mana vira al rojo para avisar que se acaba.
                m_ManaFill.color = Color.Lerp(PlaceholderArt.PortalACol, PlaceholderArt.Danger,
                    Mathf.InverseLerp(0.3f, 0.1f, mana));
            }

            if (m_HintTimer > 0f)
            {
                m_HintTimer -= Time.deltaTime;
                if (m_HintTimer <= 0f && m_HintText != null) m_HintText.text = "";
            }

            HandleGuide();
            HandlePause();
            HandleHelpButton();
            HandleMenuButton();
            UpdateKiraDialogue();
            TickBubble();
        }

        // ---------------- boton ? (controles desplegables) -------------------

        /// <summary>
        /// Boton MENU (arriba a la derecha) y atajo con Escape. Vuelve a la pantalla
        /// de titulo desde cualquier nivel.
        /// </summary>
        void HandleMenuButton()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (m_GuideOpen) { CloseGuide(); return; }
                if (m_PauseOpen) { ClosePause(); return; }
                // Jugando: ESC abre la pausa. Si el nivel ya termino, vuelve al menu como antes.
                if (m_Game != null && m_Game.State == LevelState.Playing) { OpenPause(); return; }
                VolverAlMenu();
                return;
            }
            if (m_GuideOpen || m_PauseOpen) return;

            if (m_MenuRect == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            bool encima = RectTransformUtility.RectangleContainsScreenPoint(
                m_MenuRect, mouse.position.ReadValue(), CamaraDelCanvas);

            if (m_MenuLabel != null)
            {
                m_MenuLabel.color = encima
                    ? new Color(1f, 1f, 0.85f, 1f)
                    : new Color(1f, 0.93f, 0.72f, 1f);
            }
            else if (m_MenuRect != null)
            {
                // Con el arte de Melanie, el hover crece un poco y aclara.
                var img = m_MenuRect.GetComponent<Image>();
                if (img != null) img.color = encima ? Color.white : new Color(0.88f, 0.88f, 0.88f, 1f);
                m_MenuRect.localScale = Vector3.Lerp(m_MenuRect.localScale,
                    Vector3.one * (encima ? 1.06f : 1f),
                    1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            }

            if (encima && mouse.leftButton.wasPressedThisFrame)
            {
                AudioDirector.Reproducir(Sonido.Boton);
                VolverAlMenu();
            }
        }

        public void VolverAlMenu()
        {
            if (!Application.CanStreamedLevelBeLoaded(escenaDelMenu))
            {
                Debug.LogError("[HUD] La escena '" + escenaDelMenu + "' no esta en Build Settings. " +
                               "Menu Chocoflan > 1 - Aplicar ajustes de proyecto.", this);
                return;
            }

            // El nivel pudo dejar el tiempo parado al ganar o perder.
            Time.timeScale = 1f;
            SceneManager.LoadScene(escenaDelMenu);
        }

        /// <summary>Null en Overlay, la camara del canvas en Screen Space - Camera.</summary>
        Camera CamaraDelCanvas =>
            m_Canvas == null || m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : m_Canvas.worldCamera;

        void HandleHelpButton()
        {
            if (m_HelpRect == null) return;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame &&
                m_GuideClosedFrame != Time.frameCount && !m_PauseOpen &&
                RectTransformUtility.RectangleContainsScreenPoint(m_HelpRect, mouse.position.ReadValue(), null))
            {
                m_HelpClickTime = Time.unscaledTime;
                if (m_GuideOpen) CloseGuide(); else OpenGuide();
            }

            bool encima = mouse != null && RectTransformUtility.RectangleContainsScreenPoint(
                m_HelpRect, mouse.position.ReadValue(), CamaraDelCanvas);
            float salto = Mathf.Clamp01(1f - (Time.unscaledTime - m_HelpClickTime) / 0.35f);
            float latido = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3.6f);
            if (m_HelpRing != null)
            {
                float escala = 1f + (encima ? 0.10f : 0.025f * latido) + salto * 0.15f;
                m_HelpRing.localScale = new Vector3(escala, escala, 1f);
            }

            // El brillo late para que el boton se note (con tiempo real: sigue latiendo en pausa).
            if (m_HelpGlow != null)
            {
                float alpha = m_GuideOpen ? 0.45f : 0.50f + 0.35f * latido;
                if (encima) alpha = 0.95f;
                // Con el arte de Melanie el boton ya trae su propio resplandor:
                // el brillo de codigo se queda muy suave para no duplicarlo.
                alpha = (alpha + salto * 0.15f) * m_BrilloAyudaMax;
                m_HelpGlow.color = new Color(1f, 0.82f, 0.28f, Mathf.Clamp01(alpha));
            }
        }

        // ---------------- menu de pausa (ESC) ---------------------------------

        const int PauseContinue = 0, PauseGuide = 1, PauseRestart = 2, PauseMenu = 3;

        void OpenPause()
        {
            if (m_PauseRoot == null || m_PauseOpen) return;
            m_PauseOpen = true;
            m_PauseOpenTime = Time.unscaledTime;
            m_PauseRoot.SetActive(true);
            if (m_PauseBox != null) m_PauseBox.localScale = new Vector3(0.85f, 0.85f, 1f);

            if (Time.timeScale > 0f)
            {
                Time.timeScale = 0f;
                m_PausePaused = true;
            }
        }

        void ClosePause()
        {
            if (!m_PauseOpen) return;
            m_PauseOpen = false;
            if (m_PauseRoot != null) m_PauseRoot.SetActive(false);
            if (m_PausePaused)
            {
                m_PausePaused = false;
                Time.timeScale = 1f;
            }
        }

        void HandlePause()
        {
            if (!m_PauseOpen) return;

            float t = Mathf.Clamp01((Time.unscaledTime - m_PauseOpenTime) / 0.2f);
            if (m_PauseBox != null)
            {
                float sc = Mathf.LerpUnclamped(0.85f, 1f, EaseOutBack(t));
                m_PauseBox.localScale = new Vector3(sc, sc, 1f);
            }

            var mouse = Mouse.current;
            if (mouse == null || m_PauseButtons == null) return;

            Vector2 pos = mouse.position.ReadValue();
            for (int i = 0; i < m_PauseButtons.Length; i++)
            {
                bool encima = RectTransformUtility.RectangleContainsScreenPoint(m_PauseButtons[i], pos, null);
                m_PauseLabels[i].color = encima ? Color.white : new Color(0.25f, 0.12f, 0.35f, 1f);

                if (encima && mouse.leftButton.wasPressedThisFrame &&
                    Time.unscaledTime - m_PauseOpenTime > 0.15f)
                {
                    PressPauseButton(i);
                    return;
                }
            }
        }

        void PressPauseButton(int index)
        {
            switch (index)
            {
                case PauseContinue:
                    ClosePause();
                    break;
                case PauseGuide:
                    ClosePause();
                    OpenGuide();
                    break;
                case PauseRestart:
                    ClosePause();
                    if (m_Game != null) m_Game.RestartLevel();
                    break;
                case PauseMenu:
                    ClosePause();
                    VolverAlMenu();
                    break;
            }
        }

        // ---------------- guia de como jugar (popup) --------------------------

        void OpenGuide()
        {
            if (m_GuideRoot == null || m_GuideOpen) return;
            if (m_Game != null && m_Game.State != LevelState.Playing) return;

            m_GuideOpen = true;
            s_GuideSeen = true;
            m_GuideOpenTime = Time.unscaledTime;
            m_GuideRoot.SetActive(true);
            if (m_GuideBox != null) m_GuideBox.localScale = new Vector3(0.85f, 0.85f, 1f);

            if (pausarMientrasSeLee && Time.timeScale > 0f)
            {
                Time.timeScale = 0f;
                m_GuidePaused = true;
            }
        }

        void CloseGuide()
        {
            if (!m_GuideOpen) return;
            m_GuideOpen = false;
            m_GuideClosedFrame = Time.frameCount;
            if (m_GuideRoot != null) m_GuideRoot.SetActive(false);

            if (m_GuidePaused)
            {
                m_GuidePaused = false;
                Time.timeScale = 1f;
            }
        }

        void HandleGuide()
        {
            if (!m_GuideOpen) return;

            // Animacion de entrada con tiempo real (el juego puede estar en pausa).
            float t = Mathf.Clamp01((Time.unscaledTime - m_GuideOpenTime) / 0.2f);
            if (m_GuideBox != null)
            {
                float s = Mathf.LerpUnclamped(0.85f, 1f, EaseOutBack(t));
                m_GuideBox.localScale = new Vector3(s, s, 1f);
            }

            // Evita cerrarla con el mismo click o tecla que la abrio.
            if (Time.unscaledTime - m_GuideOpenTime < 0.25f) return;

            var kb = Keyboard.current;
            var mouse = Mouse.current;

            bool close = kb != null &&
                         (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame ||
                          kb.spaceKey.wasPressedThisFrame);

            if (mouse != null)
            {
                bool encima = m_GuideButtonRect != null && RectTransformUtility.RectangleContainsScreenPoint(
                    m_GuideButtonRect, mouse.position.ReadValue(), null);
                if (m_GuideButtonLabel != null)
                    m_GuideButtonLabel.color = encima ? Color.white : new Color(0.25f, 0.12f, 0.35f, 1f);
                if (mouse.leftButton.wasPressedThisFrame) close = true;
            }

            if (close) CloseGuide();
        }

        void SetControlsOpen(bool open)
        {
            m_ControlsOpen = open;
            if (m_ControlsPanel != null) m_ControlsPanel.SetActive(open);
        }

        // ---------------- Kira habla ----------------------------------------

        void UpdateKiraDialogue()
        {
            if (!mostrarDialogosDeKira)
            {
                HideBubble();
                return;
            }

            if (m_Game == null || m_BubbleRect == null) return;

            if (m_Game.State != LevelState.Playing)
            {
                HideBubble();
                return;
            }

            float mana = m_Game.ManaNormalized;
            if (mana >= 0.6f) m_HalfArmed = true;
            if (mana >= 0.25f) m_EmptyArmed = true;

            if (mana <= 0.001f && m_EmptyArmed)
            {
                m_EmptyArmed = false;
                m_HalfArmed = false;   // ya hablo del mana: no repite el de la mitad hasta recuperarse
                ShowBubble(emptyManaMessage);
            }
            else if (mana <= 0.5f && mana > 0.001f && m_HalfArmed)
            {
                m_HalfArmed = false;
                ShowBubble(halfManaMessage);
            }
        }

        void ShowBubble(string message)
        {
            m_BubbleText.text = message;
            m_BubbleAge = 0f;
            m_BubbleActive = true;
            m_BubbleRect.localScale = new Vector3(0.5f, 0.5f, 1f);
            m_BubbleGroup.alpha = 0f;
            m_BubbleRect.gameObject.SetActive(true);
        }

        void HideBubble()
        {
            if (!m_BubbleActive) return;
            m_BubbleActive = false;
            m_BubbleRect.gameObject.SetActive(false);
        }

        void TickBubble()
        {
            if (!m_BubbleActive) return;

            m_BubbleAge += Time.deltaTime;
            if (m_BubbleAge >= dialogueSeconds)
            {
                HideBubble();
                return;
            }

            // aparece con un saltito (crece desde la colita) y se desvanece al final
            float pop = Mathf.Clamp01(m_BubbleAge / 0.2f);
            float s = Mathf.LerpUnclamped(0.5f, 1f, EaseOutBack(pop));
            m_BubbleRect.localScale = new Vector3(s, s, 1f);

            float fadeIn = Mathf.Clamp01(m_BubbleAge / 0.1f);
            float fadeOut = Mathf.Clamp01((dialogueSeconds - m_BubbleAge) / 0.4f);
            m_BubbleGroup.alpha = Mathf.Min(fadeIn, fadeOut);
        }

        static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        // ---------------- eventos ------------------------------------------

        void OnStateChanged(LevelState state, string message)
        {
            switch (state)
            {
                case LevelState.Won:
                {
                    // Si hay siguiente nivel, el GameManager va a cargarlo solo.
                    string siguiente = m_Game != null ? m_Game.SiguienteNivel : "";
                    string pie = string.IsNullOrEmpty(siguiente)
                        ? "R = reiniciar"
                        : "Siguiente nivel…\nhaz click para ir ya";

                    // Con el arte del libro no hace falta el cartel de texto: la
                    // frase ya esta escrita en el dibujo.
                    if (m_Libro != null && m_Libro.TieneArte)
                    {
                        ShowResult(false, "");
                        m_Libro.Mostrar();
                    }
                    else
                    {
                        ShowResult(true, "¡NIVEL SUPERADO!\n" + message + "\n\n" + pie);
                    }
                    break;
                }
                case LevelState.Lost:
                    if (m_Libro != null) m_Libro.Ocultar();
                    ShowResult(true, "NIVEL FALLIDO\n" + message);
                    break;
                default:
                    ShowResult(false, "");
                    if (m_Libro != null) m_Libro.Ocultar();
                    break;
            }
            RefreshTitle();
        }

        void OnHint(string message)
        {
            if (m_HintText == null) return;
            m_HintText.text = message;
            m_HintTimer = 3.5f;
        }

        void RefreshTitle()
        {
            if (m_TitleText == null) return;
            int done = m_Game != null ? m_Game.DeliveredIngredients : 0;
            int total = m_Game != null ? m_Game.TotalIngredients : 0;
            var builder = m_Game != null ? m_Game.builder : null;
            string title = builder != null ? builder.CurrentDisplayName : Level01Data.DisplayName;

            // El nombre del nivel en crema y el contador en dorado, para que se lean
            // como dos cosas distintas y no como una linea de depuracion.
            m_TitleText.text = title +
                "   <color=#FFD27A>" + done + " / " + total + " ingredientes</color>";
        }

        void ShowResult(bool visible, string message)
        {
            if (m_ResultPanel != null) m_ResultPanel.gameObject.SetActive(visible);
            if (m_ResultText != null) m_ResultText.text = message;
        }

        // ---------------- construccion del canvas ---------------------------

        void BuildUi()
        {
            var canvasGo = new GameObject("HUD_Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            m_Canvas = canvas;
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            Font font = LoadFont();

            // El HUD esta disenado en un espacio logico de 480 x 270 (16:9) y se
            // escala al alto de referencia real. Con 1080 de alto, k = 4.
            float k = referenceHeight / 270f;
            int Sz(float v) => Mathf.Max(1, Mathf.RoundToInt(v * k));
            Vector2 V(float x, float y) => new Vector2(x * k, y * k);

            // --- barra superior
            //
            // Todo lo de arriba se alinea con el MARCO DE MADERA, no con el borde de
            // la pantalla. Antes iba pegado a las esquinas del canvas y quedaba
            // desperdigado sobre el fondo, sobre todo en itch.
            //
            // La franja libre que queda por encima del marco mide FranjaAlto.
            float MarcoIzq = (Level01Art.MarcoCentro.x - Level01Art.MarcoTamano.x * 0.5f + 8f) * 30f;
            float MarcoDer = (Level01Art.MarcoCentro.x + Level01Art.MarcoTamano.x * 0.5f + 8f) * 30f;
            float FranjaAlto = (4.5f - (Level01Art.MarcoCentro.y + Level01Art.MarcoTamano.y * 0.5f)) * 30f;

            // Las cajas ocupan la franja entera (pivote arriba-izquierda, y = 0) y el
            // texto se centra dentro con MiddleLeft / MiddleRight.
            if (mostrarTitulo)
            {
                m_TitleText = MakeText(canvasGo.transform, "Txt_Titulo", font, Sz(10), TextAnchor.MiddleLeft,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), V(MarcoIzq, 0f), V(260f, FranjaAlto));
                m_TitleText.color = new Color(0.97f, 0.93f, 0.86f, 1f);
                m_TitleText.supportRichText = true;
                Sombra(m_TitleText.gameObject);
            }

            // La pista va entre el titulo y los botones, pegada por la derecha.
            m_HintText = MakeText(canvasGo.transform, "Txt_Pista", font, Sz(9), TextAnchor.MiddleRight,
                new Vector2(0f, 1f), new Vector2(0f, 1f), V(MarcoDer - 220f, 0f), V(150f, FranjaAlto));
            m_HintText.color = PlaceholderArt.Highlight;
            Sombra(m_HintText.gameObject);

            // --- barra de mana VERTICAL
            // Va en el margen izquierdo (a la izquierda del marco de madera), debajo de
            // Kira y por encima del texto de controles. En el espacio logico de 480 x 270
            // ese margen mide ~37 de ancho.
            const float manaW = 10f, manaH = 120f, manaX = 14f, manaY = 26f;

            // Si el nivel ya trae la barra de mana con el arte de Melanie (ManaBar,
            // sprites en el mundo), esta de placeholder no se dibuja: se verian las
            // dos, una encima de la otra.
            if (FindFirstObjectByType<ManaBar>() == null)
            {
                var manaBorder = MakeImage(canvasGo.transform, "ManaBar_Borde",
                    new Vector2(0f, 0f), new Vector2(0f, 0f), V(manaX - 1f, manaY - 1f), V(manaW + 2f, manaH + 2f));
                manaBorder.color = new Color(1f, 1f, 1f, 0.35f);

                var manaBg = MakeImage(manaBorder.transform, "ManaBar_Bg",
                    new Vector2(0f, 0f), new Vector2(0f, 0f), V(1f, 1f), V(manaW, manaH));
                manaBg.color = new Color(0.05f, 0.03f, 0.1f, 0.8f);

                // Image.Filled recorta la figura; ya no se comprime con localScale.
                m_ManaFill = MakeImage(manaBg.transform, "ManaBar_Fill",
                    new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, V(manaW, manaH));
                m_ManaFill.type = Image.Type.Filled;
                m_ManaFill.fillMethod = Image.FillMethod.Vertical;
                m_ManaFill.fillOrigin = (int)Image.OriginVertical.Bottom;
                m_ManaFill.fillAmount = 1f;
                m_ManaFill.color = PlaceholderArt.PortalACol;

                var manaLabel = MakeText(canvasGo.transform, "Txt_Mana", font, Sz(8), TextAnchor.LowerLeft,
                    new Vector2(0f, 0f), new Vector2(0f, 0f), V(manaX - 1f, manaY + manaH + 4f), V(30f, 10f));
                manaLabel.text = "MANÁ";
            }

            // --- controles (desplegables con el boton ?)
            m_ControlsPanel = MakeImage(canvasGo.transform, "Panel_Controles",
                new Vector2(0f, 0f), new Vector2(0f, 0f), V(34f, 3f), V(440f, 28f)).gameObject;
            var controlsBg = m_ControlsPanel.GetComponent<Image>();
            controlsBg.sprite = FxSprites.RoundedBox;
            controlsBg.type = Image.Type.Sliced;
            controlsBg.color = new Color(0.07f, 0.04f, 0.14f, 0.85f);

            m_ControlsText = MakeText(m_ControlsPanel.transform, "Txt_Controles", font, Sz(10), TextAnchor.MiddleLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), V(7f, 3f), V(428f, 22f));
            m_ControlsText.text = "Click = elegir ingrediente   |   Mantener ESPACIO = SUBIR   |   " +
                                  "Flechas IZQ/DER = MOVER   |   Soltar ESPACIO = CAER   |   " +
                                  "Click y mantener = interactuar   |   Tab = cambiar   |   R = reiniciar";

            // --- boton ? magico: en la franja de arriba, a la izquierda del boton de
            // menu. Antes iba abajo a la izquierda, donde se montaba encima de la
            // barra de mana y se salia por el borde de la pantalla.
            const float AyudaLado = 18f;
            float AyudaDer = MarcoDer - 47f;                 // deja sitio al boton de menu
            float AyudaY = -(FranjaAlto - AyudaLado) * 0.5f; // centrado en la franja

            m_HelpGlow = MakeImage(canvasGo.transform, "Help_Brillo",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                V(AyudaDer - AyudaLado - 5f, AyudaY + 5f), V(AyudaLado + 10f, AyudaLado + 10f));
            m_HelpGlow.sprite = FxSprites.Glow;
            m_HelpGlow.color = new Color(1f, 0.85f, 0.4f, 0.45f);
            m_HelpRect = m_HelpGlow.rectTransform;   // esta es el area donde se hace click

            var helpRing = MakeImage(canvasGo.transform, "Help_Aro",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                V(AyudaDer - AyudaLado, AyudaY), V(AyudaLado, AyudaLado));
            m_HelpRing = helpRing.rectTransform;

            if (spriteBotonAyuda != null)
            {
                // Con el dibujo de Melanie basta una imagen: ya trae el aro dorado,
                // el fondo y el signo. El codigo solo lo coloca y lo anima.
                helpRing.sprite = spriteBotonAyuda;
                helpRing.color = Color.white;
                helpRing.preserveAspect = true;
                m_BrilloAyudaMax = 0.4f;
            }
            else
            {
                // Placeholder: aro + disco + signo dibujados por codigo.
                helpRing.sprite = FxSprites.Disc;
                helpRing.color = new Color(1f, 0.82f, 0.35f, 1f);

                var helpDisc = MakeImage(helpRing.transform, "Help_Fondo",
                    new Vector2(0f, 1f), new Vector2(0f, 1f), V(1.5f, -1.5f), V(AyudaLado - 3f, AyudaLado - 3f));
                helpDisc.sprite = FxSprites.Disc;
                helpDisc.color = new Color(0.3f, 0.16f, 0.5f, 1f);

                var helpMark = MakeText(helpRing.transform, "Help_Signo", font, Sz(13), TextAnchor.MiddleCenter,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, V(AyudaLado, AyudaLado));
                helpMark.text = "?";
                helpMark.fontStyle = FontStyle.Bold;
                helpMark.color = new Color(1f, 0.93f, 0.62f, 1f);
            }

            // La tira de controles de una sola linea se reemplazo por la guia (popup).
            SetControlsOpen(false);

            // --- boton de volver al menu: alineado con la esquina superior DERECHA
            // DEL MARCO, dentro de la franja.
            //
            // El ancho sale del PROPIO dibujo, no de un numero escrito aqui: si
            // Melanie reexporta el boton con otras proporciones, se ajusta solo.
            float altoBoton = Mathf.Min(FranjaAlto - 4f, 20f);
            float aspectoBoton = 2.6f;
            if (spriteBotonMenu != null && spriteBotonMenu.rect.height > 0f)
                aspectoBoton = spriteBotonMenu.rect.width / spriteBotonMenu.rect.height;

            Vector2 tamBoton = new Vector2(altoBoton * aspectoBoton, altoBoton);

            var menuBtn = MakeImage(canvasGo.transform, "Btn_Menu",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                V(MarcoDer - tamBoton.x, -(FranjaAlto - altoBoton) * 0.5f), V(tamBoton.x, tamBoton.y));
            m_MenuRect = menuBtn.rectTransform;

            if (spriteBotonMenu != null)
            {
                // Con arte propio no hace falta ni el fondo de color ni el texto.
                menuBtn.sprite = spriteBotonMenu;
                menuBtn.color = Color.white;
                menuBtn.preserveAspect = true;
                m_MenuLabel = null;
            }
            else
            {
                menuBtn.color = new Color(0.22f, 0.12f, 0.33f, 0.88f);

                m_MenuLabel = MakeText(menuBtn.transform, "Txt_Menu", font, Sz(9), TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
                m_MenuLabel.rectTransform.offsetMin = Vector2.zero;
                m_MenuLabel.rectTransform.offsetMax = Vector2.zero;
                m_MenuLabel.text = "MENÚ";
                m_MenuLabel.color = new Color(1f, 0.93f, 0.72f, 1f);
            }

            // --- libro de "nivel superado" (si hay arte asignado)
            m_Libro = GetComponent<VictoryBook>();
            if (m_Libro != null && m_Libro.TieneArte) m_Libro.Construir(canvasGo.transform, font);

            // --- guia de como jugar (popup centrado; arriba de todo menos el panel de resultado)
            m_GuideRoot = new GameObject("Guia_Inicio");
            m_GuideRoot.transform.SetParent(canvasGo.transform, false);
            var guideRt = m_GuideRoot.AddComponent<RectTransform>();
            guideRt.anchorMin = Vector2.zero;
            guideRt.anchorMax = Vector2.one;
            guideRt.offsetMin = Vector2.zero;
            guideRt.offsetMax = Vector2.zero;
            var dim = m_GuideRoot.AddComponent<Image>();
            dim.color = new Color(0.02f, 0.01f, 0.06f, 0.62f);
            dim.raycastTarget = false;

            var guideBorder = MakeImage(m_GuideRoot.transform, "Guia_Borde",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(336f, 196f));
            guideBorder.sprite = FxSprites.RoundedBox;
            guideBorder.type = Image.Type.Sliced;
            guideBorder.color = new Color(1f, 0.82f, 0.35f, 1f);
            m_GuideBox = guideBorder.rectTransform;

            var guidePanel = MakeImage(guideBorder.transform, "Guia_Panel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(332f, 192f));
            guidePanel.sprite = FxSprites.RoundedBox;
            guidePanel.type = Image.Type.Sliced;
            guidePanel.color = new Color(0.1f, 0.05f, 0.2f, 0.98f);

            var guideTitle = MakeText(guidePanel.transform, "Guia_Titulo", font, Sz(15), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), V(0f, -9f), V(310f, 20f));
            guideTitle.text = "\u00bfC\u00f3mo se juega?";
            guideTitle.fontStyle = FontStyle.Bold;
            guideTitle.color = new Color(1f, 0.9f, 0.5f, 1f);

            var guideBody = MakeText(guidePanel.transform, "Guia_Texto", font, Sz(9), TextAnchor.UpperLeft,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), V(0f, -34f), V(300f, 118f));
            guideBody.supportRichText = true;
            guideBody.lineSpacing = 1.1f;
            guideBody.color = new Color(0.97f, 0.93f, 1f, 1f);
            guideBody.text =
                "Lleva el ingrediente hasta el caldero sin romperlo.\n\n" +
                "<b>Click</b> sobre el ingrediente  =  elegirlo\n" +
                "<b>Mantener ESPACIO</b>  =  levitar (gasta man\u00e1)\n" +
                "<b>Flechas IZQ / DER</b>  =  moverlo (conserva el impulso)\n" +
                "<b>Soltar ESPACIO</b>  =  caer (desde muy alto se rompe)\n" +
                "<b>Tab</b>  =  cambiar de ingrediente      <b>R</b>  =  reiniciar\n\n" +
                "Vuelve a ver esto con el bot\u00f3n <b>?</b> de abajo a la izquierda.";

            var guideBtn = MakeImage(guidePanel.transform, "Guia_Boton",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), V(0f, 9f), V(120f, 22f));
            guideBtn.sprite = FxSprites.RoundedBox;
            guideBtn.type = Image.Type.Sliced;
            guideBtn.color = new Color(1f, 0.82f, 0.35f, 1f);
            guideBtn.rectTransform.pivot = new Vector2(0.5f, 0f);
            m_GuideButtonRect = guideBtn.rectTransform;

            m_GuideButtonLabel = MakeText(guideBtn.transform, "Guia_Boton_Txt", font, Sz(10), TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            m_GuideButtonLabel.rectTransform.offsetMin = Vector2.zero;
            m_GuideButtonLabel.rectTransform.offsetMax = Vector2.zero;
            m_GuideButtonLabel.text = "\u00a1Entendido!  (ENTER)";
            m_GuideButtonLabel.fontStyle = FontStyle.Bold;
            m_GuideButtonLabel.color = new Color(0.25f, 0.12f, 0.35f, 1f);

            m_GuideRoot.SetActive(false);

            // --- menu de pausa (ESC): Continuar, Como jugar, Reiniciar, Menu principal
            m_PauseRoot = new GameObject("Menu_Pausa");
            m_PauseRoot.transform.SetParent(canvasGo.transform, false);
            var pauseRt = m_PauseRoot.AddComponent<RectTransform>();
            pauseRt.anchorMin = Vector2.zero;
            pauseRt.anchorMax = Vector2.one;
            pauseRt.offsetMin = Vector2.zero;
            pauseRt.offsetMax = Vector2.zero;
            var pauseDim = m_PauseRoot.AddComponent<Image>();
            pauseDim.color = new Color(0.02f, 0.01f, 0.06f, 0.68f);
            pauseDim.raycastTarget = false;

            var pauseBorder = MakeImage(m_PauseRoot.transform, "Pausa_Borde",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(176f, 152f));
            pauseBorder.sprite = FxSprites.RoundedBox;
            pauseBorder.type = Image.Type.Sliced;
            pauseBorder.color = new Color(1f, 0.82f, 0.35f, 1f);
            m_PauseBox = pauseBorder.rectTransform;

            var pausePanel = MakeImage(pauseBorder.transform, "Pausa_Panel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(172f, 148f));
            pausePanel.sprite = FxSprites.RoundedBox;
            pausePanel.type = Image.Type.Sliced;
            pausePanel.color = new Color(0.1f, 0.05f, 0.2f, 0.98f);

            var pauseTitle = MakeText(pausePanel.transform, "Pausa_Titulo", font, Sz(16), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), V(0f, -8f), V(150f, 20f));
            pauseTitle.text = "PAUSA";
            pauseTitle.fontStyle = FontStyle.Bold;
            pauseTitle.color = new Color(1f, 0.9f, 0.5f, 1f);

            string[] pauseNames =
            {
                "Continuar", "\u00bfC\u00f3mo se juega?", "Reiniciar nivel", "Men\u00fa principal"
            };
            m_PauseButtons = new RectTransform[pauseNames.Length];
            m_PauseLabels = new Text[pauseNames.Length];
            for (int i = 0; i < pauseNames.Length; i++)
            {
                var btn = MakeImage(pausePanel.transform, "Pausa_Boton_" + i,
                    new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), V(0f, -34f - i * 25f), V(130f, 21f));
                btn.sprite = FxSprites.RoundedBox;
                btn.type = Image.Type.Sliced;
                btn.color = new Color(1f, 0.82f, 0.35f, 1f);
                m_PauseButtons[i] = btn.rectTransform;

                var label = MakeText(btn.transform, "Pausa_Boton_Txt", font, Sz(10), TextAnchor.MiddleCenter,
                    new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                label.text = pauseNames[i];
                label.fontStyle = FontStyle.Bold;
                label.color = new Color(0.25f, 0.12f, 0.35f, 1f);
                m_PauseLabels[i] = label;
            }

            var pauseHint = MakeText(pausePanel.transform, "Pausa_Pista", font, Sz(8), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), V(0f, 3f), V(150f, 10f));
            pauseHint.text = "ESC para volver";
            pauseHint.color = new Color(0.8f, 0.75f, 0.95f, 0.9f);

            m_PauseRoot.SetActive(false);

            // --- panel de resultado
            m_ResultPanel = MakeImage(canvasGo.transform, "Panel_Resultado",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(320f, 110f));
            m_ResultPanel.color = new Color(0.05f, 0.03f, 0.1f, 0.88f);

            m_ResultText = MakeText(m_ResultPanel.transform, "Txt_Resultado", font, Sz(14), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, V(300f, 100f));

            // --- globo de dialogo de Kira (arriba a la izquierda, a la derecha de ella)
            // La colita apunta a Kira. Crece desde la colita (pivote a la izquierda).
            var bubbleGo = new GameObject("Kira_Globo");
            bubbleGo.transform.SetParent(canvasGo.transform, false);
            m_BubbleRect = bubbleGo.AddComponent<RectTransform>();
            m_BubbleRect.anchorMin = new Vector2(0f, 1f);
            m_BubbleRect.anchorMax = new Vector2(0f, 1f);
            m_BubbleRect.pivot = new Vector2(0f, 0.5f);
            m_BubbleRect.sizeDelta = V(150f, 34f);
            m_BubbleRect.anchoredPosition = V(100f, -48f);

            m_BubbleGroup = bubbleGo.AddComponent<CanvasGroup>();
            m_BubbleGroup.interactable = false;
            m_BubbleGroup.blocksRaycasts = false;

            Color bubbleOutline = new Color(0.32f, 0.17f, 0.5f, 1f);
            Color bubbleFill = new Color(1f, 0.97f, 0.88f, 1f);

            StretchImage(bubbleGo.transform, "Globo_Borde", FxSprites.RoundedBox, bubbleOutline, 0f);
            MakeDiamond(bubbleGo.transform, "Colita_Borde", bubbleOutline, V(10.5f, 10.5f));
            StretchImage(bubbleGo.transform, "Globo_Relleno", FxSprites.RoundedBox, bubbleFill, 1.3f * k);
            MakeDiamond(bubbleGo.transform, "Colita_Relleno", bubbleFill, V(8f, 8f));

            m_BubbleText = MakeText(bubbleGo.transform, "Txt_Dialogo", font, Sz(9), TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(0f, 0f), V(5f, 2f), V(140f, 30f));
            m_BubbleText.color = new Color(0.25f, 0.12f, 0.35f, 1f);

            bubbleGo.SetActive(false);
        }

        /// <summary>Imagen que ocupa todo el padre, con un margen en pixeles del canvas.</summary>
        static Image StretchImage(Transform parent, string name, Sprite sprite, Color color, float insetPx)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.color = color;

            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(insetPx, insetPx);
            rt.offsetMax = new Vector2(-insetPx, -insetPx);
            return img;
        }

        /// <summary>Cuadrado girado 45 grados en el borde izquierdo del padre: la colita del globo.</summary>
        static Image MakeDiamond(Transform parent, string name, Color color, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.color = color;

            RectTransform rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0f, 0f, 45f);
            return img;
        }

        static Font LoadFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        /// <summary>
        /// Sombra suave detras del texto. El HUD va sobre el fondo pintado, que tiene
        /// zonas claras y oscuras; sin esto el titulo se pierde segun donde caiga.
        /// </summary>
        static void Sombra(GameObject go)
        {
            if (go == null || go.GetComponent<Shadow>() != null) return;
            var s = go.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.65f);
            s.effectDistance = new Vector2(2f, -2f);
        }

        static Text MakeText(Transform parent, string name, Font font, int size, TextAnchor anchor,
                             Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size2)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            RectTransform rt = text.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin.x, anchorMax.y);
            rt.sizeDelta = size2;
            rt.anchoredPosition = position;
            return text;
        }

        static Image MakeImage(Transform parent, string name,
                               Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;

            RectTransform rt = img.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin.x, anchorMax.y);
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            return img;
        }
    }
}
