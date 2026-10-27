using UnityEngine;
using UnityEngine.InputSystem;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Levitacion fluida: mantener ESPACIO hace subir al ingrediente seleccionado.
    /// Mientras flota, IZQUIERDA y DERECHA lo dirigen horizontalmente.
    /// Al soltar ESPACIO, vuelve la gravedad y el ingrediente cae.
    /// El mana se consume mientras levita y se regenera cuando no levita.
    /// Click selecciona un ingrediente; Tab cambia al siguiente.
    /// </summary>
    public class LevitationController : MonoBehaviour
    {
        [Header("Levitar - controles")]
        [Tooltip("Tecla a mantener para subir: ESPACIO.")]
        public Key levitateKey = Key.Space;

        [Header("Movimiento con peso (lag)")]
        [Tooltip("Velocidad de subida automatica al mantener ESPACIO (u/s). Antes 2.4.")]
        public float riseSpeed = 2f;

        [Tooltip("Velocidad horizontal maxima al usar las flechas (u/s). Antes 3.6.")]
        public float horizontalSpeed = 2.8f;

        [Tooltip("Aceleracion horizontal (u/s^2). Tiempo en llegar a tope = velocidad / aceleracion. " +
                 "Mas baja = el huevo tarda mas en responder. Antes 12.")]
        public float horizontalAcceleration = 5f;

        [Tooltip("Frenado horizontal al soltar las flechas (u/s^2). Mas bajo que la aceleracion = " +
                 "el huevo sigue deslizando un poco (el lag). Antes era igual a la aceleracion.")]
        public float horizontalBrake = 3f;

        [Tooltip("Aceleracion vertical mientras se mantiene ESPACIO (u/s^2). Antes 8.")]
        public float riseAcceleration = 3.5f;

        [Header("Sensacion de flotar")]
        [Tooltip("Vaiven vertical al levitar, como oleaje (u/s extra sobre la subida). 0 = sin vaiven.")]
        public float bobAmplitude = 0.45f;

        [Tooltip("Cuantas veces por segundo se repite el vaiven.")]
        public float bobFrequency = 1.3f;

        [Tooltip("Inclinacion maxima del ingrediente segun va a un lado (grados). 0 = no se inclina.")]
        public float tiltDegrees = 10f;

        [Tooltip("Que tan rapido se inclina (mayor = mas rapido).")]
        public float tiltSmoothing = 6f;

        [Header("Temblor (levitacion nerviosa)")]
        [Tooltip("Fuerza del temblor continuo (u/s). 0 = sin temblor. 0.2 = sutil, 0.3 = nervioso, 0.6 = muy erratico.")]
        public float tremorStrength = 0.3f;

        [Tooltip("Que tan rapido vibra el temblor (mayor = mas rapido y mas nervioso).")]
        public float tremorSpeed = 7f;

        [Tooltip("Respingos: veces por segundo, en promedio, que el ingrediente da un tiron corto. 0 = sin respingos.")]
        public float twitchChance = 0.6f;

        [Tooltip("Fuerza de cada respingo (u/s).")]
        public float twitchStrength = 0.9f;

        [Tooltip("Cuanto tiembla de lado el ingrediente (grados). 0 = sin vibracion de giro.")]
        public float tremorTiltDegrees = 5f;

        [Tooltip("Cuanto MAS tiembla cuando se acaba el mana. 0 = igual siempre. 0.5 = hasta 50% mas con el mana en cero.")]
        public float tremorExtraAtLowMana = 0.5f;

        [Header("Seleccion")]
        [Tooltip("Radio extra de tolerancia al hacer click sobre un ingrediente, en unidades.")]
        public float clickTolerance = 0.18f;

        [Header("Referencias")]
        public Camera worldCamera;

        public Ingredient Selected { get; private set; }
        public bool IsLevitating { get; private set; }

        // estado leido en Update y aplicado en FixedUpdate
        bool m_LevitateHeld;
        float m_HorizontalInput;
        float m_LevitateTime;
        float m_TremorSeed;

        // click y mantener
        IClickHoldInteractable m_HoldTarget;
        float m_HoldTimer;

        GameManager Game => GameManager.Instance;

        void Awake()
        {
            if (worldCamera == null) worldCamera = Camera.main;
            m_TremorSeed = Random.Range(0f, 100f);
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
                m_HorizontalInput = 0f;
                return;
            }

            // ESPACIO levanta por si solo: no hace falta usar flecha ARRIBA.
            m_LevitateHeld = kb[levitateKey].isPressed;

            // Las flechas solo dirigen el movimiento horizontal.
            float x = 0f;
            if (kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.rightArrowKey.isPressed) x += 1f;
            m_HorizontalInput = x;

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
                        m_LevitateTime = 0f;
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

            m_LevitateTime += Time.fixedDeltaTime;

            // Aproximacion progresiva de ambas velocidades: el control no pega tirones.
            // Acelerar y frenar tienen ritmos distintos: arranca con pereza y, al soltar
            // las flechas, sigue deslizando un poco. Esa diferencia es el "lag".
            Vector2 velocity = rb.linearVelocity;

            // Temblor: ruido suave y distinto en cada eje, mas un respingo de vez en cuando.
            // Crece un poco cuando el mana esta bajo.
            float manaFactor = 1f;
            if (Game != null) manaFactor = 1f + tremorExtraAtLowMana * (1f - Game.ManaNormalized);
            float tremorX = 0f, tremorY = 0f, tremorAngle = 0f;
            if (tremorStrength > 0f || tremorTiltDegrees > 0f)
            {
                float t = m_LevitateTime * tremorSpeed;
                tremorX = (Mathf.PerlinNoise(t, m_TremorSeed) - 0.5f) * 2f;
                tremorY = (Mathf.PerlinNoise(t * 1.3f, m_TremorSeed + 17f) - 0.5f) * 2f;
                tremorAngle = (Mathf.PerlinNoise(t * 0.9f, m_TremorSeed + 41f) - 0.5f) * 2f;
            }

            float targetX = m_HorizontalInput * horizontalSpeed + tremorX * tremorStrength * manaFactor;
            bool reversing = targetX != 0f && velocity.x != 0f &&
                             Mathf.Sign(targetX) != Mathf.Sign(velocity.x);
            float rateX = targetX == 0f
                ? horizontalBrake
                : (reversing ? horizontalAcceleration + horizontalBrake : horizontalAcceleration);
            velocity.x = Mathf.MoveTowards(velocity.x, targetX, rateX * Time.fixedDeltaTime);

            // Al mantener ESPACIO sube solo, con un vaiven suave encima.
            float bob = Mathf.Sin(m_LevitateTime * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            velocity.y = Mathf.MoveTowards(
                velocity.y, riseSpeed + bob + tremorY * tremorStrength * manaFactor,
                riseAcceleration * Time.fixedDeltaTime);

            // Respingo: un tiron corto en una direccion al azar.
            if (twitchChance > 0f && twitchStrength > 0f &&
                Random.value < twitchChance * Time.fixedDeltaTime)
            {
                velocity += Random.insideUnitCircle.normalized * twitchStrength * manaFactor;
            }
            rb.linearVelocity = velocity;

            // Se inclina un poco hacia donde va, con una vibracion de giro encima.
            if (tiltDegrees > 0f || tremorTiltDegrees > 0f)
            {
                float lean = Mathf.Clamp(velocity.x / Mathf.Max(0.01f, horizontalSpeed), -1f, 1f);
                float targetAngle = -lean * tiltDegrees + tremorAngle * tremorTiltDegrees * manaFactor;
                float blend = 1f - Mathf.Exp(-tiltSmoothing * Time.fixedDeltaTime);
                rb.MoveRotation(Mathf.LerpAngle(rb.rotation, targetAngle, blend));
            }
        }
    }
}
