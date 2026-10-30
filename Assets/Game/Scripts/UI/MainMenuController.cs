using System;
using System.Collections;
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
        public string escenaDeJuego = "LevelSelect";

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
            public bool animating;
        }

        readonly List<Boton> m_Botones = new List<Boton>();
        Canvas m_Canvas;
        RectTransform m_PanelCreditos;
        RectTransform m_PanelAjustes;
        Font m_Font;

        /// <summary>
        /// Camara que hay que pasarle al hit-test de los botones. En Screen Space -
        /// Overlay tiene que ser null; en Screen Space - Camera, la del canvas. Si
        /// se equivoca, los clicks caen en el sitio que no es.
        /// </summary>
        Camera CamaraDelCanvas =>
            m_Canvas == null || m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : m_Canvas.worldCamera;

        bool HayPanelAbierto =>
            (m_PanelCreditos != null && m_PanelCreditos.gameObject.activeSelf) ||
            (m_PanelAjustes != null && m_PanelAjustes.gameObject.activeSelf);

        /// <summary>Nombre del canvas del menu, tanto horneado como creado en Play.</summary>
        public const string NombreCanvas = "Menu_Canvas";

        void Start()
        {
            m_Font = CargarFuente();

            // Si el menu ya esta HORNEADO en la escena (se ve en la Hierarchy), se
            // reutiliza tal cual y solo se le enganchan las acciones a los botones.
            // Si no esta, se construye en caliente como antes.
            Canvas horneado = BuscarCanvasHorneado();
            if (horneado != null) EngancharCanvasHorneado(horneado);
            else Construir();

            // Los paneles de creditos y ajustes siempre se arman en caliente: son
            // dialogos, no algo que se retoque en la Hierarchy.
            ConstruirPaneles();
            CerrarPaneles();

            Debug.Log("[Menu] Listo (" + (horneado != null ? "horneado en la escena" : "construido en Play") +
                      "). Botones: " + m_Botones.Count +
                      " | fondo=" + (art.fondo != null) +
                      " titulo=" + (art.titulo != null) +
                      " play=" + (art.botonPlay != null) +
                      " | fuente=" + (m_Font != null ? m_Font.name : "NINGUNA") +
                      " | canvas hijos=" + (m_Canvas != null ? m_Canvas.transform.childCount : -1), this);
        }

        Canvas BuscarCanvasHorneado()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform hijo = transform.GetChild(i);
                if (hijo.name != NombreCanvas) continue;
                var c = hijo.GetComponent<Canvas>();
                if (c != null) return c;
            }
            return null;
        }

        /// <summary>
        /// Reutiliza los objetos ya presentes en la escena: busca los botones por
        /// nombre y les engancha su accion. No crea ni mueve nada, asi que lo que
        /// se vea en la Hierarchy es exactamente lo que se juega.
        /// </summary>
        void EngancharCanvasHorneado(Canvas canvas)
        {
            m_Canvas = canvas;
            m_Botones.Clear();

            EngancharBoton(canvas.transform, "Boton_Creditos", AbrirCreditos);
            EngancharBoton(canvas.transform, "Boton_Play", Jugar);
            EngancharBoton(canvas.transform, "Boton_Settings", AbrirAjustes);
        }

        void EngancharBoton(Transform canvas, string nombre, Action onClick)
        {
            Transform t = canvas.Find(nombre);
            if (t == null)
            {
                Debug.LogWarning("[Menu] No encontre el boton '" + nombre + "' dentro de " +
                                 NombreCanvas + ". Vuelve a hornear el menu desde " +
                                 "Chocoflan > 7 - Hornear menu en la escena.", this);
                return;
            }

            var rt = t as RectTransform;
            if (rt == null) return;

            m_Botones.Add(new Boton
            {
                rect = rt,
                image = t.GetComponent<Image>(),
                onClick = onClick,
                baseScale = rt.localScale
            });
        }

        // ------------------------------------------------- horneado (Editor)

        /// <summary>
        /// Crea el menu como GameObjects de verdad en la escena, para poder verlo y
        /// retocarlo en la Hierarchy sin entrar en Play. Lo llama el item de menu
        /// Chocoflan > 7.
        /// </summary>
        public void HornearEnEscena()
        {
            LimpiarDeEscena();
            m_Font = CargarFuente();
            Construir();
        }

        /// <summary>Borra el canvas horneado de la escena.</summary>
        public void LimpiarDeEscena()
        {
            m_Botones.Clear();
            m_PanelCreditos = null;
            m_PanelAjustes = null;
            m_Canvas = null;

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform hijo = transform.GetChild(i);
                if (hijo.name != NombreCanvas) continue;
                if (Application.isPlaying) Destroy(hijo.gameObject);
                else DestroyImmediate(hijo.gameObject);
            }
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
                              RectTransformUtility.RectangleContainsScreenPoint(b.rect, screen, CamaraDelCanvas);
                b.hover = dentro;

                if (!b.animating)
                {
                    float objetivo = dentro ? hoverScale : 1f;
                    b.rect.localScale = Vector3.Lerp(
                        b.rect.localScale,
                        b.baseScale * objetivo,
                        1f - Mathf.Exp(-hoverLerp * Time.unscaledDeltaTime));
                }

                if (dentro && click && !b.animating)
                {
                    if (b.esDePanel) b.onClick?.Invoke();
                    else StartCoroutine(AnimarClickMagico(b));
                }
            }
        }


        /// <summary>Pequena respuesta magica al pulsar los tres botones principales.</summary>
        IEnumerator AnimarClickMagico(Boton boton)
        {
            if (boton == null || boton.rect == null) yield break;

            boton.animating = true;
            RectTransform rt = boton.rect;
            Vector3 escalaInicial = rt.localScale;
            float tiempo = 0f;
            const float duracion = 0.28f;

            // Destello de pequenas estrellas doradas alrededor del boton.
            const int cantidad = 8;
            Image[] estrellas = new Image[cantidad];
            Vector2[] direcciones = new Vector2[cantidad];
            Sprite pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1),
                new Vector2(0.5f, 0.5f));

            for (int i = 0; i < cantidad; i++)
            {
                float angulo = (Mathf.PI * 2f * i) / cantidad;
                direcciones[i] = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
                GameObject estrella = new GameObject("Click_Sparkle", typeof(RectTransform), typeof(Image));
                estrella.transform.SetParent(rt, false);
                RectTransform estrellaRt = estrella.GetComponent<RectTransform>();
                estrellaRt.anchorMin = estrellaRt.anchorMax = new Vector2(0.5f, 0.5f);
                estrellaRt.anchoredPosition = Vector2.zero;
                float lado = (i % 2 == 0) ? 13f : 8f;
                estrellaRt.sizeDelta = new Vector2(lado, lado);
                estrellaRt.localRotation = Quaternion.Euler(0f, 0f, 45f);
                estrellas[i] = estrella.GetComponent<Image>();
                estrellas[i].sprite = pixel;
                estrellas[i].color = new Color(1f, 0.82f, 0.38f, 0.95f);
                estrellas[i].raycastTarget = false;
            }

            while (tiempo < duracion)
            {
                tiempo += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(tiempo / duracion);

                // Apretar y luego dar un pequeno rebote.
                float escala;
                if (t < 0.28f) escala = Mathf.Lerp(1f, 0.88f, t / 0.28f);
                else escala = Mathf.Lerp(0.88f, 1.12f, (t - 0.28f) / 0.72f);
                rt.localScale = escalaInicial * escala;

                for (int i = 0; i < cantidad; i++)
                {
                    if (estrellas[i] == null) continue;
                    RectTransform estrellaRt = estrellas[i].rectTransform;
                    estrellaRt.anchoredPosition = direcciones[i] * (18f + 48f * t);
                    float alpha = 0.95f * (1f - t);
                    estrellas[i].color = new Color(1f, 0.82f, 0.38f, alpha);
                    estrellaRt.localScale = Vector3.one * (1f - 0.45f * t);
                }

                yield return null;
            }

            rt.localScale = escalaInicial;
            for (int i = 0; i < estrellas.Length; i++)
                if (estrellas[i] != null) Destroy(estrellas[i].gameObject);

            boton.animating = false;
            boton.onClick?.Invoke();
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
            m_Botones.Clear();

            // El canvas tambien se crea ya con RectTransform, por lo mismo.
            var canvasGo = new GameObject(NombreCanvas, typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            canvasGo.transform.SetParent(transform, false);

            m_Canvas = canvasGo.AddComponent<Canvas>();
            // Screen Space - Camera para que el canvas quede DENTRO del marco de la
            // camara en la Scene view, igual que los niveles, y se pueda editar a
            // mano sin pelearse con un rectangulo gigante fuera de cuadro.
            Camera cam = Camera.main;
            if (cam != null)
            {
                m_Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                m_Canvas.worldCamera = cam;
                m_Canvas.planeDistance = 10f;
            }
            else
            {
                m_Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            m_Canvas.sortingOrder = 100;   // por delante de cualquier otro canvas

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
        }

        /// <summary>
        /// Los paneles de creditos y ajustes se arman siempre en caliente, cuelguen
        /// de un canvas horneado o de uno recien creado.
        /// </summary>
        void ConstruirPaneles()
        {
            if (m_Canvas == null) return;
            m_PanelCreditos = ConstruirCreditos(m_Canvas.transform);
            m_PanelAjustes = ConstruirAjustes(m_Canvas.transform);
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

        // Paleta de los paneles, tomada del arte del menu.
        static readonly Color PanelFondo   = new Color(0.10f, 0.07f, 0.16f, 0.97f);
        static readonly Color PanelBorde   = new Color(0.96f, 0.89f, 0.78f, 1f);
        static readonly Color PanelBarra   = new Color(0.42f, 0.28f, 0.20f, 1f);
        static readonly Color TextoClaro   = new Color(0.96f, 0.91f, 0.82f, 1f);
        static readonly Color TextoSuave   = new Color(0.78f, 0.72f, 0.66f, 1f);
        static readonly Color BotonFondo   = new Color32(0x6B, 0x4A, 0x33, 0xFF);

        static readonly string[] Equipo =
        {
            "Sasha Servat Quezada|Productora / Game Designer",
            "Melanie Pinto Sulca|Artista / Animadora",
            "Andrea Ximena Fontenla Alva|Game Designer / Programadora",
            "Edson Vasquez Solis|Programador",
            "Enzo David Castro Contreras|Programador",
        };

        RectTransform ConstruirCreditos(Transform parent)
        {
            // Tarjeta 980 x 760. La barra de titulo ocupa 76 arriba y el "Cuerpo"
            // queda desplazado 33 px hacia abajo respecto al centro de la tarjeta:
            // por eso las Y de aqui llevan ese +33 ya incorporado.
            var raiz = NuevoPanel(parent, "Panel_Creditos", new Vector2(980f, 760f), "Creditos");
            Transform cuerpo = raiz.transform.Find("Borde/Tarjeta/Cuerpo");

            float y = 273f;
            foreach (string entrada in Equipo)
            {
                string[] partes = entrada.Split('|');

                var nombre = NuevoTextoEn(cuerpo, "Nombre", partes[0], 34, TextAnchor.MiddleCenter,
                    new Vector2(0f, y), new Vector2(880f, 40f));
                nombre.color = TextoClaro;

                var rol = NuevoTextoEn(cuerpo, "Rol", partes[1], 24, TextAnchor.MiddleCenter,
                    new Vector2(0f, y - 34f), new Vector2(880f, 32f));
                rol.color = TextoSuave;

                y -= 86f;
            }

            var estudio = NuevoTextoEn(cuerpo, "Txt_Estudio", "Equipo Chocoflan  -  2026", 24,
                TextAnchor.MiddleCenter, new Vector2(0f, -167f), new Vector2(880f, 32f));
            estudio.color = TextoSuave;

            BotonTexto(raiz, "Volver", new Vector2(0f, -300f), CerrarPaneles, new Vector2(260f, 72f));
            return raiz.GetComponent<RectTransform>();
        }

        RectTransform ConstruirAjustes(Transform parent)
        {
            // Tarjeta 820 x 600, mismo desplazamiento de 33 px del Cuerpo.
            var raiz = NuevoPanel(parent, "Panel_Ajustes", new Vector2(820f, 600f), "Ajustes");
            Transform cuerpo = raiz.transform.Find("Borde/Tarjeta/Cuerpo");

            // --- volumen: etiqueta y debajo  [-]  valor  [+]
            var etqVol = NuevoTextoEn(cuerpo, "Txt_Volumen", "Volumen", 32, TextAnchor.MiddleCenter,
                new Vector2(0f, 183f), new Vector2(700f, 40f));
            etqVol.color = TextoClaro;

            var valor = NuevoTextoEn(cuerpo, "Txt_Valor", TextoVolumen(), 36, TextAnchor.MiddleCenter,
                new Vector2(0f, 113f), new Vector2(200f, 56f));
            valor.color = TextoClaro;

            BotonTexto(raiz, "-", new Vector2(-170f, 80f), () =>
            {
                AudioListener.volume = Mathf.Clamp01(AudioListener.volume - 0.1f);
                valor.text = TextoVolumen();
            }, new Vector2(80f, 60f));

            BotonTexto(raiz, "+", new Vector2(170f, 80f), () =>
            {
                AudioListener.volume = Mathf.Clamp01(AudioListener.volume + 0.1f);
                valor.text = TextoVolumen();
            }, new Vector2(80f, 60f));

            Separador(cuerpo, new Vector2(0f, 53f), 620f);

            // --- pantalla: etiqueta y un boton que alterna
            var etqPant = NuevoTextoEn(cuerpo, "Txt_Pantalla", "Pantalla", 32, TextAnchor.MiddleCenter,
                new Vector2(0f, 3f), new Vector2(700f, 40f));
            etqPant.color = TextoClaro;

            var btnPant = BotonTexto(raiz, TextoPantalla(), new Vector2(0f, -100f), null,
                new Vector2(420f, 64f));
            Text modo = btnPant.GetComponentInChildren<Text>();
            m_Botones[m_Botones.Count - 1].onClick = () =>
            {
                Screen.fullScreen = !Screen.fullScreen;
                if (modo != null) modo.text = TextoPantalla();
            };

            BotonTexto(raiz, "Volver", new Vector2(0f, -210f), CerrarPaneles, new Vector2(260f, 72f));
            return raiz.GetComponent<RectTransform>();
        }

        static string TextoVolumen() => Mathf.RoundToInt(AudioListener.volume * 100f) + " %";
        static string TextoPantalla() => Screen.fullScreen ? "Pantalla completa" : "Ventana";

        /// <summary>Linea fina para separar secciones dentro de un panel.</summary>
        void Separador(Transform parent, Vector2 pos, float ancho)
        {
            var go = NuevoUI(parent, "Separador");
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.12f);
            img.raycastTarget = false;
            Colocar(img.rectTransform, pos, new Vector2(ancho, 2f));
        }

        /// <summary>
        /// Panel de dialogo: oscurece el menu de atras, y encima una tarjeta con
        /// borde claro, barra de titulo y un hijo "Cuerpo" donde va el contenido.
        /// Devuelve la raiz (que ocupa toda la pantalla), no la tarjeta.
        /// </summary>
        GameObject NuevoPanel(Transform parent, string nombre, Vector2 size, string titulo)
        {
            // Capa que cubre la pantalla y apaga el menu de atras.
            var raiz = NuevoUI(parent, nombre);
            var velo = raiz.AddComponent<Image>();
            velo.color = new Color(0.03f, 0.02f, 0.06f, 0.72f);
            velo.raycastTarget = false;
            Estirar(velo.rectTransform);

            // Borde claro = una tarjeta algo mas grande por detras.
            var borde = NuevoUI(raiz.transform, "Borde");
            var bordeImg = borde.AddComponent<Image>();
            bordeImg.color = PanelBorde;
            bordeImg.raycastTarget = false;
            Colocar(bordeImg.rectTransform, Vector2.zero, size + new Vector2(10f, 10f));

            var tarjeta = NuevoUI(borde.transform, "Tarjeta");
            var fondoImg = tarjeta.AddComponent<Image>();
            fondoImg.color = PanelFondo;
            fondoImg.raycastTarget = false;
            Estirar(fondoImg.rectTransform);
            fondoImg.rectTransform.offsetMin = new Vector2(5f, 5f);
            fondoImg.rectTransform.offsetMax = new Vector2(-5f, -5f);

            // Barra de titulo pegada arriba.
            const float altoBarra = 76f;
            var barra = NuevoUI(tarjeta.transform, "Barra");
            var barraImg = barra.AddComponent<Image>();
            barraImg.color = PanelBarra;
            barraImg.raycastTarget = false;
            var brt = barraImg.rectTransform;
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = new Vector2(1f, 1f);
            brt.pivot = new Vector2(0.5f, 1f);
            brt.offsetMin = new Vector2(0f, -altoBarra);
            brt.offsetMax = Vector2.zero;
            brt.sizeDelta = new Vector2(0f, altoBarra);

            var txtTitulo = NuevoTexto(barra.transform, "Txt_Titulo", titulo, 42, TextAnchor.MiddleCenter);
            Estirar(txtTitulo.rectTransform);
            txtTitulo.color = TextoClaro;

            // Cuerpo: todo lo que se coloque dentro queda por debajo del titulo.
            var cuerpo = NuevoUI(tarjeta.transform, "Cuerpo");
            var crt = cuerpo.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(20f, 20f);
            crt.offsetMax = new Vector2(-20f, -altoBarra - 10f);

            return raiz;
        }

        GameObject BotonTexto(GameObject parent, string etiqueta, Vector2 pos, Action onClick,
                              Vector2? size = null)
        {
            Vector2 tam = size ?? new Vector2(300f, 90f);

            // Borde claro, igual que el de la tarjeta, para que los botones del
            // panel se lean como parte del mismo conjunto.
            var go = NuevoUI(parent.transform, "Btn_" + etiqueta);
            var borde = go.AddComponent<Image>();
            borde.color = PanelBorde;
            borde.raycastTarget = false;
            Colocar(borde.rectTransform, pos, tam);

            var relleno = NuevoUI(go.transform, "Fondo");
            var img = relleno.AddComponent<Image>();
            img.color = BotonFondo;
            img.raycastTarget = false;
            Estirar(img.rectTransform);
            img.rectTransform.offsetMin = new Vector2(4f, 4f);
            img.rectTransform.offsetMax = new Vector2(-4f, -4f);

            var txt = NuevoTexto(relleno.transform, "Txt", etiqueta,
                Mathf.RoundToInt(Mathf.Clamp(tam.y * 0.45f, 22f, 40f)), TextAnchor.MiddleCenter);
            Estirar(txt.rectTransform);
            txt.color = TextoClaro;

            m_Botones.Add(new Boton
            {
                rect = borde.rectTransform,
                image = img,
                onClick = onClick,
                baseScale = borde.rectTransform.localScale,
                esDePanel = true
            });
            return go;
        }

        /// <summary>
        /// Crea el GameObject YA con RectTransform. Importante: si se crea con un
        /// Transform normal y despues se le anade un Image, Unity tiene que
        /// convertir el Transform en RectTransform sobre la marcha, que es una
        /// fuente clasica de UI que no aparece.
        /// </summary>
        static GameObject NuevoUI(Transform parent, string nombre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        /// <summary>Texto con posicion y tamano explicitos. Evita los solapes del layout a ojo.</summary>
        Text NuevoTextoEn(Transform parent, string nombre, string contenido, int size,
                          TextAnchor anchor, Vector2 pos, Vector2 tam)
        {
            var txt = NuevoTexto(parent, nombre, contenido, size, anchor);
            Colocar(txt.rectTransform, pos, tam);
            return txt;
        }

        Image NuevaImagen(Transform parent, string nombre, Sprite sprite)
        {
            var go = NuevoUI(parent, nombre);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        Text NuevoTexto(Transform parent, string nombre, string contenido, int size, TextAnchor anchor)
        {
            var go = NuevoUI(parent, nombre);
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
