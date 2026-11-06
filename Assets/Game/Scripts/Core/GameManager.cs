using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Chocoflan.KiraPotionQuest
{
    public enum LevelState { Playing, Won, Lost }

    /// <summary>
    /// Reglas del nivel (GDD):
    ///   VICTORIA : llevar TODOS los ingredientes al caldero.
    ///   DERROTA  : un ingrediente es destruido por un obstaculo.
    /// Tambien lleva el mana de levitacion y dispara los eventos que consume el HUD.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Maná de levitación")]
        public float maxMana = Level01Data.MaxMana;
        public float manaDrainPerSecond = Level01Data.ManaDrainPerSecond;
        public float manaRegenPerSecond = Level01Data.ManaRegenPerSecond;

        [Header("Referencias")]
        [Tooltip("Builder del nivel. Si esta asignado, R reconstruye el tablero sin recargar la escena.")]
        public LevelBuilder builder;

        [Header("Al superar el nivel")]
        [Tooltip("Escena que se carga al ganar. Vacio = se queda en la pantalla de victoria.")]
        public string escenaSiguiente = "";

        [Tooltip("Segundos que se ve el cartel de victoria antes de pasar al siguiente nivel.")]
        public float segundosParaSiguiente = 2.5f;

        [Tooltip("Tiempo minimo que el cartel se queda en pantalla, aunque se pulse algo.")]
        public float minimoEnPantalla = 0.8f;

        [Tooltip("Congela el nivel al ganar o perder, para que el mapa no siga moviendose detras del cartel.")]
        public bool pausarAlTerminar = true;

        [Header("Mecanica opcional del GDD (\"por alcance\")")]
        [Tooltip("Activa la mecanica Lanzar: apuntar con el mouse y soltar el ingrediente, con proyeccion de trayectoria.")]
        public bool enableThrow = true;

        public LevelState State { get; private set; } = LevelState.Playing;
        public float Mana { get; private set; }
        public float ManaNormalized => maxMana <= 0f ? 0f : Mathf.Clamp01(Mana / maxMana);

        public int TotalIngredients => m_Ingredients.Count;
        public int DeliveredIngredients { get; private set; }
        public int RemainingIngredients => Mathf.Max(0, TotalIngredients - DeliveredIngredients);
        public string LastMessage { get; private set; } = "";

        /// <summary>Cambio de estado del nivel (Playing / Won / Lost) con el mensaje a mostrar.</summary>
        public event Action<LevelState, string> StateChanged;

        /// <summary>Pista o aviso corto para el HUD.</summary>
        public event Action<string> HintChanged;

        /// <summary>El set de ingredientes cambio (entregado o roto).</summary>
        public event Action IngredientsChanged;

        readonly List<Ingredient> m_Ingredients = new List<Ingredient>();

        void Awake()
        {
            Instance = this;
            Mana = maxMana;
            if (builder == null) builder = GetComponent<LevelBuilder>();
        }

        void Start()
        {
            // Si el nivel esta HORNEADO en la escena, nadie llamo a
            // RegisterIngredient (el builder no construyo nada). Los buscamos.
            if (m_Ingredients.Count == 0)
            {
                foreach (var ing in FindObjectsByType<Ingredient>(FindObjectsSortMode.None))
                    RegisterIngredient(ing);

                if (m_Ingredients.Count > 0)
                    IngredientsChanged?.Invoke();
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>El nivel esta horneado a mano en la escena, no lo genera el builder.</summary>
        bool NivelHorneado => builder != null && !builder.buildOnAwake;

        float m_CuentaAtras = -1f;
        float m_VistoVictoria;
        bool m_SoltoTrasGanar;

        void Update()
        {
            HandleRestartInput();
            HandleSiguienteNivel();
        }

        /// <summary>
        /// Al ganar, espera unos segundos con el cartel en pantalla y pasa al
        /// siguiente nivel. Con una tecla o un click se salta la espera.
        /// </summary>
        void HandleSiguienteNivel()
        {
            if (m_CuentaAtras < 0f) return;

            m_VistoVictoria += Time.unscaledDeltaTime;

            var mouse = Mouse.current;

            // Solo adelanta el CLICK. El teclado queda fuera a proposito: se gana
            // levitando, o sea con ESPACIO pulsado, y cualquier roce de esa tecla
            // se llevaba por delante el libro de pociones antes de poder leerlo.
            bool algoPulsado = mouse != null && mouse.leftButton.isPressed;
            if (!m_SoltoTrasGanar)
            {
                if (!algoPulsado) m_SoltoTrasGanar = true;
            }
            else if (m_VistoVictoria >= minimoEnPantalla)
            {
                bool saltar = mouse != null && mouse.leftButton.wasPressedThisFrame;
                if (saltar)
                {
                    m_CuentaAtras = -1f;
                    CargarSiguiente();
                    return;
                }
            }

            m_CuentaAtras -= Time.unscaledDeltaTime;
            if (m_CuentaAtras > 0f) return;

            m_CuentaAtras = -1f;
            CargarSiguiente();
        }

        void CargarSiguiente()
        {
            if (string.IsNullOrEmpty(escenaSiguiente)) return;

            if (!Application.CanStreamedLevelBeLoaded(escenaSiguiente))
            {
                Debug.LogError("[GameManager] La escena '" + escenaSiguiente + "' no esta en Build Settings. " +
                               "Menu Chocoflan > 1 - Aplicar ajustes de proyecto.", this);
                return;
            }
            Time.timeScale = 1f;
            SceneManager.LoadScene(escenaSiguiente);
        }

        /// <summary>Nombre legible del siguiente nivel, para el cartel de victoria.</summary>
        public string SiguienteNivel => escenaSiguiente;

        void HandleRestartInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.rKey.wasPressedThisFrame) RestartLevel();
        }

        // ---- registro de ingredientes -------------------------------------

        public void RegisterIngredient(Ingredient ing)
        {
            if (ing != null && !m_Ingredients.Contains(ing))
                m_Ingredients.Add(ing);
        }

        public IReadOnlyList<Ingredient> Ingredients => m_Ingredients;

        // ---- mana ---------------------------------------------------------

        /// <summary>Intenta gastar mana. Devuelve false si no queda.</summary>
        public bool TrySpendMana(float amount)
        {
            if (Mana <= 0f) return false;
            Mana = Mathf.Max(0f, Mana - amount);
            return true;
        }

        public void RegenMana(float deltaTime)
        {
            Mana = Mathf.Min(maxMana, Mana + manaRegenPerSecond * deltaTime);
        }

        // ---- resultado ----------------------------------------------------

        public void OnIngredientDelivered(Ingredient ing)
        {
            if (State != LevelState.Playing) return;
            DeliveredIngredients++;
            IngredientsChanged?.Invoke();

            if (DeliveredIngredients >= TotalIngredients)
                SetState(LevelState.Won, "Poción completa. Nivel superado.");
            else
                ShowHint("Ingrediente en el caldero. Faltan " + RemainingIngredients + ".");
        }

        public void OnIngredientBroken(Ingredient ing, string reason)
        {
            if (State != LevelState.Playing) return;
            IngredientsChanged?.Invoke();
            SetState(LevelState.Lost, reason + ". Pulsa R para reintentar.");
        }

        void SetState(LevelState newState, string message)
        {
            State = newState;

            // Al ganar o perder se para el nivel entero: ingredientes, plataformas
            // moviles, llamas y particulas. Antes el mapa seguia animandose detras
            // del cartel. El HUD, el libro de victoria y la barra de mana usan
            // tiempo real (unscaledDeltaTime), asi que siguen funcionando.
            if (newState != LevelState.Playing)
            {
                CongelarIngredientes();
                if (pausarAlTerminar) Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = 1f;
            }
            m_CuentaAtras = (newState == LevelState.Won && !string.IsNullOrEmpty(escenaSiguiente))
                ? Mathf.Max(0.3f, segundosParaSiguiente)
                : -1f;
            m_VistoVictoria = 0f;
            m_SoltoTrasGanar = false;
            LastMessage = message;
            StateChanged?.Invoke(newState, message);
        }

        /// <summary>
        /// Deja quietos los ingredientes que no se entregaron ni se rompieron.
        /// Kinematic + velocidad a cero: ni caen ni responden a empujones.
        /// </summary>
        void CongelarIngredientes()
        {
            foreach (var ing in m_Ingredients)
            {
                if (ing == null || ing.IsBroken || ing.IsDelivered) continue;

                var rb = ing.Body;
                if (rb == null) continue;

                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        public void ShowHint(string message)
        {
            LastMessage = message;
            HintChanged?.Invoke(message);
        }

        /// <summary>
        /// Reinicia el nivel. Por defecto reconstruye el tablero en caliente (no depende
        /// de que la escena este en Build Settings); si no hay builder, recarga la escena.
        /// </summary>
        public void RestartLevel()
        {
            // Con el nivel horneado NO se puede reconstruir en caliente: eso
            // borraria los objetos de la escena y con ellos lo que el equipo haya
            // movido a mano. Se recarga la escena.
            if (NivelHorneado)
            {
                Scene actual = SceneManager.GetActiveScene();
                if (actual.buildIndex >= 0) SceneManager.LoadScene(actual.buildIndex);
                else SceneManager.LoadScene(actual.name);
                return;
            }

            if (builder != null)
            {
                // Suelta el ingrediente seleccionado antes de destruirlo.
                var levitation = GetComponent<LevitationController>();
                if (levitation != null) levitation.Select(null);

                m_Ingredients.Clear();
                DeliveredIngredients = 0;
                Mana = maxMana;
                State = LevelState.Playing;
                LastMessage = "";
                builder.Rebuild();
                IngredientsChanged?.Invoke();
                SetState(LevelState.Playing, "");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }
    }
}
