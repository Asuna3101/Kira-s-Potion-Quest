using System.Collections;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Un ingrediente: el objeto que el jugador mueve con la mecanica Levitar.
    /// GDD: "algunos ingredientes son mas fragiles que otros".
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class Ingredient : MonoBehaviour
    {
        [Header("Identidad")]
        public string ingredientName = "Ingrediente";

        [Header("Fragilidad")]
        [Tooltip("Si es fragil se rompe tambien por impactos fuertes, no solo por pinchos y fuego.")]
        public bool fragile = false;

        [Tooltip("Velocidad de impacto (u/s) a partir de la cual un ingrediente fragil se rompe.")]
        public float breakImpactSpeed = 10f;

        [Header("Fisica")]
        public float normalGravityScale = 0.6f;
        public float normalLinearDamping = 0.35f;

        [Header("Levitar - sensacion")]
        [Tooltip("Rozamiento del aire mientras levita. Bajo = flota libre y LevitationController " +
                 "puede alcanzar la velocidad que pide (con 3.5 el aire frenaba demasiado).")]
        public float levitationLinearDamping = 0.8f;

        [Tooltip("Cada cuantos segundos suelta una chispa mientras levita (0 = sin chispas).")]
        public float sparkInterval = 0.07f;

        [Header("Romperse")]
        [Tooltip("Color de los cascarones (o de las motas de polvo si shellShards esta apagado).")]
        public Color shellColor = new Color32(0xFF, 0xF4, 0xD6, 0xFF);

        [Tooltip("Color de las gotas de yema que salen al romperse.")]
        public Color yolkColor = new Color32(0xFF, 0xC1, 0x3A, 0xFF);

        [Tooltip("Encendido: estalla en cascarones y yema (huevo). Apagado: estalla en motas de polvo.")]
        public bool shellShards = true;

        [Tooltip("OPCIONAL: frames de arte de la rotura, en orden. Si se llenan, reemplazan " +
                 "la animacion por codigo y se queda el ultimo frame.")]
        public Sprite[] breakFrames;

        public float breakFrameRate = 14f;

        [Tooltip("Segundos antes de mostrar NIVEL FALLIDO, para que se alcance a ver la rotura.")]
        public float lostDelay = 0.9f;

        public bool IsDelivered { get; private set; }
        public bool IsBroken { get; private set; }
        public bool IsLevitating { get; private set; }

        Rigidbody2D m_Rb;
        SpriteRenderer m_Sr;
        Transform m_SelectionRing;
        float m_SparkTimer;

        public Rigidbody2D Body => m_Rb;

        void Awake()
        {
            m_Rb = GetComponent<Rigidbody2D>();
            m_Sr = GetComponent<SpriteRenderer>();

            m_Rb.bodyType = RigidbodyType2D.Dynamic;
            m_Rb.gravityScale = normalGravityScale;
            m_Rb.linearDamping = normalLinearDamping;
            m_Rb.angularDamping = 2f;
            m_Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            m_Rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            BuildSelectionRing();
        }

        void BuildSelectionRing()
        {
            var ring = new GameObject("SelectionRing");
            ring.transform.SetParent(transform, false);
            var sr = ring.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderArt.Get(ArtKind.SelectionRing, 2, 2);
            sr.sortingLayerName = "FX";
            sr.sortingOrder = -1;
            Material mat = PlaceholderArt.UnlitSpriteMaterial();
            if (mat != null) sr.sharedMaterial = mat;
            // El ingrediente mide 0.8 u, asi que su transform viene escalado. El aro
            // deshace ese escalado para medir siempre 1 u en el mundo y quedar como
            // un halo justo por fuera del ingrediente.
            Vector3 parentScale = transform.localScale;
            ring.transform.localScale = new Vector3(
                Mathf.Approximately(parentScale.x, 0f) ? 1f : 1f / parentScale.x,
                Mathf.Approximately(parentScale.y, 0f) ? 1f : 1f / parentScale.y,
                1f);

            var anim = ring.AddComponent<PlaceholderAnimator>();
            anim.mode = PlaceholderAnimator.Mode.Spin;
            anim.speed = 90f;
            ring.SetActive(false);
            m_SelectionRing = ring.transform;
        }

        public void SetSelected(bool selected)
        {
            if (m_SelectionRing != null)
                m_SelectionRing.gameObject.SetActive(selected && !IsDelivered && !IsBroken);
        }

        // ---- Levitar ------------------------------------------------------

        public void BeginLevitate()
        {
            if (IsBroken || IsDelivered) return;
            IsLevitating = true;
            m_Rb.gravityScale = 0f;
            m_Rb.linearDamping = levitationLinearDamping;
            m_Rb.angularVelocity = 0f;
            m_SparkTimer = 0f;
        }

        public void EndLevitate()
        {
            IsLevitating = false;
            if (IsBroken || IsDelivered) return;
            m_Rb.gravityScale = normalGravityScale;
            m_Rb.linearDamping = normalLinearDamping;
        }

        // ---- Resultado ----------------------------------------------------

        /// <summary>Lo destruyo un obstaculo (pinchos, fuego) o un impacto demasiado fuerte.</summary>
        public void Break(string reason)
        {
            if (IsBroken || IsDelivered) return;
            IsBroken = true;
            IsLevitating = false;
            SetSelected(false);

            m_Rb.bodyType = RigidbodyType2D.Kinematic;
            m_Rb.linearVelocity = Vector2.zero;
            m_Rb.angularVelocity = 0f;
            foreach (var c in GetComponents<Collider2D>()) c.enabled = false;

            // El nivel se marca como perdido al terminar la animacion (lostDelay),
            // no al instante: si no, el cartel taparia la rotura.
            StartCoroutine(BreakRoutine(reason));
        }

        IEnumerator BreakRoutine(string reason)
        {
            float elapsed = 0f;

            if (breakFrames != null && breakFrames.Length > 0 && m_Sr != null)
            {
                // Arte de Melanie: se reproducen sus frames y se queda el ultimo.
                float frameTime = 1f / Mathf.Max(1f, breakFrameRate);
                for (int i = 0; i < breakFrames.Length; i++)
                {
                    m_Sr.sprite = breakFrames[i];
                    yield return new WaitForSeconds(frameTime);
                    elapsed += frameTime;
                }
            }
            else
            {
                // Placeholder: se aplasta un instante (el golpe) y estalla.
                const float squashTime = 0.07f;
                Vector3 baseScale = transform.localScale;
                for (float t = 0f; t < squashTime; t += Time.deltaTime)
                {
                    float k = t / squashTime;
                    transform.localScale = new Vector3(
                        baseScale.x * Mathf.Lerp(1f, 1.25f, k),
                        baseScale.y * Mathf.Lerp(1f, 0.7f, k),
                        baseScale.z);
                    yield return null;
                }
                elapsed += squashTime;
                transform.localScale = baseScale;
                if (m_Sr != null) m_Sr.enabled = false;
                SpawnBreakBurst();
            }

            yield return new WaitForSeconds(Mathf.Max(0f, lostDelay - elapsed));
            GameManager.Instance?.OnIngredientBroken(this, reason);
        }

        void SpawnBreakBurst()
        {
            Transform parent = transform.parent;
            Vector2 center = transform.position;
            float radius = GridSettings.IngredientSize * 0.5f;

            // destello del golpe
            FxParticle.Spawn(parent, FxSprites.Glow, center, Vector2.zero, 0f, 0f, 0.28f,
                0.5f, 1.8f, new Color(1f, 0.97f, 0.82f, 0.85f), 0f, 0f, "FX", 6);

            const int pieces = 8;
            for (int i = 0; i < pieces; i++)
            {
                float angle = ((i + Random.Range(-0.3f, 0.3f)) / pieces) * Mathf.PI * 2f;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 velocity = dir * Random.Range(1.6f, 3.0f) + Vector2.up * Random.Range(0.8f, 1.8f);
                Vector2 start = center + dir * radius * 0.4f;

                if (shellShards)
                {
                    // cascarones: giran, caen y se desvanecen al final
                    FxParticle.Spawn(parent, FxSprites.Shard, start, velocity, 9f, 0.3f,
                        Random.Range(0.8f, 1.2f), Random.Range(0.55f, 0.95f), Random.Range(0.45f, 0.7f),
                        shellColor, Random.Range(-540f, 540f), 0.55f);
                }
                else
                {
                    // motas de polvo: suben un poco, se agrandan y se esfuman
                    FxParticle.Spawn(parent, FxSprites.Glow, start, velocity * 0.5f, 1f, 1.5f,
                        Random.Range(0.55f, 0.85f), Random.Range(0.18f, 0.28f), Random.Range(0.4f, 0.55f),
                        shellColor, 0f, 0.1f);
                }
            }

            if (shellShards)
            {
                // gotas de yema
                const int drops = 6;
                for (int i = 0; i < drops; i++)
                {
                    Vector2 velocity = new Vector2(Random.Range(-1.6f, 1.6f), Random.Range(0.4f, 2.2f));
                    FxParticle.Spawn(parent, FxSprites.Disc, center + Random.insideUnitCircle * radius * 0.3f,
                        velocity, 8f, 0.2f, Random.Range(0.7f, 1.0f),
                        Random.Range(0.1f, 0.18f), Random.Range(0.06f, 0.1f),
                        yolkColor, 0f, 0.6f);
                }
            }
        }

        void Update()
        {
            if (!IsLevitating || sparkInterval <= 0f) return;

            m_SparkTimer -= Time.deltaTime;
            if (m_SparkTimer > 0f) return;
            m_SparkTimer = sparkInterval * Random.Range(0.7f, 1.4f);

            // Polvo magico: nace dentro del ingrediente y se queda atras al subir.
            float radius = GridSettings.IngredientSize * 0.5f;
            Vector2 position = (Vector2)transform.position + Random.insideUnitCircle * radius;
            Vector2 velocity = new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(-0.45f, -0.1f));
            FxParticle.Spawn(transform.parent, FxSprites.Glow, position, velocity, 0f, 0.5f,
                Random.Range(0.5f, 0.9f), Random.Range(0.08f, 0.15f), 0f,
                new Color(1f, 0.96f, 0.72f, 0.9f), 0f, 0f, "FX", 5);
        }

        /// <summary>Entro al caldero.</summary>
        public void Deliver(Vector3 cauldronCenter)
        {
            if (IsBroken || IsDelivered) return;
            IsDelivered = true;
            IsLevitating = false;
            SetSelected(false);

            m_Rb.bodyType = RigidbodyType2D.Kinematic;
            m_Rb.linearVelocity = Vector2.zero;
            foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
            transform.position = new Vector3(cauldronCenter.x, cauldronCenter.y, transform.position.z);

            var anim = GetComponent<PlaceholderAnimator>();
            if (anim == null) anim = gameObject.AddComponent<PlaceholderAnimator>();
            anim.Play(PlaceholderAnimator.Mode.ShrinkAndFade, 2.2f);

            GameManager.Instance?.OnIngredientDelivered(this);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!fragile || IsBroken || IsDelivered) return;
            // Se rompe segun la velocidad con la que choca, venga de una caida alta o de
            // ir demasiado rapido (por ejemplo, un lanzamiento).
            Vector2 impact = collision.relativeVelocity;
            if (impact.magnitude >= breakImpactSpeed)
            {
                bool fell = Mathf.Abs(impact.y) >= Mathf.Abs(impact.x);
                Break(ingredientName + (fell ? " se rompio al caer de muy alto"
                                             : " se rompio por ir demasiado rapido"));
            }
        }
    }
}
