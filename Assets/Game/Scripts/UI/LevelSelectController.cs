using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Una tarjeta de la pantalla de seleccion: un nivel.
    /// Para anadir un nivel nuevo basta con sumar un elemento a la lista del
    /// Inspector; la pantalla los reparte sola.
    /// </summary>
    [Serializable]
    public class EntradaNivel
    {
        [Tooltip("Lo que se lee debajo de la vista previa.")]
        public string nombre = "Nivel";

        [Tooltip("Escena que carga al pulsarlo. Tiene que estar en Build Settings.")]
        public string escena = "Level01";

        [Tooltip("Imagen de la vista previa (Sprites/UI/Preview_*.png).")]
        public Sprite vistaPrevia;
    }

    /// <summary>
    /// Pantalla de seleccion de nivel.
    ///
    /// Prototipo: mismo fondo que el menu, las vistas previas en marcos y al
    /// pulsar una se carga ese nivel. Sin EventSystem, igual que el resto del
    /// proyecto: lee Mouse.current y hace hit-test contra cada tarjeta.
    /// </summary>
    public class LevelSelectController : MonoBehaviour
    {
        [Header("Arte")]
        public Sprite fondo;

        [Tooltip("Arte del boton Volver (Sprites/UI/boton_volver.png). Vacio = rectangulo con el texto.")]
        public Sprite spriteBotonVolver;

        [Header("Pergamino")]
        [Tooltip("Background/pergaminoCerrado.PNG. El rollo cerrado que se ve al entrar.")]
        public Sprite pergaminoCerrado;

        [Tooltip("Background/pergamino.PNG. El pergamino desplegado, ya trae escrito 'Elige un nivel'.")]
        public Sprite pergaminoAbierto;

        [Tooltip("Ancho del pergamino abierto en pixeles de canvas. Lo demas se calcula de ahi.")]
        public float anchoPergamino = 1560f;

        [Tooltip("Desplazamiento del pergamino dentro de la pantalla.")]
        public Vector2 posicionPergamino = new Vector2(0f, 40f);

        [Header("Pergamino - tiempos (segundos)")]
        [Tooltip("El rollo cerrado entra creciendo.")]
        public float entrada = 0.28f;

        [Tooltip("Cuanto se queda CERRADO antes de desplegarse.")]
        public float pausaCerrado = 0.35f;

        [Tooltip("Lo que tarda en desplegarse a lo ancho.")]
        public float apertura = 0.55f;

        [Tooltip("Lo que tardan en aparecer las tarjetas, una tras otra.")]
        public float aparicionTarjetas = 0.45f;

        [Tooltip("Apagado: el pergamino sale ya abierto, sin animacion.")]
        public bool animarApertura = true;

        [Header("Zona util del papel")]
        [Tooltip("Parte plana del pergamino donde caben las tarjetas, en fraccion del dibujo " +
                 "(x, y, ancho, alto). Por defecto deja fuera el rollo de la derecha y el titulo de arriba.")]
        public Rect zonaUtil = new Rect(0.05f, 0.21f, 0.74f, 0.58f);

        [Tooltip("Empujoncito de las tarjetas dentro de la zona, en pixeles de canvas. " +
                 "Y positivo las sube. Para afinar sin tocar la zona.")]
        public Vector2 ajusteTarjetas = Vector2.zero;

        [Header("Niveles")]
        public List<EntradaNivel> niveles = new List<EntradaNivel>();

        [Header("Navegacion")]
        public string escenaDelMenu = "MainMenu";

        [Header("Canvas")]
        public int canvasWidth = GridSettings.ReferenceWidthPx;
        public int canvasHeight = GridSettings.ReferenceHeightPx;

        [Header("Tarjetas")]
        public Vector2 tamanoTarjeta = new Vector2(760f, 428f);
        public float separacion = 80f;
        public float alturaTarjetas = 30f;
        public float hoverScale = 1.04f;
        public float hoverLerp = 12f;

        public const string NombreCanvas = "Select_Canvas";

        static readonly Color Borde = new Color(0.96f, 0.89f, 0.78f, 1f);
        static readonly Color BordeHover = new Color(1f, 0.84f, 0.42f, 1f);
        static readonly Color Fondo = new Color(0.10f, 0.07f, 0.16f, 0.97f);
        static readonly Color TextoClaro = new Color(0.96f, 0.91f, 0.82f, 1f);
        // Sobre el pergamino el texto claro no se lee: va tinta oscura.
        static readonly Color TextoOscuro = new Color(0.28f, 0.17f, 0.09f, 1f);
        static readonly Color BotonFondo = new Color32(0x6B, 0x4A, 0x33, 0xFF);

        class Tarjeta
        {
            public RectTransform rect;
            public Image borde;
            public Action onClick;
            public Vector3 baseScale;
        }

        readonly List<Tarjeta> m_Tarjetas = new List<Tarjeta>();
        Canvas m_Canvas;
        Font m_Font;

        Image m_Pergamino;
        RectTransform m_PergaminoRect;
        CanvasGroup m_PergaminoGrupo;
        RectTransform m_ZonaRect;
        CanvasGroup m_ZonaGrupo;
        float m_T = -1f;

        Camera CamaraDelCanvas =>
            m_Canvas == null || m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : m_Canvas.worldCamera;

        void Start()
        {
            m_Font = CargarFuente();

            // Si la pantalla ya esta HORNEADA en la escena (se ve en la Hierarchy),
            // se reutiliza tal cual y solo se le enganchan las acciones. Asi lo que
            // se ve en el editor es exactamente lo que se juega.
            Canvas horneado = BuscarCanvasHorneado();
            if (horneado != null) EngancharCanvasHorneado(horneado);
            else Construir();

            PrepararApertura();
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

        void EngancharCanvasHorneado(Canvas canvas)
        {
            m_Canvas = canvas;
            m_Tarjetas.Clear();

            foreach (EntradaNivel nivel in niveles)
            {
                string escena = nivel.escena;
                Enganchar(canvas.transform, "Tarjeta_" + nivel.nombre, () => Cargar(escena));
            }
            Enganchar(canvas.transform, "Btn_Volver", Volver);

            // Las fuentes de los Text horneados pueden venir vacias segun la
            // version de Unity; se rellenan para que no salgan invisibles.
            foreach (var t in canvas.GetComponentsInChildren<Text>(true))
                if (t.font == null) t.font = m_Font;
        }

        /// <summary>
        /// Busca por nombre en TODA la rama, no solo entre los hijos directos.
        ///
        /// Hace falta porque desde que hay pergamino las tarjetas cuelgan de
        /// Select_Canvas/Pergamino/Zona_Niveles. Con Transform.Find a secas no
        /// aparecian, la lista de tarjetas quedaba vacia y no se podia pulsar nada.
        /// </summary>
        static Transform BuscarEnLaRama(Transform raiz, string nombre)
        {
            if (raiz.name == nombre) return raiz;
            for (int i = 0; i < raiz.childCount; i++)
            {
                Transform hallado = BuscarEnLaRama(raiz.GetChild(i), nombre);
                if (hallado != null) return hallado;
            }
            return null;
        }

        void Enganchar(Transform canvas, string nombre, Action onClick)
        {
            Transform t = BuscarEnLaRama(canvas, nombre);
            if (t == null)
            {
                Debug.LogWarning("[Seleccion] No encontre '" + nombre + "' dentro de " + NombreCanvas +
                                 ". Revisa que el nombre del nivel coincida con el de la tarjeta.", this);
                return;
            }

            var rect = t as RectTransform;
            if (rect == null) return;

            m_Tarjetas.Add(new Tarjeta
            {
                rect = rect,
                borde = t.GetComponent<Image>(),
                onClick = onClick,
                baseScale = rect.localScale
            });
        }

        void Update()
        {
            AnimarPergamino();

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Volver();

            var mouse = Mouse.current;
            if (mouse == null) return;

            // Mientras el pergamino se despliega no se puede pulsar nada: si no,
            // un clic de mas en el menu entraria a un nivel sin querer.
            if (m_T >= 0f && m_T < entrada + pausaCerrado + apertura) return;

            Vector2 screen = mouse.position.ReadValue();
            bool click = mouse.leftButton.wasPressedThisFrame;

            foreach (var t in m_Tarjetas)
            {
                bool dentro = t.rect.gameObject.activeInHierarchy &&
                              RectTransformUtility.RectangleContainsScreenPoint(t.rect, screen, CamaraDelCanvas);

                t.rect.localScale = Vector3.Lerp(t.rect.localScale,
                    t.baseScale * (dentro ? hoverScale : 1f),
                    1f - Mathf.Exp(-hoverLerp * Time.unscaledDeltaTime));

                if (t.borde != null) t.borde.color = dentro ? BordeHover : Borde;

                if (dentro && click)
                {
                    AudioDirector.Reproducir(Sonido.Boton);
                    t.onClick?.Invoke();
                }
            }
        }

        public void Volver() => Cargar(escenaDelMenu);

        void Cargar(string escena)
        {
            if (string.IsNullOrEmpty(escena)) return;
            if (!Application.CanStreamedLevelBeLoaded(escena))
            {
                Debug.LogError("[Seleccion] La escena '" + escena + "' no esta en Build Settings. " +
                               "Menu Chocoflan > 1 - Aplicar ajustes de proyecto.", this);
                return;
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene(escena);
        }

        // ------------------------------------------------------ construccion

        void Construir()
        {
            m_Tarjetas.Clear();

            var canvasGo = new GameObject(NombreCanvas, typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            canvasGo.transform.SetParent(transform, false);

            m_Canvas = canvasGo.AddComponent<Canvas>();
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
            m_Canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(canvasWidth, canvasHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            // --- fondo, el mismo del menu
            var bg = NuevaImagen(canvasGo.transform, "Fondo", fondo);
            Estirar(bg.rectTransform);
            if (fondo == null) bg.color = new Color32(0x1E, 0x1A, 0x33, 0xFF);

            // Velo suave para que el pergamino resalte sobre el fondo.
            var velo = NuevaImagen(canvasGo.transform, "Velo", null);
            Estirar(velo.rectTransform);
            velo.color = new Color(0.05f, 0.03f, 0.09f, 0.45f);

            // --- pergamino: el papel sobre el que van las tarjetas
            Transform padreTarjetas = canvasGo.transform;
            if (pergaminoAbierto != null)
            {
                padreTarjetas = CrearPergamino(canvasGo.transform).transform;
            }
            else
            {
                // Sin arte: el titulo escrito por codigo, como antes.
                var titulo = NuevoTextoEn(canvasGo.transform, "Txt_Titulo", "Elige un nivel", 64,
                    TextAnchor.MiddleCenter, new Vector2(0f, 390f), new Vector2(1200f, 80f));
                titulo.color = TextoClaro;
            }

            // --- tarjetas
            // Si la lista esta vacia, el aviso lo pone CrearTarjetas.
            CrearTarjetas(padreTarjetas);

            // --- volver
            if (spriteBotonVolver != null)
                BotonArte(canvasGo.transform, "Volver", new Vector2(0f, -420f), Volver,
                    spriteBotonVolver, 100f);
            else
                BotonTexto(canvasGo.transform, "Volver", new Vector2(0f, -420f), Volver,
                    new Vector2(260f, 72f));
        }

        /// <summary>
        /// Despliega el pergamino en tres tiempos: el rollo cerrado entra
        /// creciendo, se queda un momento, y se abre a lo ancho. Las tarjetas
        /// aparecen despues, ya con el papel extendido.
        ///
        /// Usa tiempo real, asi que funciona aunque el juego este en pausa.
        /// </summary>
        /// <summary>
        /// Localiza las piezas del pergamino (ya esten horneadas en la escena o
        /// recien creadas) y arranca la animacion de apertura.
        ///
        /// El ancho final se toma del propio objeto, no del campo: asi, si alguien
        /// reescala el pergamino a mano en la escena, la animacion respeta su tamano.
        /// </summary>
        void PrepararApertura()
        {
            if (m_Canvas == null) return;

            Transform papel = m_Canvas.transform.Find("Pergamino");
            if (papel == null) return;

            m_Pergamino = papel.GetComponent<Image>();
            m_PergaminoRect = papel as RectTransform;
            m_PergaminoGrupo = papel.GetComponent<CanvasGroup>();
            if (m_PergaminoGrupo == null) m_PergaminoGrupo = papel.gameObject.AddComponent<CanvasGroup>();

            Transform zona = papel.Find("Zona_Niveles");
            if (zona != null)
            {
                m_ZonaRect = zona as RectTransform;
                m_ZonaGrupo = zona.GetComponent<CanvasGroup>();
                if (m_ZonaGrupo == null) m_ZonaGrupo = zona.gameObject.AddComponent<CanvasGroup>();
            }

            if (m_PergaminoRect != null) anchoPergamino = m_PergaminoRect.sizeDelta.x;

            if (!animarApertura || m_Pergamino == null)
            {
                m_T = -1f;
                return;
            }

            if (pergaminoCerrado != null) m_Pergamino.sprite = pergaminoCerrado;
            m_PergaminoGrupo.alpha = 0f;
            if (m_ZonaGrupo != null) m_ZonaGrupo.alpha = 0f;
            m_T = 0f;
        }

        void AnimarPergamino()
        {
            if (m_T < 0f || m_PergaminoRect == null) return;

            m_T += Time.unscaledDeltaTime;

            float alto = m_PergaminoRect.sizeDelta.y;
            float anchoCerrado = pergaminoCerrado != null
                ? alto * (pergaminoCerrado.rect.width / pergaminoCerrado.rect.height)
                : anchoPergamino * 0.2f;

            // 1. el rollo entra
            float e = entrada <= 0f ? 1f : Mathf.Clamp01(m_T / entrada);
            if (m_PergaminoGrupo != null) m_PergaminoGrupo.alpha = e;
            float pop = 1f - Mathf.Pow(1f - e, 3f);

            // 2. se despliega
            float a = apertura <= 0f ? 1f : Mathf.Clamp01((m_T - entrada - pausaCerrado) / apertura);
            bool abierto = a > 0f;
            if (abierto && pergaminoAbierto != null && m_Pergamino.sprite != pergaminoAbierto)
                m_Pergamino.sprite = pergaminoAbierto;

            float ancho = abierto
                ? Mathf.Lerp(anchoCerrado, anchoPergamino, Suave(a))
                : anchoCerrado;
            m_PergaminoRect.sizeDelta = new Vector2(ancho, alto);
            m_PergaminoRect.localScale = new Vector3(pop, pop, 1f);

            // La zona de tarjetas mantiene su tamano final mientras el papel crece,
            // para que las vistas previas no salgan aplastadas.
            if (m_ZonaRect != null)
            {
                m_ZonaRect.anchorMin = m_ZonaRect.anchorMax = new Vector2(0.5f, 0.5f);
                m_ZonaRect.sizeDelta = new Vector2(anchoPergamino, alto);
                m_ZonaRect.anchoredPosition = Vector2.zero;
                m_ZonaRect.localScale = Vector3.one;
            }

            // 3. aparecen las tarjetas
            float c = aparicionTarjetas <= 0f
                ? 1f
                : Mathf.Clamp01((m_T - entrada - pausaCerrado - apertura) / aparicionTarjetas);
            if (m_ZonaGrupo != null) m_ZonaGrupo.alpha = c;

            if (c >= 1f) m_T = -1f;   // listo, no hay mas que animar
        }

        static float Suave(float x) => x * x * (3f - 2f * x);

        /// <summary>
        /// El pergamino: un rollo cerrado que se despliega. Devuelve el objeto
        /// sobre el que van las tarjetas, para que viajen con el papel.
        /// </summary>
        GameObject CrearPergamino(Transform canvas)
        {
            float alto = anchoPergamino * (pergaminoAbierto.rect.height / pergaminoAbierto.rect.width);

            var go = NuevoUI(canvas, "Pergamino");
            m_Pergamino = go.AddComponent<Image>();
            m_Pergamino.sprite = pergaminoAbierto;
            m_Pergamino.raycastTarget = false;
            m_Pergamino.preserveAspect = false;
            m_PergaminoRect = m_Pergamino.rectTransform;
            Colocar(m_PergaminoRect, posicionPergamino, new Vector2(anchoPergamino, alto));

            // Las tarjetas cuelgan de aqui: asi el papel puede crecer y encogerse
            // sin deformarlas, porque este objeto se queda siempre del tamano final.
            var zona = NuevoUI(go.transform, "Zona_Niveles");
            m_ZonaRect = zona.GetComponent<RectTransform>();
            Estirar(m_ZonaRect);
            m_ZonaGrupo = zona.AddComponent<CanvasGroup>();
            m_PergaminoGrupo = go.AddComponent<CanvasGroup>();

            // Se deja TODO visible: la animacion la arranca PrepararApertura() al
            // empezar la partida. Si se horneara con alpha 0, en el editor no se
            // veria nada y en una escena horneada se quedaria invisible para siempre.
            m_ZonaGrupo.alpha = 1f;
            m_PergaminoGrupo.alpha = 1f;
            return zona;
        }

        /// <summary>
        /// Reparte las tarjetas sobre la parte plana del papel: tres arriba y las
        /// que sobren abajo, centradas, como en el boceto.
        /// </summary>
        void CrearTarjetas(Transform parent)
        {
            int n = niveles.Count;
            if (n == 0)
            {
                var aviso = NuevoTextoEn(parent, "Txt_Vacio",
                    "No hay niveles en la lista del Inspector.", 34,
                    TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1200f, 60f));
                aviso.color = TextoOscuro;
                return;
            }

            // Zona util en pixeles de canvas, medida sobre el pergamino.
            bool sobrePapel = m_PergaminoRect != null;
            Vector2 papel = sobrePapel ? m_PergaminoRect.sizeDelta : new Vector2(canvasWidth, canvasHeight);
            float zx = (zonaUtil.x + zonaUtil.width * 0.5f - 0.5f) * papel.x;
            float zy = (0.5f - (zonaUtil.y + zonaUtil.height * 0.5f)) * papel.y;
            Vector2 zonaTam = new Vector2(zonaUtil.width * papel.x, zonaUtil.height * papel.y);

            const int porFila = 3;
            int filas = Mathf.CeilToInt(n / (float)porFila);
            float hueco = Mathf.Min(44f, zonaTam.x * 0.04f);

            float anchoT = (zonaTam.x - hueco * (porFila - 1)) / porFila;
            float altoFila = (zonaTam.y - hueco * (filas - 1)) / filas;
            // Hueco para el nombre bajo cada tarjeta.
            float altoT = Mathf.Min(anchoT * 9f / 16f, altoFila - 56f);
            Vector2 tam = new Vector2(anchoT - 14f, altoT);

            for (int i = 0; i < n; i++)
            {
                int fila = i / porFila;
                int col = i % porFila;
                int enEstaFila = Mathf.Min(porFila, n - fila * porFila);

                float x = zx + (col - (enEstaFila - 1) * 0.5f) * (anchoT + hueco);
                float y = zy + ((filas - 1) * 0.5f - fila) * (altoFila + hueco);
                x += ajusteTarjetas.x;
                y += ajusteTarjetas.y;

                EntradaNivel nivel = niveles[i];
                string escena = nivel.escena;
                CrearTarjeta(parent, nivel, new Vector2(x, y), () => Cargar(escena), tam);
            }
        }

        void CrearTarjeta(Transform parent, EntradaNivel nivel, Vector2 pos, Action onClick)
            => CrearTarjeta(parent, nivel, pos, onClick, tamanoTarjeta);

        void CrearTarjeta(Transform parent, EntradaNivel nivel, Vector2 pos, Action onClick, Vector2 tamanoTarjeta)
        {
            // Marco claro, y dentro la vista previa.
            var marco = NuevoUI(parent, "Tarjeta_" + nivel.nombre);
            var bordeImg = marco.AddComponent<Image>();
            bordeImg.color = Borde;
            bordeImg.raycastTarget = false;
            Colocar(bordeImg.rectTransform, pos, tamanoTarjeta + new Vector2(14f, 14f));

            var interior = NuevoUI(marco.transform, "Interior");
            var interiorImg = interior.AddComponent<Image>();
            interiorImg.color = Fondo;
            interiorImg.raycastTarget = false;
            Estirar(interiorImg.rectTransform);
            interiorImg.rectTransform.offsetMin = new Vector2(7f, 7f);
            interiorImg.rectTransform.offsetMax = new Vector2(-7f, -7f);

            var vista = NuevaImagen(interior.transform, "VistaPrevia", nivel.vistaPrevia);
            Estirar(vista.rectTransform);
            if (nivel.vistaPrevia == null)
            {
                vista.color = new Color(0.18f, 0.14f, 0.26f, 1f);
                var falta = NuevoTextoEn(vista.transform, "Txt_Falta", "sin vista previa", 28,
                    TextAnchor.MiddleCenter, Vector2.zero, tamanoTarjeta);
                falta.color = TextoClaro;
            }

            // Nombre bajo la tarjeta.
            var etiqueta = NuevoTextoEn(parent, "Txt_" + nivel.nombre, nivel.nombre, 40,
                TextAnchor.MiddleCenter,
                new Vector2(pos.x, pos.y - tamanoTarjeta.y * 0.5f - 40f),
                new Vector2(tamanoTarjeta.x, 56f));
            etiqueta.color = pergaminoAbierto != null ? TextoOscuro : TextoClaro;

            m_Tarjetas.Add(new Tarjeta
            {
                rect = bordeImg.rectTransform,
                borde = bordeImg,
                onClick = onClick,
                baseScale = bordeImg.rectTransform.localScale
            });
        }

        /// <summary>
        /// Boton hecho con el dibujo de Melanie: una sola imagen, sin marco ni
        /// texto de codigo. El ancho sale del propio PNG, asi que si lo reexporta
        /// con otras proporciones no hay que tocar nada aqui.
        /// </summary>
        void BotonArte(Transform parent, string etiqueta, Vector2 pos, Action onClick,
                       Sprite sprite, float alto)
        {
            var go = NuevoUI(parent, "Btn_" + etiqueta);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            float aspecto = sprite.rect.height > 0f ? sprite.rect.width / sprite.rect.height : 2.8f;
            Colocar(img.rectTransform, pos, new Vector2(alto * aspecto, alto));

            m_Tarjetas.Add(new Tarjeta
            {
                rect = img.rectTransform,
                borde = null,            // sin marco: el hover solo crece
                onClick = onClick,
                baseScale = img.rectTransform.localScale
            });
        }

        void BotonTexto(Transform parent, string etiqueta, Vector2 pos, Action onClick, Vector2 tam)
        {
            var go = NuevoUI(parent, "Btn_" + etiqueta);
            var borde = go.AddComponent<Image>();
            borde.color = Borde;
            borde.raycastTarget = false;
            Colocar(borde.rectTransform, pos, tam);

            var relleno = NuevoUI(go.transform, "Fondo");
            var img = relleno.AddComponent<Image>();
            img.color = BotonFondo;
            img.raycastTarget = false;
            Estirar(img.rectTransform);
            img.rectTransform.offsetMin = new Vector2(4f, 4f);
            img.rectTransform.offsetMax = new Vector2(-4f, -4f);

            var txt = NuevoTextoEn(relleno.transform, "Txt", etiqueta, 32, TextAnchor.MiddleCenter,
                Vector2.zero, tam);
            Estirar(txt.rectTransform);
            txt.color = TextoClaro;

            m_Tarjetas.Add(new Tarjeta
            {
                rect = borde.rectTransform,
                borde = borde,
                onClick = onClick,
                baseScale = borde.rectTransform.localScale
            });
        }

        // ------------------------------------------------------- utilidades

        static GameObject NuevoUI(Transform parent, string nombre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        Image NuevaImagen(Transform parent, string nombre, Sprite sprite)
        {
            var go = NuevoUI(parent, nombre);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        Text NuevoTextoEn(Transform parent, string nombre, string contenido, int size,
                          TextAnchor anchor, Vector2 pos, Vector2 tam)
        {
            var go = NuevoUI(parent, nombre);
            var txt = go.AddComponent<Text>();
            txt.font = m_Font;
            txt.fontSize = size;
            txt.alignment = anchor;
            txt.text = contenido;
            txt.color = TextoClaro;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            Colocar(txt.rectTransform, pos, tam);
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
