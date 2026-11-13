using System.Collections.Generic;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Boton de presion (GDD: "se activan al tener contacto con los ingredientes").
    /// Al pisarlo se abre la pared movil que tenga asignada.
    ///
    /// COMO SE AJUSTA
    /// --------------
    /// "Paredes" es la lista de compuertas que abre. "Se queda pulsado" decide si
    /// el boton es de un solo uso (tutorial) o si hay que mantener el ingrediente
    /// encima (puzzle). Todo desde el Inspector.
    ///
    /// QUE SE ARREGLO
    /// --------------
    /// Antes llevaba una CUENTA de contactos. Eso fallaba de dos formas: un
    /// ingrediente con dos colliders contaba doble, y si se rompia estando encima
    /// nunca llegaba el aviso de salida, asi que la cuenta se quedaba trabada en
    /// uno y el boton no se soltaba nunca.
    ///
    /// Ahora se guarda QUIEN esta encima, no cuantos contactos hay, y en cada
    /// paso de fisica se descartan los que ya no valen (rotos, entregados o
    /// destruidos). Tambien se revisa la zona directamente, asi que un ingrediente
    /// que se queda quieto encima sigue contando aunque Unity duerma su cuerpo.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PressurePlate : MonoBehaviour
    {
        [Header("Que abre")]
        [Tooltip("Paredes moviles que abre este boton.")]
        public List<Gate> gates = new List<Gate>();

        [Header("Comportamiento")]
        [Tooltip("Encendido: una vez pisado se queda pulsado para siempre (tutorial). " +
                 "Apagado: hay que mantener un ingrediente encima (puzzle).")]
        public bool seQuedaPulsado = true;

        [Header("Deteccion")]
        [Tooltip("Alto de la zona que detecta al ingrediente, por encima del boton. " +
                 "Mas alto = se activa aunque el ingrediente venga levitando un poco separado.")]
        public float alturaDeteccion = 0.4f;

        [Header("Aspecto")]
        [Tooltip("Cuanto se hunde el boton al pisarlo, en unidades.")]
        public float hundimiento = 0.06f;

        [Tooltip("Velocidad con la que se hunde y vuelve.")]
        public float velocidadHundido = 10f;

        [Header("Aviso")]
        [Tooltip("Mensaje al pisarlo por primera vez. Vacio = sin mensaje.")]
        public string aviso = "La pared se abrió.";

        public bool IsPressed { get; private set; }

        SpriteRenderer m_Sr;
        Collider2D m_Col;
        Vector3 m_PosArriba;
        bool m_AvisoDado;

        readonly HashSet<Ingredient> m_Encima = new HashSet<Ingredient>();
        static readonly List<Ingredient> s_Fuera = new List<Ingredient>();
        ContactFilter2D m_Filtro;
        static readonly Collider2D[] s_Buf = new Collider2D[8];

        void Awake()
        {
            m_Sr = GetComponent<SpriteRenderer>();
            m_Col = BuscarOCrearZona();
            m_PosArriba = transform.position;

            m_Filtro = new ContactFilter2D();
            m_Filtro.NoFilter();
            m_Filtro.useTriggers = false;
        }

        /// <summary>
        /// Devuelve la zona que detecta al ingrediente.
        ///
        /// OJO: el boton suele tener un collider SOLIDO, porque el ingrediente se
        /// apoya encima. Ese no se puede convertir en trigger o el ingrediente lo
        /// atravesaria. Asi que, si no hay ya una zona de deteccion, se crea una
        /// franja justo encima del boton y se deja el solido en paz.
        /// </summary>
        Collider2D BuscarOCrearZona()
        {
            var cols = GetComponents<Collider2D>();
            foreach (var c in cols)
                if (c.isTrigger) return c;

            Vector2 tam = Vector2.one;
            Vector2 off = Vector2.zero;
            if (cols.Length > 0)
            {
                if (cols[0] is BoxCollider2D caja) { tam = caja.size; off = caja.offset; }
                else if (cols[0] is CapsuleCollider2D cap) { tam = cap.size; off = cap.offset; }
            }

            var zona = gameObject.AddComponent<BoxCollider2D>();
            zona.isTrigger = true;
            zona.size = new Vector2(tam.x, tam.y + alturaDeteccion);
            zona.offset = off + new Vector2(0f, alturaDeteccion * 0.5f);
            return zona;
        }

        void FixedUpdate()
        {
            if (seQuedaPulsado && IsPressed) return;   // ya no hay nada que vigilar

            RevisarQuienEstaEncima();
            SetPressed(m_Encima.Count > 0);
        }

        /// <summary>
        /// Se consulta la zona en vez de fiarse solo de los avisos de trigger,
        /// porque a un Rigidbody dormido Unity deja de mandarle OnTriggerStay2D.
        /// </summary>
        void RevisarQuienEstaEncima()
        {
            // 1. fuera los que ya no valen
            s_Fuera.Clear();
            foreach (var ing in m_Encima)
                if (ing == null || ing.IsBroken || ing.IsDelivered) s_Fuera.Add(ing);
            foreach (var ing in s_Fuera) m_Encima.Remove(ing);

            if (m_Col == null || !m_Col.enabled) return;

            // 2. quien esta dentro de la zona ahora mismo
            m_Encima.Clear();
            Bounds b = m_Col.bounds;
            int n = Physics2D.OverlapBox(b.center, b.size, 0f, m_Filtro, s_Buf);
            for (int i = 0; i < n; i++)
            {
                if (s_Buf[i] == null) continue;
                var ing = s_Buf[i].GetComponentInParent<Ingredient>();
                if (ing != null && !ing.IsBroken && !ing.IsDelivered) m_Encima.Add(ing);
            }
        }

        void Update()
        {
            // El boton se hunde al pisarlo. Tiempo real: sigue animando en pausa.
            Vector3 objetivo = IsPressed
                ? m_PosArriba + new Vector3(0f, -hundimiento, 0f)
                : m_PosArriba;

            if ((transform.position - objetivo).sqrMagnitude > 0.0000001f)
                transform.position = Vector3.Lerp(transform.position, objetivo,
                    1f - Mathf.Exp(-velocidadHundido * Time.unscaledDeltaTime));
        }

        void SetPressed(bool pressed)
        {
            if (seQuedaPulsado && IsPressed) return;
            if (IsPressed == pressed) return;
            IsPressed = pressed;

            foreach (var g in gates)
            {
                if (g == null) continue;
                if (pressed) g.Open(); else g.Close();
            }

            if (m_Sr != null)
                m_Sr.color = pressed ? PlaceholderArt.ButtonDone : Color.white;

            if (!pressed) return;

            AudioDirector.Reproducir(Sonido.Boton);

            if (!m_AvisoDado && !string.IsNullOrEmpty(aviso))
            {
                m_AvisoDado = true;
                GameManager.Instance?.ShowHint(aviso);
            }
        }
    }
}
