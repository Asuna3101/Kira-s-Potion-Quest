using System.Collections.Generic;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Portal (GDD): transporta al ingrediente hacia donde esta el otro portal del par.
    /// Conserva la velocidad y aplica un cooldown para no rebotar entre los dos.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Portal : MonoBehaviour, IClickHoldInteractable
    {
        public Portal linkedPortal;

        [Tooltip("Segundos durante los que un ingrediente recien teletransportado no puede volver a entrar.")]
        public float cooldown = 0.45f;

        readonly Dictionary<Ingredient, float> m_Blocked = new Dictionary<Ingredient, float>();

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        public void Link(Portal other)
        {
            linkedPortal = other;
            if (other != null) other.linkedPortal = this;
        }

        /// <summary>Marca al ingrediente como "acaba de salir de aqui".</summary>
        public void BlockFor(Ingredient ing)
        {
            if (ing != null) m_Blocked[ing] = Time.time + cooldown;
        }

        bool IsBlocked(Ingredient ing)
        {
            return m_Blocked.TryGetValue(ing, out float until) && Time.time < until;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var ing = other.GetComponentInParent<Ingredient>();
            if (ing == null || ing.IsBroken || ing.IsDelivered) return;
            if (linkedPortal == null || IsBlocked(ing)) return;

            Rigidbody2D rb = ing.Body;
            Vector2 keptVelocity = rb != null ? rb.linearVelocity : Vector2.zero;

            linkedPortal.BlockFor(ing);
            BlockFor(ing);

            Vector3 target = linkedPortal.transform.position;
            ing.transform.position = new Vector3(target.x, target.y, ing.transform.position.z);
            if (rb != null)
            {
                rb.position = new Vector2(target.x, target.y);
                rb.linearVelocity = keptVelocity;
            }
        }

        // "Interactuar: click y mantener" -> resalta el portal pareja.
        public float HoldSeconds => 0.3f;

        public void OnHoldProgress(float normalized) { }

        public void OnHoldCompleted()
        {
            if (linkedPortal == null) return;
            var anim = linkedPortal.GetComponent<PlaceholderAnimator>();
            if (anim != null) anim.Play(PlaceholderAnimator.Mode.Pulse, 8f);
            GameManager.Instance?.ShowHint("Este portal sale por su pareja del otro color.");
        }

        public void OnHoldCancelled() { }
    }
}
