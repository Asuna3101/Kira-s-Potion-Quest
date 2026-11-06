using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>Los efectos del juego. El nombre coincide con el del archivo .wav.</summary>
    public enum Sonido
    {
        Ninguno,
        Romper,      // snd_romper   - el ingrediente se rompe (pinchos o caida)
        Quemar,      // snd_fire     - el ingrediente se quema
        Caldero,     // snd_caldero  - el ingrediente entra al caldero
        Ganar,       // snd_win      - cartel de nivel superado
        Perder,      // snd_perder   - cartel de nivel fallido
        Boton,       // snd_boton    - click en una opcion
        Lanzar,      // snd_lanzar   - se lanza un ingrediente
        EmpezarNivel // snd_startlevel
    }

    /// <summary>
    /// Centraliza la musica y los efectos.
    ///
    /// Se pone UNO por escena, en el objeto que ya lleva el resto de controladores
    /// (--- GAME --- o --- MENU ---), con los clips asignados en el Inspector.
    /// Desde cualquier sitio se reproduce con una linea:
    ///
    ///     AudioDirector.Reproducir(Sonido.Romper);
    ///
    /// Es seguro llamarlo aunque la escena no tenga director: simplemente no suena,
    /// asi que ningun script se rompe por eso.
    ///
    /// Los efectos usan ignoreListenerPause para que el cartel de victoria o
    /// derrota se oiga aunque el juego este congelado con timeScale = 0.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioDirector : MonoBehaviour
    {
        public static AudioDirector Instancia { get; private set; }

        [Header("Musica (en bucle)")]
        [Tooltip("La que suena en esta escena. Menu: snd_backgroundmusicmenu. Niveles: snd_backgroundmusiclevel.")]
        public AudioClip musica;

        [Range(0f, 1f)] public float volumenMusica = 0.45f;

        [Header("Efectos")]
        [Range(0f, 1f)] public float volumenEfectos = 0.85f;

        public AudioClip romper;
        public AudioClip quemar;
        public AudioClip caldero;
        public AudioClip ganar;
        public AudioClip perder;
        public AudioClip boton;
        public AudioClip lanzar;
        public AudioClip empezarNivel;

        [Header("Al empezar")]
        [Tooltip("Reproduce el sonido de inicio de nivel en cuanto arranca la escena.")]
        public bool sonarAlEmpezar = true;

        [Tooltip("Segundos que tarda la musica en subir de volumen al entrar.")]
        public float fundidoEntrada = 0.6f;

        AudioSource m_Musica;
        AudioSource m_Efectos;
        float m_Fundido;

        void Awake()
        {
            Instancia = this;

            m_Musica = gameObject.AddComponent<AudioSource>();
            m_Musica.clip = musica;
            m_Musica.loop = true;
            m_Musica.playOnAwake = false;
            m_Musica.volume = 0f;
            m_Musica.spatialBlend = 0f;          // 2D: se oye igual en toda la pantalla
            m_Musica.ignoreListenerPause = true;

            m_Efectos = gameObject.AddComponent<AudioSource>();
            m_Efectos.playOnAwake = false;
            m_Efectos.spatialBlend = 0f;
            m_Efectos.ignoreListenerPause = true;
        }

        void Start()
        {
            if (musica != null) m_Musica.Play();
            if (sonarAlEmpezar) Reproducir(Sonido.EmpezarNivel);
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        void Update()
        {
            // Tiempo real: la musica no se corta al pausar el juego.
            if (m_Musica == null || musica == null) return;
            if (m_Fundido >= 1f) return;

            m_Fundido = fundidoEntrada <= 0f
                ? 1f
                : Mathf.Clamp01(m_Fundido + Time.unscaledDeltaTime / fundidoEntrada);
            m_Musica.volume = volumenMusica * m_Fundido;
        }

        /// <summary>Reproduce un efecto. Seguro de llamar aunque no haya director en la escena.</summary>
        public static void Reproducir(Sonido sonido)
        {
            var d = Instancia;
            if (d == null) return;
            d.Sonar(d.ClipDe(sonido));
        }

        /// <summary>Reproduce un clip suelto, por si algo necesita uno propio.</summary>
        public static void ReproducirClip(AudioClip clip, float volumen = 1f)
        {
            var d = Instancia;
            if (d == null || clip == null) return;
            d.m_Efectos.PlayOneShot(clip, d.volumenEfectos * volumen);
        }

        void Sonar(AudioClip clip)
        {
            if (clip == null || m_Efectos == null) return;
            m_Efectos.PlayOneShot(clip, volumenEfectos);
        }

        AudioClip ClipDe(Sonido sonido)
        {
            switch (sonido)
            {
                case Sonido.Romper: return romper;
                case Sonido.Quemar: return quemar;
                case Sonido.Caldero: return caldero;
                case Sonido.Ganar: return ganar;
                case Sonido.Perder: return perder;
                case Sonido.Boton: return boton;
                case Sonido.Lanzar: return lanzar;
                case Sonido.EmpezarNivel: return empezarNivel;
                default: return null;
            }
        }
    }
}
