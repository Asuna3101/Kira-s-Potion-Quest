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

        public bool IsDelivered { get; private set; }
        public bool IsBroken { get; private set; }
        public bool IsLevitating { get; private set; }

        Rigidbody2D m_Rb;
        SpriteRenderer m_Sr;
        Transform m_SelectionRing;

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
            m_Rb.linearDamping = 3.5f;
            m_Rb.angularVelocity = 0f;
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

            if (m_Sr != null) m_Sr.color = new Color(1f, 1f, 1f, 0.25f);
            m_Rb.bodyType = RigidbodyType2D.Kinematic;
            m_Rb.linearVelocity = Vector2.zero;
            foreach (var c in GetComponents<Collider2D>()) c.enabled = false;

            GameManager.Instance?.OnIngredientBroken(this, reason);
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
            if (collision.relativeVelocity.magnitude >= breakImpactSpeed)
                Break(ingredientName + " se rompio por el golpe");
        }
    }
}
