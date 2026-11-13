using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Video de introduccion. Se reproduce UNA sola vez: la primera vez que
    /// alguien abre el juego. Despues, esta escena pasa de largo al menu sin que
    /// se note.
    ///
    /// COMO SE CAMBIA EL VIDEO
    /// -----------------------
    /// El archivo vive en Assets/StreamingAssets/introKira.mp4. Para cambiarlo,
    /// se reemplaza ese archivo por otro con el mismo nombre, o se escribe el
    /// nombre nuevo en el campo "Archivo" del Inspector. No hay que tocar codigo.
    ///
    /// POR QUE NO SE USA UN VideoClip
    /// ------------------------------
    /// En WebGL (que es como se publica en itch) Unity NO soporta los VideoClip
    /// importados: hay que servir el archivo por URL. Por eso el mp4 va en
    /// StreamingAssets, que se copia tal cual a la build, y se reproduce desde
    /// ahi. Asi funciona igual en el editor y en el navegador.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class IntroVideo : MonoBehaviour
    {
        [Header("Video")]
        [Tooltip("Nombre del archivo dentro de Assets/StreamingAssets.")]
        public string archivo = "introKira.mp4";

        [Header("Que hacer al terminar")]
        [Tooltip("Escena que se carga cuando acaba el video o se salta.")]
        public string escenaSiguiente = "MainMenu";

        [Header("Solo la primera vez")]
        [Tooltip("Activado: el video sale solo la primera vez que se abre el juego. " +
                 "Desactivado: sale siempre (util para probarlo).")]
        public bool soloLaPrimeraVez = true;

        [Tooltip("Donde se recuerda que ya se vio. Cambiarlo hace que vuelva a salir para todos.")]
        public string claveGuardado = "Chocoflan.IntroVista";

        [Header("Saltar")]
        [Tooltip("Se puede saltar con clic o con Escape.")]
        public bool sePuedeSaltar = true;

        [Tooltip("Segundos antes de permitir saltarlo, para que no se salte de un clic suelto.")]
        public float esperaAntesDeSaltar = 0.6f;

        [Tooltip("Texto pequeno abajo. Vacio = no se muestra.")]
        public string aviso = "clic para saltar";

        [Header("Seguridad")]
        [Tooltip("Si el video no arranca (archivo que falta, navegador que lo bloquea), " +
                 "pasa al menu despues de estos segundos en vez de quedarse en negro.")]
        public float segundosDeRescate = 20f;

        VideoPlayer m_Video;
        bool m_Saliendo;
        float m_T;
        bool m_Arranco;
        GUIStyle m_Estilo;

        /// <summary>True si el video ya se vio alguna vez en este dispositivo.</summary>
        public bool YaSeVio => PlayerPrefs.GetInt(claveGuardado, 0) == 1;

        void Awake()
        {
            m_Video = GetComponent<VideoPlayer>();

            // Si ya se vio, ni siquiera se prepara: se salta la escena de una.
            if (soloLaPrimeraVez && YaSeVio)
            {
                m_Saliendo = true;
                Cargar();
                return;
            }

            Configurar();
        }

        void Configurar()
        {
            m_Video.source = VideoSource.Url;
            m_Video.url = System.IO.Path.Combine(Application.streamingAssetsPath, archivo);

            // Se dibuja directamente sobre la camara: no hace falta RenderTexture
            // ni Canvas, y se adapta solo a cualquier resolucion.
            m_Video.renderMode = VideoRenderMode.CameraNearPlane;
            m_Video.targetCamera = Camera.main != null
                ? Camera.main
                : FindFirstObjectByType<Camera>();
            m_Video.aspectRatio = VideoAspectRatio.FitInside;
            m_Video.targetCameraAlpha = 1f;

            // Direct: es el unico modo de audio que funciona en WebGL.
            m_Video.audioOutputMode = VideoAudioOutputMode.Direct;

            m_Video.isLooping = false;
            m_Video.playOnAwake = false;
            m_Video.waitForFirstFrame = true;

            m_Video.errorReceived += OnError;
            m_Video.loopPointReached += OnFin;
            m_Video.prepareCompleted += OnListo;
            m_Video.Prepare();
        }

        void OnDestroy()
        {
            if (m_Video == null) return;
            m_Video.errorReceived -= OnError;
            m_Video.loopPointReached -= OnFin;
            m_Video.prepareCompleted -= OnListo;
        }

        void OnListo(VideoPlayer _)
        {
            m_Arranco = true;
            m_Video.Play();
        }

        void OnFin(VideoPlayer _) => Terminar();

        void OnError(VideoPlayer _, string mensaje)
        {
            // Mejor ir al menu que dejar al jugador mirando una pantalla negra.
            Debug.LogWarning("[Intro] No se pudo reproducir el video: " + mensaje, this);
            Terminar();
        }

        void Update()
        {
            if (m_Saliendo) return;
            m_T += Time.unscaledDeltaTime;

            if (sePuedeSaltar && m_T >= esperaAntesDeSaltar)
            {
                var mouse = Mouse.current;
                var kb = Keyboard.current;
                bool clic = mouse != null && mouse.leftButton.wasPressedThisFrame;
                bool escape = kb != null && (kb.escapeKey.wasPressedThisFrame ||
                                             kb.spaceKey.wasPressedThisFrame ||
                                             kb.enterKey.wasPressedThisFrame);
                if (clic || escape) { Terminar(); return; }
            }

            // Red de seguridad: si nunca llego a arrancar, no se queda colgado.
            if (!m_Arranco && segundosDeRescate > 0f && m_T >= segundosDeRescate)
                Terminar();
        }

        /// <summary>Marca el video como visto y pasa al menu.</summary>
        public void Terminar()
        {
            if (m_Saliendo) return;
            m_Saliendo = true;

            PlayerPrefs.SetInt(claveGuardado, 1);
            PlayerPrefs.Save();

            if (m_Video != null && m_Video.isPlaying) m_Video.Stop();
            Cargar();
        }

        void Cargar()
        {
            if (string.IsNullOrEmpty(escenaSiguiente)) return;

            if (!Application.CanStreamedLevelBeLoaded(escenaSiguiente))
            {
                Debug.LogError("[Intro] La escena '" + escenaSiguiente + "' no esta en Build Settings. " +
                               "Menu Chocoflan > 1 - Aplicar ajustes de proyecto.", this);
                return;
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene(escenaSiguiente);
        }

        void OnGUI()
        {
            if (m_Saliendo || !sePuedeSaltar || string.IsNullOrEmpty(aviso)) return;
            if (m_T < esperaAntesDeSaltar) return;

            if (m_Estilo == null)
            {
                m_Estilo = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.LowerCenter,
                    fontSize = Mathf.Max(12, Screen.height / 45)
                };
            }

            // Late suave, para que se note sin molestar.
            float a = 0.45f + 0.35f * Mathf.Sin(m_T * 2.2f);
            m_Estilo.normal.textColor = new Color(1f, 0.97f, 0.9f, a);

            var r = new Rect(0f, Screen.height - Screen.height * 0.12f,
                             Screen.width, Screen.height * 0.08f);
            GUI.Label(r, aviso, m_Estilo);
        }
    }
}
