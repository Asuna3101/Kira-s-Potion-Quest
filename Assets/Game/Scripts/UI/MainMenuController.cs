using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Sprites del menu de inicio (Assets/Game/Sprites/UI).
    /// Igual que el arte del nivel, cada PNG es el lienzo completo de 3840 x 2160
    /// con el elemento en su sitio, ya recortado en el Sprite Editor.
    /// </summary>
    [Serializable]
    public class MenuArt
    {
        public Sprite fondo;            // Fondo_,menu.JPG
        public Sprite titulo;           // Title.PNG
        public Sprite botonPlay;        // Boton_play.PNG
        public Sprite botonCreditos;    // Boton_créditos.PNG
        public Sprite botonSettings;    // Boton_settings.PNG
    }

    /// <summary>
    /// Menu de inicio de Kira's Potion Quest.
    ///
    /// COLOCACION AUTOMATICA
    /// ---------------------
    /// Melanie dibuja cada elemento sobre el lienzo de 3840 x 2160. El canvas del
    /// menu es de 1920 x 1080, exactamente la mitad, asi que la posicion de cada
    /// pieza sale sola de su rect en el Sprite Editor:
    ///
    ///     pos.x  = (rect.x + rect.ancho/2 - 1920) / 2
    ///     pos.y  = (rect.y + rect.alto/2  - 1080) / 2
    ///     tamano = rect.ancho/2 , rect.alto/2
    ///
    /// Es decir: el menu queda identico al mockup sin colocar nada a mano. Si
    /// Melanie reexporta y mueve un boton, se recalculan estas constantes (o se
    /// mira el rect en el Sprite Editor) y listo.
    ///
    /// INPUT
    /// -----
    /// El proyecto usa SOLO el Input System nuevo, asi que el menu no usa
    /// EventSystem ni UnityEngine.UI.Button: lee Mouse.current y hace el
    /// hit-test contra el RectTransform de cada boton. Menos piezas que
    /// configurar y no depende de que el EventSystem tenga el modulo correcto.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Arte (Assets/Game/Sprites/UI)")]
        public MenuArt art = new MenuArt();

        [Header("Escenas")]
        [Tooltip("Escena que carga el boton Play. Debe estar en Build Settings.")]
        public string escenaDeJuego = "Level01";

        [Header("Canvas")]
        public int canvasWidth = GridSettings.ReferenceWidthPx;   // 1920
        public int canvasHeight = GridSettings.ReferenceHeightPx; // 1080

        [Header("Feedback")]
        [Tooltip("Cuanto crece el boton al pasar el mouse por encima.")]
        public float hoverScale = 1.06f;
        public float hoverLerp = 12f;

        // --- posiciones medidas de los rects del Sprite Editor ---------------
        //  Title           rect (721, 694, 2489, 1262)
        //  Boton_créditos  rect (528, 203,  772,  374)
        //  Boton_play      rect (1539,197,  772,  374)
        //  Boton_settings  rect (2550,197,  772,  374)

        static readonly Vector2 TituloPos = new Vector2(22.75f, 122.5f);
        static readonly Vector2 TituloSize = new Vector2(1244.5f, 631f);

        static readonly Vector2 CreditosPos = new Vector2(-503f, -345f);
        static readonly Vector2 PlayPos = new Vector2(2.5f, -348f);
        static readonly Vector2 SettingsPos = new Vector2(508f, -348f);
        static readonly Vector2 BotonSize = new Vector2(386f, 187f);

        class Boton
        {
            public RectTransform rect;
            public Image image;
            public Action onClick;
            public Vector3 baseScale;
            public bool hover;

            /// <summary>Los botones de dentro de un panel son los unicos activos mientras ese panel esta abierto.</summary>
            public bool esDePanel;
        }

        readonly List<Boton> m_Botones = new List<Boton>();
        Canvas m_Canvas;
        RectTransform m_PanelCreditos;
        RectTransform m_PanelAjustes;
        Font m_Font;

        bool HayPanelAbierto =>
            (m_PanelCreditos != null && m_PanelCreditos.gameObject.activeSelf) ||
            (m_PanelAjustes != null && m_PanelAjustes.gameObject.activeSelf);

        void Start()
        {
            m_Font = CargarFuente();
            Construir();
        }

        // ------------------------------------------------------------ input

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && HayPanelAbierto)
                CerrarPaneles();

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screen = mouse.position.ReadValue();
            bool click = mouse.leftButton.wasPressedThisFrame;
            bool bloqueado = HayPanelAbierto;

            foreach (var b in m_Botones)
            {
                // Con un panel abierto solo responden los botones del panel.
                bool activo = b.rect.gameObject.activeInHierarchy && (!bloqueado || b.esDePanel);

                bool dentro = activo &&
                              RectTransformUtility.RectangleContainsScreenPoint(b.rect, screen, null);
                b.hover = dentro;

                float objetivo = dentro ? hoverScale : 1f;
                b.rect.localScale = Vector3.Lerp(
                    b.rect.localScale,
                    b.baseScale * objetivo,
                    1f - Mathf.Exp(-hoverLerp * Time.unscaledDeltaTime));

                if (dentro && click) b.onClick?.Invoke();
            }
        }

        // ------------------------------------------------------- acciones

        public void Jugar()
        {
            if (string.IsNullOrEmpty(escenaDeJuego)) return;

            if (!Application.CanStreamedLevelBeLoaded(escenaDeJuego))
            {
                Debug.LogError("[Menu] La escena '" + escenaDeJuego + "' no esta en Build Settings. " +
                               "Menu Chocoflan > 1 - Aplicar ajustes de proyecto, o " +
                               "File > Build Profiles > Scene List.");
                return;
            }
            SceneManager.LoadScene(escenaDeJuego);
        }

        public void AbrirCreditos()
        {
            CerrarPaneles();
            if (m_PanelCreditos != null) m_PanelCreditos.gameObject.SetActive(true);
        }

        public void AbrirAjustes()
        {
            CerrarPaneles();
            if (m_PanelAjustes != null) m_PanelAjustes.gameObject.SetActive(true);
        }

        public void CerrarPaneles()
        {
            if (m_PanelCreditos != null) m_PanelCreditos.gameObject.SetActive(false);
            if (m_PanelAjustes != null) m_PanelAjustes.gameObject.SetActive(false);
        }

        // --------------------------------------------------- construccion

        void Construir()
        {
            var canvasGo = new GameObject("Menu_Canvas");
            canvasGo.transform.SetParent(transform, false);

            m_Canvas = canvasGo.AddComponent<Canvas>();
            m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(canvasWidth, canvasHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;   // encaja por alto, como la camara del juego

            // --- fondo, estirado a toda la pantalla
            var fondo = NuevaImagen(canvasGo.transform, "Fondo", art.fondo);
            fondo.rectTransform.anchorMin = Vector2.zero;
            fondo.rectTransform.anchorMax = Vector2.one;
            fondo.rectTransform.offsetMin = Vector2.zero;
            fondo.rectTransform.offsetMax = Vector2.zero;
            fondo.preserveAspect = false;
            if (art.fondo == null) fondo.color = new Color32(0x1E, 0x1A, 0x33, 0xFF);

            // --- titulo
            var titulo = NuevaImagen(canvasGo.transform, "Titulo", art.titulo);
            Colocar(titulo.rectTransform, TituloPos, TituloSize);
            if (art.titulo == null)
            {
                titulo.color = new Color(0, 0, 0, 0);
                var txt = NuevoTexto(titulo.transform, "Txt_Titulo", "Kira's Potion Quest", 90, TextAnchor.MiddleCenter);
                Estirar(txt.rectTransform);
            }
            var anim = titulo.gameObject.AddComponent<PlaceholderAnimator>();
            anim.mode = PlaceholderAnimator.Mode.Bob;
            anim.speed = 1.1f;
            anim.amplitude = 6f;          // en pixeles de canvas, no en unidades

            // --- botones (de izquierda a derecha, como en el mockup)
            CrearBoton(canvasGo.transform, "Boton_Creditos", art.botonCreditos, "creditos",
                CreditosPos, AbrirCreditos);
            CrearBoton(canvasGo.transform, "Boton_Play", art.botonPlay, "play",
                PlayPos, Jugar);
            CrearBoton(canvasGo.transform, "Boton_Settings", art.botonSettings, "settings",
                SettingsPos, AbrirAjustes);

            // --- paneles
            m_PanelCreditos = ConstruirCreditos(canvasGo.transform);
            m_PanelAjustes = ConstruirAjustes(canvasGo.transform);
            CerrarPaneles();
        }

        void CrearBoton(Transform parent, string nombre, Sprite sprite, string etiqueta,
                        Vector2 pos, Action onClick)
        {
            var img = NuevaImagen(parent, nombre, sprite);
            Colocar(img.rectTransform, pos, BotonSize);

            if (sprite == null)
            {
                img.color = new Color32(0x6B, 0x4A, 0x33, 0xFF);
                var txt = NuevoTexto(img.transform, "Txt", etiqueta, 48, TextAnchor.MiddleCenter);
                Estirar(txt.rectTransform);
            }

            m_Botones.Add(new Boton
            {
                rect = img.rectTransform,
                image = img,
                onClick = onClick,
                baseScale = img.rectTransform.localScale
            });
        }

        RectTransform ConstruirCreditos(Transform parent)
        {
            var panel = NuevoPanel(parent, "Panel_Creditos", new Vector2(1100f, 700f));

            NuevoTexto(panel.transform, "Txt_Titulo", "Chocoflan", 72, TextAnchor.UpperCenter)
                .rectTransform.anchoredPosition = new Vector2(0f, -40f);

            var cuerpo = NuevoTexto(panel.transform, "Txt_Equipo",
                "Sasha Servat Quezada  -  Productora / Game Designer\n" +
                "Melanie Pinto Sulca  -  Artista / Animadora\n" +
                "Andrea Ximena Fontenla Alva  -  Game Designer / Programadora\n" +
                "Edson Vasquez Solis  -  Programador\n" +
                "Enzo David Castro Contreras  -  Programador",
                40, TextAnchor.MiddleCenter);
            cuerpo.rectTransform.sizeDelta = new Vector2(980f, 400f);
            cuerpo.rectTransform.anchoredPosition = new Vector2(0f, 10f);

            BotonTexto(panel, "Volver", new Vector2(0f, -280f), CerrarPaneles);
            return panel.GetComponent<RectTransform>();
        }

        RectTransform ConstruirAjustes(Transform parent)
        {
            var panel = NuevoPanel(parent, "Panel_Ajustes", new Vector2(900f, 560f));

            NuevoTexto(panel.transform, "Txt_Titulo", "Ajustes", 72, TextAnchor.UpperCenter)
                .rectTransform.anchoredPosition = new Vector2(0f, -40f);

            // Volumen general: barra con dos botones. Simple a proposito, el
            // mixer de audio todavia no existe.
            var etiqueta = NuevoTexto(panel.transform, "Txt_Volumen", "Volumen", 44, TextAnchor.MiddleCenter);
            etiqueta.rectTransform.anchoredPosition = new Vector2(0f, 60f);
            etiqueta.rectTransform.sizeDelta = new Vector2(700f, 60f);

            var valor = NuevoTexto(panel.transform, "Txt_Valor",
                Mathf.RoundToInt(AudioListener.volume * 100f) + " %", 44, TextAnchor.MiddleCenter);
            valor.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            valor.rectTransform.sizeDelta = new Vector2(300f, 60f);

            BotonTexto(panel, "-", new Vector2(-220f, -10f), () =>
            {
                AudioListener.volume = Mathf.Clamp01(AudioListener.volume - 0.1f);
                valor.text = Mathf.RoundToInt(AudioListener.volume * 100f) + " %";
            }, new Vector2(110f, 80f));

            BotonTexto(panel, "+", new Vector2(220f, -10f), () =>
            {
                AudioListener.volume = Mathf.Clamp01(AudioListener.volume + 0.1f);
                valor.text = Mathf.RoundToInt(AudioListener.volume * 100f) + " %";
            }, new Vector2(110f, 80f));

            var pantalla = NuevoTexto(panel.transform, "Txt_Pantalla",
                Screen.fullScreen ? "Pantalla completa" : "Ventana", 40, TextAnchor.MiddleCenter);
            pantalla.rectTransform.anchoredPosition = new Vector2(0f, -110f);
            pantalla.rectTransform.sizeDelta = new Vector2(700f, 60f);

            BotonTexto(panel, "Cambiar", new Vector2(0f, -180f), () =>
            {
                Screen.fullScreen = !Screen.fullScreen;
                pantalla.text = Screen.fullScreen ? "Pantalla completa" : "Ventana";
            }, new Vector2(300f, 80f));

            BotonTexto(panel, "Volver", new Vector2(0f, -250f), CerrarPaneles);
            return panel.GetComponent<RectTransform>();
        }

        // ------------------------------------------------------- utilidades

        GameObject NuevoPanel(Transform parent, string nombre, Vector2 size)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.07f, 0.05f, 0.12f, 0.93f);
            Colocar(img.rectTransform, Vector2.zero, size);
            return go;
        }

        void BotonTexto(GameObject parent, string etiqueta, Vector2 pos, Action onClick,
                        Vector2? size = null)
        {
            var go = new GameObject("Btn_" + etiqueta);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = new Color32(0x6B, 0x4A, 0x33, 0xFF);
            Colocar(img.rectTransform, pos, size ?? new Vector2(300f, 90f));

            var txt = NuevoTexto(go.transform, "Txt", etiqueta, 40, TextAnchor.MiddleCenter);
            Estirar(txt.rectTransform);

            m_Botones.Add(new Boton
            {
                rect = img.rectTransform,
                image = img,
                onClick = onClick,
                baseScale = img.rectTransform.localScale,
                esDePanel = true
            });
        }

        Image NuevaImagen(Transform parent, string nombre, Sprite sprite)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        Text NuevoTexto(Transform parent, string nombre, string contenido, int size, TextAnchor anchor)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = m_Font;
            txt.fontSize = size;
            txt.alignment = anchor;
            txt.text = contenido;
            txt.color = new Color32(0xF5, 0xE4, 0xC8, 0xFF);
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            Colocar(txt.rectTransform, Vector2.zero, new Vector2(600f, 80f));
            return txt;
        }

        static void Colocar(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Font CargarFuente()
        {
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
