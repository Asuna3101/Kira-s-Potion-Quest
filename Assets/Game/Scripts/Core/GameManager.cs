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

        [Header("Mana de levitacion")]
        public float maxMana = Level01Data.MaxMana;
        public float manaDrainPerSecond = Level01Data.ManaDrainPerSecond;
        public float manaRegenPerSecond = Level01Data.ManaRegenPerSecond;

        [Header("Referencias")]
        [Tooltip("Builder del nivel. Si esta asignado, R reconstruye el tablero sin recargar la escena.")]
        public LevelBuilder builder;

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

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            HandleRestartInput();
        }

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
                SetState(LevelState.Won, "Pocion completa. Nivel superado.");
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
            LastMessage = message;
            StateChanged?.Invoke(newState, message);
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
