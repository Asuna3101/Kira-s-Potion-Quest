using System.Collections.Generic;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Palanca (GDD, control "Interactuar: click y mantener").
    /// Implementada y lista para usar; NO esta colocada en el Nivel 1 a proposito
    /// (ver decisiones de diseno en Docs/Nivel01_Medidas.md). Para usarla basta poner
    /// el caracter 'L' en el mapa de Level01Data.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Lever : MonoBehaviour, IClickHoldInteractable
    {
        public List<Gate> gates = new List<Gate>();

        [Tooltip("Segundos que hay que mantener el click para accionarla.")]
        public float holdSeconds = 0.4f;

        public bool IsOn { get; private set; }

        SpriteRenderer m_Sr;

        void Awake() { m_Sr = GetComponent<SpriteRenderer>(); }

        public float HoldSeconds => holdSeconds;

        public void OnHoldProgress(float normalized)
        {
            if (m_Sr != null)
                m_Sr.color = Color.Lerp(Color.white, PlaceholderArt.Highlight, normalized);
        }

        public void OnHoldCompleted()
        {
            IsOn = !IsOn;
            foreach (var g in gates)
            {
                if (g == null) continue;
                if (IsOn) g.Open(); else g.Close();
            }
            transform.localRotation = Quaternion.Euler(0f, 0f, IsOn ? -35f : 35f);
            if (m_Sr != null) m_Sr.color = IsOn ? PlaceholderArt.ButtonDone : Color.white;
        }

        public void OnHoldCancelled()
        {
            if (m_Sr != null) m_Sr.color = IsOn ? PlaceholderArt.ButtonDone : Color.white;
        }
    }
}
