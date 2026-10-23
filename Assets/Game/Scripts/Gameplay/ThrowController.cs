using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Mecanica "Lanzar" del GDD, marcada ahi como **por alcance** (opcional):
    /// "usar el mouse para apuntar y soltar el ingrediente", con proyeccion de trayectoria.
    ///
    /// Como se usa: click izquierdo SOBRE el ingrediente y arrastrar hacia atras
    /// (estilo Angry Birds). Mientras se arrastra aparecen puntos con la trayectoria
    /// prevista. Al soltar, el ingrediente sale disparado.
    ///
    /// Se puede desactivar por completo desde GameManager.enableThrow sin romper nada:
    /// la mecanica principal (Levitar) es independiente de este script.
    /// </summary>
    [RequireComponent(typeof(LevitationController))]
    public class ThrowController : MonoBehaviour
    {
        [Header("Lanzar")]
        [Tooltip("Arrastre minimo (en unidades) para que cuente como lanzamiento y no como simple click.")]
        public float minDrag = 0.35f;

        [Tooltip("Arrastre maximo considerado; mas alla no gana potencia. El mockup de arte marca 1.8 u.")]
        public float maxDrag = 1.8f;

        [Tooltip("Impulso por unidad de arrastre.")]
        public float powerPerUnit = 4.2f;

        [Header("Proyeccion de trayectoria")]
        public int previewDots = 14;
        public float previewStep = 0.08f;

        [Tooltip("Diametro de cada punto de la trayectoria. El mockup de arte marca 0.15 u.")]
        public float previewDotSize = 0.15f;

        LevitationController m_Levitation;
        Camera m_Camera;
        readonly List<Transform> m_Dots = new List<Transform>();

        Ingredient m_Aiming;
        Vector2 m_AimStartWorld;
        Transform m_DotsRoot;

        bool Enabled => GameManager.Instance == null || GameManager.Instance.enableThrow;

        void Awake()
        {
            m_Levitation = GetComponent<LevitationController>();
            m_Camera = m_Levitation.worldCamera != null ? m_Levitation.worldCamera : Camera.main;
            BuildDots();
        }

        void BuildDots()
        {
            var root = new GameObject("TrajectoryPreview");
            root.transform.SetParent(transform, false);
            m_DotsRoot = root.transform;

            Material mat = PlaceholderArt.UnlitSpriteMaterial();
            for (int i = 0; i < previewDots; i++)
            {
                var dot = new GameObject("AimDot_" + i);
                dot.transform.SetParent(m_DotsRoot, false);
                var sr = dot.AddComponent<SpriteRenderer>();
                sr.sprite = PlaceholderLibrary.Get(ArtKind.AimDot, 1, 1);
                sr.sortingLayerName = "FX";
                sr.sortingOrder = 10;

                // El sprite placeholder mide 1 tile (0.5 u); el documento de arte
                // pide puntos de 0.15 u de diametro.
                float native = sr.sprite.bounds.size.x;
                if (native > 0.0001f)
                {
                    float k = previewDotSize / native;
                    dot.transform.localScale = new Vector3(k, k, 1f);
                }
                float a = 1f - (float)i / previewDots;
                sr.color = new Color(1f, 1f, 1f, 0.25f + 0.6f * a);
                if (mat != null) sr.sharedMaterial = mat;
                dot.SetActive(false);
                m_Dots.Add(dot.transform);
            }
        }

        void Update()
        {
            if (!Enabled) { HideDots(); return; }

            var gm = GameManager.Instance;
            if (gm != null && gm.State != LevelState.Playing) { CancelAim(); return; }

            var mouse = Mouse.current;
            if (mouse == null || m_Camera == null) return;

            Vector2 world = m_Levitation.ScreenToWorld(mouse.position.ReadValue());

            // Levitar tiene prioridad: no se apunta mientras se esta levitando.
            if (m_Levitation.IsLevitating) { CancelAim(); return; }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                Ingredient sel = m_Levitation.Selected;
                if (sel != null && !sel.IsBroken && !sel.IsDelivered &&
                    ((Vector2)sel.transform.position - world).magnitude <= 0.6f)
                {
                    m_Aiming = sel;
                    m_AimStartWorld = sel.transform.position;
                }
            }

            if (m_Aiming == null) return;

            if (m_Aiming.IsBroken || m_Aiming.IsDelivered) { CancelAim(); return; }

            // Apuntar: se arrastra HACIA ATRAS, el ingrediente sale en sentido opuesto.
            Vector2 pull = m_AimStartWorld - world;
            float len = Mathf.Min(pull.magnitude, maxDrag);
            Vector2 launchVelocity = pull.sqrMagnitude < 0.000001f
                ? Vector2.zero
                : pull.normalized * len * powerPerUnit;

            if (mouse.leftButton.isPressed)
            {
                if (len >= minDrag) DrawTrajectory(m_Aiming, launchVelocity);
                else HideDots();
            }
            else
            {
                HideDots();
                if (len >= minDrag)
                {
                    Rigidbody2D rb = m_Aiming.Body;
                    if (rb != null)
                    {
                        m_Aiming.EndLevitate();
                        rb.linearVelocity = launchVelocity;
                    }
                }
                m_Aiming = null;
            }
        }

        void DrawTrajectory(Ingredient ing, Vector2 v0)
        {
            Vector2 p0 = ing.transform.position;
            Vector2 g = Physics2D.gravity * ing.normalGravityScale;

            for (int i = 0; i < m_Dots.Count; i++)
            {
                float t = (i + 1) * previewStep;
                Vector2 p = p0 + v0 * t + 0.5f * g * t * t;
                m_Dots[i].position = new Vector3(p.x, p.y, 0f);
                m_Dots[i].gameObject.SetActive(true);
            }
        }

        void HideDots()
        {
            for (int i = 0; i < m_Dots.Count; i++)
                if (m_Dots[i].gameObject.activeSelf) m_Dots[i].gameObject.SetActive(false);
        }

        void CancelAim()
        {
            m_Aiming = null;
            HideDots();
        }
    }
}
