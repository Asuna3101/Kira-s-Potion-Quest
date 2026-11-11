using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Destellos magicos alrededor del ingrediente seleccionado.
    ///
    /// Sustituye al aro de seleccion, que tapaba el dibujo. En vez de una forma
    /// solida encima, van saliendo chispitas pequenas alrededor del ingrediente:
    /// se ve cual esta elegido sin esconder el arte de Melanie.
    ///
    /// Lo lleva el mismo objeto que el LevitationController y usa el sistema de
    /// particulas del proyecto (FxParticle), igual que la varita y el caldero.
    /// </summary>
    [RequireComponent(typeof(LevitationController))]
    public class SelectionSparkles : MonoBehaviour
    {
        [Header("Ritmo")]
        [Tooltip("Segundos entre chispas con el ingrediente quieto.")]
        public float intervalo = 0.08f;

        [Tooltip("Segundos entre chispas mientras levita. Mas bajo = mas chispas.")]
        public float intervaloLevitando = 0.04f;

        [Header("Forma")]
        [Tooltip("Radio del anillo por el que aparecen, en unidades.")]
        public float radio = 0.44f;

        [Tooltip("Variacion del radio, para que no salgan todas a la misma distancia.")]
        public float variacionRadio = 0.12f;

        [Tooltip("Tamano de cada chispa en unidades.")]
        public float tamano = 0.19f;

        [Tooltip("Cuanto duran en pantalla.")]
        public float duracion = 0.65f;

        [Header("Color")]
        public Color color = new Color(1f, 0.93f, 0.60f, 1f);

        [Tooltip("Color mientras levita, para que se note que el hechizo esta activo.")]
        public Color colorLevitando = new Color(0.70f, 0.92f, 1f, 1f);

        LevitationController m_Levitation;
        float m_Timer;
        float m_Angulo;

        void Awake()
        {
            m_Levitation = GetComponent<LevitationController>();
            m_Angulo = Random.Range(0f, 360f);
        }

        void Update()
        {
            if (m_Levitation == null) return;

            Ingredient sel = m_Levitation.Selected;
            if (sel == null || sel.IsBroken || sel.IsDelivered) return;

            var gm = GameManager.Instance;
            if (gm != null && gm.State != LevelState.Playing) return;

            bool levitando = m_Levitation.IsLevitating;

            m_Timer -= Time.deltaTime;
            if (m_Timer > 0f) return;
            m_Timer = Mathf.Max(0.02f, levitando ? intervaloLevitando : intervalo);

            // Las chispas salen girando alrededor del ingrediente: el angulo avanza
            // en un numero feo a proposito para que no se repita el mismo patron.
            m_Angulo += levitando ? 137.5f : 97.3f;
            float rad = m_Angulo * Mathf.Deg2Rad;
            float r = radio + Random.Range(-variacionRadio, variacionRadio);

            Vector2 centro = sel.transform.position;
            Vector2 punto = centro + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;

            // Suben flotando y hacia fuera, muy suave.
            Vector2 velocidad = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 0.25f
                                + new Vector2(0f, Random.Range(0.1f, 0.35f));

            FxParticle.Spawn(
                transform.parent,
                FxSprites.Glow,
                punto,
                velocidad,
                0f,                       // sin gravedad: flotan
                1.2f,                     // se van frenando
                duracion * Random.Range(0.8f, 1.2f),
                tamano * Random.Range(0.75f, 1.25f),
                tamano * 0.25f,           // no se apagan del todo: se ven hasta el final
                levitando ? colorLevitando : color,
                Random.Range(-90f, 90f),
                0f,
                "FX",
                6,
                0.15f);                   // vaiven lateral
        }
    }
}
