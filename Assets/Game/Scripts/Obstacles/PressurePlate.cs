
using System.Collections.Generic;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    [RequireComponent(typeof(Collider2D))]
    public class PressurePlate : MonoBehaviour
    {
        [Header("Que abre")]

        [Tooltip("Paredes moviles que abre este boton.")]
        public List<Gate> gates = new List<Gate>();


        [Header("Comportamiento")]

        [Tooltip("Una vez pulsado permanece activado.")]
        public bool seQuedaPulsado = true;


        [Header("Deteccion de impacto")]

        [Tooltip("Exige un impacto fisico para activar el boton.")]
        public bool requiereImpacto = true;

        [Tooltip("Velocidad minima hacia el boton para activarlo.")]
        [Min(0f)]
        public float velocidadMinimaImpacto = 2.5f;

        [Tooltip("Solo permite impactos desde la cara superior.")]
        public bool requiereImpactoSuperior = true;

        [Tooltip("Tolerancia de la direccion del contacto.")]
        [Range(0f, 1f)]
        public float alineacionMinimaImpacto = 0.65f;

        [Tooltip("Permite activar el boton mientras se levita.")]
        public bool permitirImpactoLevitando = false;


        [Header("Deteccion de permanencia")]

        [Tooltip("Altura de la zona que detecta al ingrediente.")]
        [Min(0f)]
        public float alturaDeteccion = 0.4f;


        [Header("Aspecto")]

        public Sprite spritePresionado;

        [Min(0f)]
        public float hundimiento = 0.06f;

        [Min(0f)]
        public float velocidadHundido = 10f;


        [Header("Aviso")]

        public string aviso = "La pared se abrió.";


        [Header("Depuracion")]

        [SerializeField]
        private float ultimoImpacto;

        [SerializeField]
        private bool impactoValido;


        public bool IsPressed { get; private set; }


        SpriteRenderer m_Sr;
        Sprite m_SpriteNormal;
        Collider2D m_Col;

        Vector3 m_PosArriba;

        bool m_AvisoDado;

        readonly HashSet<Ingredient> m_Encima =
            new HashSet<Ingredient>();

        // Ingredientes que han activado el boton
        // mediante un impacto valido.
        readonly HashSet<Ingredient> m_Impactaron =
            new HashSet<Ingredient>();

        readonly List<Ingredient> m_Fuera =
            new List<Ingredient>();

        ContactFilter2D m_Filtro;

        static readonly Collider2D[] s_Buf =
            new Collider2D[32];


        void Awake()
        {
            m_Sr = GetComponent<SpriteRenderer>();

            if (m_Sr != null)
                m_SpriteNormal = m_Sr.sprite;

            m_Col = BuscarOCrearZona();

            m_PosArriba = transform.position;

            m_Filtro = new ContactFilter2D();
            m_Filtro.NoFilter();
            m_Filtro.useTriggers = false;
        }


        Collider2D BuscarOCrearZona()
        {
            var cols = GetComponents<Collider2D>();

            foreach (var c in cols)
            {
                if (c.isTrigger)
                    return c;
            }

            Vector2 tam = Vector2.one;
            Vector2 off = Vector2.zero;

            if (cols.Length > 0)
            {
                if (cols[0] is BoxCollider2D caja)
                {
                    tam = caja.size;
                    off = caja.offset;
                }
                else if (cols[0] is CapsuleCollider2D cap)
                {
                    tam = cap.size;
                    off = cap.offset;
                }
            }

            var zona =
                gameObject.AddComponent<BoxCollider2D>();

            zona.isTrigger = true;

            zona.size = new Vector2(
                tam.x,
                tam.y + alturaDeteccion
            );

            zona.offset = off + new Vector2(
                0f,
                alturaDeteccion * 0.5f
            );

            return zona;
        }


        bool IngredienteValido(Ingredient ing)
        {
            return ing != null &&
                   !ing.IsBroken &&
                   !ing.IsDelivered;
        }


        void OnCollisionEnter2D(Collision2D collision)
        {
            if (!requiereImpacto)
                return;

            if (seQuedaPulsado && IsPressed)
                return;

            var ing = collision.collider
                .GetComponentInParent<Ingredient>();

            if (!IngredienteValido(ing))
                return;

            impactoValido = false;
            ultimoImpacto = 0f;

            // No permitir que la levitacion
            // active el boton accidentalmente.
            if (!permitirImpactoLevitando &&
                ing.IsLevitating)
                return;

            // Direccion superior local del boton.
            Vector2 arriba = transform.up;

            // Direccion en la que debe viajar el
            // ingrediente para presionar el boton.
            Vector2 direccionPresion = -arriba;

            Vector2 velocidadRelativa =
                collision.relativeVelocity;

            // Velocidad hacia la superficie del boton.
            float velocidadImpacto = Vector2.Dot(
                velocidadRelativa,
                direccionPresion
            );

            ultimoImpacto = Mathf.Max(
                0f, velocidadImpacto
            );

            if (velocidadImpacto <
                velocidadMinimaImpacto)
                return;

            // Comprobar que ha golpeado
            // realmente la cara superior.
            if (requiereImpactoSuperior)
            {
                bool contactoSuperior = false;

                for (int i = 0;
                     i < collision.contactCount;
                     i++)
                {
                    ContactPoint2D contacto =
                        collision.GetContact(i);

                    float alineacion = Vector2.Dot(
                        -contacto.normal,
                        arriba
                    );

                    if (alineacion >=
                        alineacionMinimaImpacto)
                    {
                        contactoSuperior = true;
                        break;
                    }
                }

                if (!contactoSuperior)
                    return;
            }

            // El impacto cumple las condiciones.
            impactoValido = true;

            m_Impactaron.Add(ing);

            SetPressed(true);
        }


        void FixedUpdate()
        {
            if (seQuedaPulsado && IsPressed)
                return;

            RevisarQuienEstaEncima();

            if (!requiereImpacto)
            {
                // Comportamiento original.
                SetPressed(m_Encima.Count > 0);
                return;
            }

            // Eliminar ingredientes que ya
            // no permanecen sobre el boton.
            m_Fuera.Clear();

            foreach (var ing in m_Impactaron)
            {
                if (!IngredienteValido(ing) ||
                    !m_Encima.Contains(ing))
                {
                    m_Fuera.Add(ing);
                }
            }

            foreach (var ing in m_Fuera)
                m_Impactaron.Remove(ing);

            // Solo mantienen pulsado el boton
            // quienes lo activaron correctamente.
            SetPressed(m_Impactaron.Count > 0);
        }


        void RevisarQuienEstaEncima()
        {
            m_Encima.Clear();

            if (m_Col == null || !m_Col.enabled)
                return;

            Bounds b = m_Col.bounds;

            int n = Physics2D.OverlapBox(
                b.center,
                b.size,
                0f,
                m_Filtro,
                s_Buf
            );

            for (int i = 0; i < n; i++)
            {
                if (s_Buf[i] == null)
                    continue;

                var ing = s_Buf[i]
                    .GetComponentInParent<Ingredient>();

                if (IngredienteValido(ing))
                    m_Encima.Add(ing);
            }
        }


        void Update()
        {
            float hundir = spritePresionado != null
                ? 0f
                : hundimiento;

            Vector3 objetivo = IsPressed
                ? m_PosArriba +
                  new Vector3(0f, -hundir, 0f)
                : m_PosArriba;

            if ((transform.position - objetivo)
                .sqrMagnitude > 0.0000001f)
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    objetivo,
                    1f - Mathf.Exp(
                        -velocidadHundido *
                        Time.unscaledDeltaTime
                    )
                );
            }
        }


        void PonerSpriteApoyado(Sprite nuevo)
        {
            if (m_Sr == null ||
                nuevo == null ||
                m_Sr.sprite == nuevo)
                return;

            float base0 = m_Sr.bounds.min.y;

            m_Sr.sprite = nuevo;

            float dy =
                base0 - m_Sr.bounds.min.y;

            transform.position +=
                new Vector3(0f, dy, 0f);

            m_PosArriba +=
                new Vector3(0f, dy, 0f);
        }


        void SetPressed(bool pressed)
        {
            if (seQuedaPulsado && IsPressed)
                return;

            if (IsPressed == pressed)
                return;

            IsPressed = pressed;

            foreach (var g in gates)
            {
                if (g == null)
                    continue;

                if (pressed)
                    g.Open();
                else
                    g.Close();
            }

            if (m_Sr != null)
            {
                if (spritePresionado != null)
                {
                    PonerSpriteApoyado(
                        pressed
                        ? spritePresionado
                        : m_SpriteNormal
                    );

                    m_Sr.color = Color.white;
                }
                else
                {
                    m_Sr.color = pressed
                        ? PlaceholderArt.ButtonDone
                        : Color.white;
                }
            }

            if (!pressed)
                return;

            AudioDirector.Reproducir(Sonido.Boton);

            if (!m_AvisoDado &&
                !string.IsNullOrEmpty(aviso))
            {
                m_AvisoDado = true;

                GameManager.Instance?.ShowHint(aviso);
            }
        }
    }
}
