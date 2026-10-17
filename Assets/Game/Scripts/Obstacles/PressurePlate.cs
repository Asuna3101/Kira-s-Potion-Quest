using System.Collections.Generic;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Boton de presion (GDD: "se activan al tener contacto con los ingredientes").
    /// En el Nivel 1 es latching: una vez pisado la compuerta queda abierta, para que el
    /// tutorial no castigue al jugador.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PressurePlate : MonoBehaviour
    {
        [Tooltip("Compuertas que abre este boton.")]
        public List<Gate> gates = new List<Gate>();

        [Tooltip("Si es true se queda pulsado para siempre. Si es false se suelta al salir el ingrediente.")]
        public bool latching = true;

        public bool IsPressed { get; private set; }

        SpriteRenderer m_Sr;
        int m_Contacts;

        void Awake()
        {
            m_Sr = GetComponent<SpriteRenderer>();
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var ing = other.GetComponentInParent<Ingredient>();
            if (ing == null || ing.IsBroken) return;
            m_Contacts++;
            SetPressed(true);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var ing = other.GetComponentInParent<Ingredient>();
            if (ing == null) return;
            m_Contacts = Mathf.Max(0, m_Contacts - 1);
            if (!latching && m_Contacts == 0) SetPressed(false);
        }

        void SetPressed(bool pressed)
        {
            if (IsPressed == pressed) return;
            IsPressed = pressed;

            foreach (var g in gates)
            {
                if (g == null) continue;
                if (pressed) g.Open(); else g.Close();
            }

            if (m_Sr != null)
                m_Sr.color = pressed ? PlaceholderArt.ButtonDone : Color.white;

            if (pressed)
                GameManager.Instance?.ShowHint("La compuerta se abrio. El caldero ya es alcanzable.");
        }
    }
}
