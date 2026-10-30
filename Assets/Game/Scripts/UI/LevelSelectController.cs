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

        void Enganchar(Transform canvas, string nombre, Action onClick)
        {
            Transform t = canvas.Find(nombre);
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
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Volver();

            var mouse = Mouse.current;
            if (mouse == null) return;

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

                if (dentro && click) t.onClick?.Invoke();
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

            // Velo suave para que las tarjetas resalten sobre el fondo.
            var velo = NuevaImagen(canvasGo.transform, "Velo", null);
            Estirar(velo.rectTransform);
            velo.color = new Color(0.05f, 0.03f, 0.09f, 0.45f);

            // --- titulo
            var titulo = NuevoTextoEn(canvasGo.transform, "Txt_Titulo", "Elige un nivel", 64,
                TextAnchor.MiddleCenter, new Vector2(0f, 390f), new Vector2(1200f, 80f));
            titulo.color = TextoClaro;

            // --- tarjetas, repartidas en fila y centradas
            int n = niveles.Count;
            float paso = tamanoTarjeta.x + separacion;
            float x0 = -(n - 1) * paso * 0.5f;

            for (int i = 0; i < n; i++)
            {
                EntradaNivel nivel = niveles[i];
                string escena = nivel.escena;
                CrearTarjeta(canvasGo.transform, nivel,
                    new Vector2(x0 + i * paso, alturaTarjetas),
                    () => Cargar(escena));
            }

            if (n == 0)
            {
                var aviso = NuevoTextoEn(canvasGo.transform, "Txt_Vacio",
                    "No hay niveles en la lista del Inspector.", 34,
                    TextAnchor.MiddleCenter, Vector2.zero, new Vector2(1200f, 60f));
                aviso.color = TextoClaro;
            }

            // --- volver
            BotonTexto(canvasGo.transform, "Volver", new Vector2(0f, -420f), Volver,
                new Vector2(260f, 72f));
        }

        void CrearTarjeta(Transform parent, EntradaNivel nivel, Vector2 pos, Action onClick)
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
                new Vector2(pos.x, pos.y - tamanoTarjeta.y * 0.5f - 52f),
                new Vector2(tamanoTarjeta.x, 56f));
            etiqueta.color = TextoClaro;

            m_Tarjetas.Add(new Tarjeta
            {
                rect = bordeImg.rectTransform,
                borde = bordeImg,
                onClick = onClick,
                baseScale = bordeImg.rectTransform.localScale
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
