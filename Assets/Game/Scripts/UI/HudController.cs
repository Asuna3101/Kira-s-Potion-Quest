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

        [Header("Volver al menu")]
        [Tooltip("Escena que carga el boton MENU y la tecla Escape.")]
        public string escenaDelMenu = "MainMenu";

        [Header("Ayuda (boton ?)")]
        [Tooltip("Mostrar las indicaciones de control desde el inicio. Por defecto estan ocultas.")]
        public bool controlsStartOpen = false;

        [Header("Kira habla")]
        [Tooltip("Globo de dialogo de Kira al bajar el mana. Desactivado: la barra de mana ya cuenta eso.")]
        public bool mostrarDialogosDeKira = false;

        public string halfManaMessage = "Oh no, solo te queda la mitad de mana!! T.T";
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
        }

        void OnDestroy()
        {
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
                m_ManaFill.rectTransform.localScale = new Vector3(1f, mana, 1f);
                // Con poco mana vira al rojo para avisar que se acaba.
                m_ManaFill.color = Color.Lerp(PlaceholderArt.PortalACol, PlaceholderArt.Danger,
                    Mathf.InverseLerp(0.3f, 0.1f, mana));
            }

            if (m_HintTimer > 0f)
            {
                m_HintTimer -= Time.deltaTime;
                if (m_HintTimer <= 0f && m_HintText != null) m_HintText.text = "";
            }

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
                VolverAlMenu();
                return;
            }

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

            if (encima && mouse.leftButton.wasPressedThisFrame) VolverAlMenu();
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
                RectTransformUtility.RectangleContainsScreenPoint(m_HelpRect, mouse.position.ReadValue(), null))
            {
                SetControlsOpen(!m_ControlsOpen);
            }

            // El brillo late mientras las indicaciones estan cerradas, para que el boton se note.
            if (m_HelpGlow != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
                float alpha = m_ControlsOpen ? 0.25f : 0.25f + 0.35f * pulse;
                m_HelpGlow.color = new Color(1f, 0.85f, 0.4f, alpha);
            }
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
                        : "Siguiente nivel...\nclick o ESPACIO para ir ya";

                    // Con el arte del libro no hace falta el cartel de texto: la
                    // frase ya esta escrita en el dibujo.
                    if (m_Libro != null && m_Libro.TieneArte)
                    {
                        ShowResult(false, "");
                        m_Libro.Mostrar();
                    }
                    else
                    {
                        ShowResult(true, "NIVEL SUPERADO\n" + message + "\n\n" + pie);
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
            m_TitleText.text = title + "   Ingredientes " + done + " / " + total;
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
            m_TitleText = MakeText(canvasGo.transform, "Txt_Titulo", font, Sz(12), TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), V(6f, -6f), V(300f, 16f));

            m_HintText = MakeText(canvasGo.transform, "Txt_Pista", font, Sz(11), TextAnchor.UpperRight,
                new Vector2(1f, 1f), new Vector2(1f, 1f), V(-6f, -6f), V(300f, 32f));
            m_HintText.color = PlaceholderArt.Highlight;

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

                // pivote abajo-izquierda: al escalar en Y crece hacia arriba
                m_ManaFill = MakeImage(manaBg.transform, "ManaBar_Fill",
                    new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, V(manaW, manaH));
                m_ManaFill.color = PlaceholderArt.PortalACol;

                var manaLabel = MakeText(canvasGo.transform, "Txt_Mana", font, Sz(8), TextAnchor.LowerLeft,
                    new Vector2(0f, 0f), new Vector2(0f, 0f), V(manaX - 1f, manaY + manaH + 4f), V(30f, 10f));
                manaLabel.text = "MANA";
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

            // --- boton ? magico: abajo a la izquierda, junto a la barra de mana
            m_HelpGlow = MakeImage(canvasGo.transform, "Help_Brillo",
                new Vector2(0f, 0f), new Vector2(0f, 0f), V(6f, -1f), V(26f, 26f));
            m_HelpGlow.sprite = FxSprites.Glow;
            m_HelpGlow.color = new Color(1f, 0.85f, 0.4f, 0.4f);
            m_HelpRect = m_HelpGlow.rectTransform;   // esta es el area donde se hace click

            var helpRing = MakeImage(canvasGo.transform, "Help_Aro",
                new Vector2(0f, 0f), new Vector2(0f, 0f), V(10f, 3f), V(18f, 18f));
            helpRing.sprite = FxSprites.Disc;
            helpRing.color = new Color(1f, 0.82f, 0.35f, 1f);

            var helpDisc = MakeImage(helpRing.transform, "Help_Fondo",
                new Vector2(0f, 0f), new Vector2(0f, 0f), V(1.3f, 1.3f), V(15.4f, 15.4f));
            helpDisc.sprite = FxSprites.Disc;
            helpDisc.color = new Color(0.3f, 0.16f, 0.5f, 1f);

            var helpMark = MakeText(helpRing.transform, "Help_Signo", font, Sz(13), TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(0f, 0f), Vector2.zero, V(18f, 18f));
            helpMark.text = "?";
            helpMark.fontStyle = FontStyle.Bold;
            helpMark.color = new Color(1f, 0.93f, 0.62f, 1f);

            SetControlsOpen(controlsStartOpen);

            // --- boton de volver al menu: arriba a la derecha
            var menuBtn = MakeImage(canvasGo.transform, "Btn_Menu",
                new Vector2(1f, 1f), new Vector2(1f, 1f), V(-6f, -6f), V(58f, 20f));
            menuBtn.color = new Color(0.22f, 0.12f, 0.33f, 0.88f);
            menuBtn.rectTransform.pivot = new Vector2(1f, 1f);
            m_MenuRect = menuBtn.rectTransform;

            m_MenuLabel = MakeText(menuBtn.transform, "Txt_Menu", font, Sz(9), TextAnchor.MiddleCenter,
                new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            m_MenuLabel.rectTransform.offsetMin = Vector2.zero;
            m_MenuLabel.rectTransform.offsetMax = Vector2.zero;
            m_MenuLabel.text = "MENU";
            m_MenuLabel.color = new Color(1f, 0.93f, 0.72f, 1f);

            // --- libro de "nivel superado" (si hay arte asignado)
            m_Libro = GetComponent<VictoryBook>();
            if (m_Libro != null && m_Libro.TieneArte) m_Libro.Construir(canvasGo.transform, font);

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
