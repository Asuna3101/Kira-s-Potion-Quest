using UnityEngine;
using UnityEngine.InputSystem;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// MECANICA PRINCIPAL DEL GDD - "Levitar: control del ingrediente en el aire".
    ///
    /// El GDD dice Ctrl + flechas; se usa ESPACIO + flechas porque Ctrl + flechas
    /// choca con atajos del sistema (en macOS Ctrl+flecha cambia de escritorio) y
    /// porque espacio es mas comodo de mantener con el pulgar mientras la otra mano
    /// esta en las flechas. Cambiarlo es cambiar levitateKey en el inspector.
    ///
    /// Tambien resuelve los otros dos controles del GDD:
    ///   - Seleccionar ingrediente: click izquierdo sobre el.
    ///   - Interactuar: click y mantener sobre elementos del tablero (IClickHoldInteractable).
    ///
    /// Reglas:
    ///   - Mantener espacio levita el ingrediente seleccionado: se le anula la
    ///     gravedad y las flechas lo mueven. Espacio sin flechas = flotar quieto.
    ///   - Levitar consume mana. Al quedarse a cero el ingrediente cae.
    ///   - El mana se recupera cuando no se levita.
    /// </summary>
    public class LevitationController : MonoBehaviour
    {
        [Header("Levitar")]
        [Tooltip("Tecla que hay que mantener para levitar. Por defecto Espacio.")]
        public Key levitateKey = Key.Space;

        [Tooltip("Si tambien se acepta Ctrl, como dice literalmente el GDD.")]
        public bool alsoAcceptCtrl = true;

        [Tooltip("Velocidad objetivo mientras se levita, en unidades por segundo.")]
        public float levitateSpeed = 4f;

        [Tooltip("Aceleracion con la que se alcanza la velocidad objetivo (u/s^2).")]
        public float levitateAcceleration = 26f;

        [Header("Seleccion")]
        [Tooltip("Radio extra de tolerancia al hacer click sobre un ingrediente, en unidades.")]
        public float clickTolerance = 0.18f;

        [Header("Referencias")]
        public Camera worldCamera;

        public Ingredient Selected { get; private set; }
        public bool IsLevitating { get; private set; }

        // estado leido en Update y aplicado en FixedUpdate
        bool m_LevitateHeld;
        Vector2 m_MoveInput;

        // click y mantener
        IClickHoldInteractable m_HoldTarget;
        float m_HoldTimer;

        GameManager Game => GameManager.Instance;

        void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
        }

        void Update()
        {
            if (Game != null && Game.State != LevelState.Playing)
            {
                ReleaseLevitation();
                return;
            }

            ReadKeyboard();
            ReadMouse();
            UpdateMana();
        }

        // ---------------- input --------------------------------------------

        void ReadKeyboard()
        {
            var kb = Keyboard.current;
            if (kb == null)
            {
                m_LevitateHeld = false;
                m_MoveInput = Vector2.zero;
                return;
            }

            m_LevitateHeld = kb[levitateKey].isPressed ||
                             (alsoAcceptCtrl && (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed));

            float x = 0f, y = 0f;
            if (kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.rightArrowKey.isPressed) x += 1f;
            if (kb.downArrowKey.isPressed) y -= 1f;
            if (kb.upArrowKey.isPressed) y += 1f;
            m_MoveInput = new Vector2(x, y);

            // Tab cambia de ingrediente seleccionado.
            if (kb.tabKey.wasPressedThisFrame) SelectNext();

            // Si se pulsa la tecla de levitar sin nada seleccionado, se autoselecciona.
            if (m_LevitateHeld && (Selected == null || Selected.IsDelivered || Selected.IsBroken))
                SelectNext();
        }

        void ReadMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null || worldCamera == null) return;

            Vector2 world = ScreenToWorld(mouse.position.ReadValue());

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Ingredient ing = PickIngredient(world);
                if (ing != null)
                {
                    Select(ing);
                    m_HoldTarget = null;
                }
                else
                {
                    m_HoldTarget = PickInteractable(world);
                    m_HoldTimer = 0f;
                }
            }

            if (m_HoldTarget != null)
            {
                if (mouse.leftButton.isPressed)
                {
                    m_HoldTimer += Time.deltaTime;
                    float need = Mathf.Max(0.01f, m_HoldTarget.HoldSeconds);
                    m_HoldTarget.OnHoldProgress(Mathf.Clamp01(m_HoldTimer / need));
                    if (m_HoldTimer >= need)
                    {
                        m_HoldTarget.OnHoldCompleted();
                        m_HoldTarget = null;
                    }
                }
                else
                {
                    m_HoldTarget.OnHoldCancelled();
                    m_HoldTarget = null;
                }
            }
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            Vector3 p = worldCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z));
            return new Vector2(p.x, p.y);
        }

        Ingredient PickIngredient(Vector2 world)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(world, clickTolerance);
            Ingredient best = null;
            float bestDist = float.MaxValue;
            foreach (var h in hits)
            {
                var ing = h.GetComponentInParent<Ingredient>();
                if (ing == null || ing.IsBroken || ing.IsDelivered) continue;
                float d = ((Vector2)ing.transform.position - world).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = ing; }
            }
            return best;
        }

        IClickHoldInteractable PickInteractable(Vector2 world)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(world, clickTolerance);
            foreach (var h in hits)
            {
                var it = h.GetComponentInParent<IClickHoldInteractable>();
                if (it != null) return it;
            }
            return null;
        }

        // ---------------- seleccion ----------------------------------------

        public void Select(Ingredient ing)
        {
            if (Selected == ing) return;
            if (Selected != null)
            {
                Selected.EndLevitate();
                Selected.SetSelected(false);
            }
            Selected = ing;
            IsLevitating = false;
            if (Selected != null) Selected.SetSelected(true);
        }

        /// <summary>Selecciona el siguiente ingrediente disponible (Tab o autoseleccion).</summary>
        public void SelectNext()
        {
            if (Game == null) return;
            var list = Game.Ingredients;
            if (list.Count == 0) return;

            int start = 0;
            for (int i = 0; i < list.Count; i++)
                if (list[i] == Selected) { start = i + 1; break; }

            for (int k = 0; k < list.Count; k++)
            {
                Ingredient candidate = list[(start + k) % list.Count];
                if (candidate != null && !candidate.IsBroken && !candidate.IsDelivered)
                {
                    Select(candidate);
                    return;
                }
            }
        }

        // ---------------- mana + fisica ------------------------------------

        void UpdateMana()
        {
            if (Game == null) return;

            bool wantsLevitate = m_LevitateHeld && Selected != null && !Selected.IsBroken && !Selected.IsDelivered;

            if (wantsLevitate)
            {
                if (Game.TrySpendMana(Game.manaDrainPerSecond * Time.deltaTime))
                {
                    if (!IsLevitating)
                    {
                        IsLevitating = true;
                        Selected.BeginLevitate();
                    }
                }
                else
                {
                    ReleaseLevitation();
                    Game.ShowHint("Sin mana. Deja de levitar para recuperarlo.");
                }
            }
            else
            {
                ReleaseLevitation();
                Game.RegenMana(Time.deltaTime);
            }
        }

        void ReleaseLevitation()
        {
            if (!IsLevitating) return;
            IsLevitating = false;
            if (Selected != null) Selected.EndLevitate();
        }

        void FixedUpdate()
        {
            if (!IsLevitating || Selected == null) return;
            Rigidbody2D rb = Selected.Body;
            if (rb == null) return;

            Vector2 dir = m_MoveInput.sqrMagnitude > 1f ? m_MoveInput.normalized : m_MoveInput;
            Vector2 target = dir * levitateSpeed;

            rb.linearVelocity = Vector2.MoveTowards(
                rb.linearVelocity, target, levitateAcceleration * Time.fixedDeltaTime);
        }
    }
}
