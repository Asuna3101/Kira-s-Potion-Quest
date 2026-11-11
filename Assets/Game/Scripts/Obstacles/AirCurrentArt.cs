using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Anima la corriente de aire alternando los dibujos de Melanie
    /// (Sprites/Tablero/Obstacles/aires.PNG y aires2.PNG).
    ///
    /// Son dos dibujos casi iguales del mismo remolino: al cambiar de uno a otro
    /// el aire parece moverse, como en una animacion de dos fotogramas.
    ///
    /// COMO SE CAMBIA EL ARTE
    /// ----------------------
    /// Se arrastran los PNG a la lista "Fotogramas" en el Inspector. Admite dos o
    /// mas; si solo hay uno, se queda quieto. No hay que tocar codigo.
    ///
    /// El dibujo se REPITE a lo alto (modo Tiled) en vez de estirarse, asi que el
    /// remolino conserva su grosor aunque la columna sea muy larga.
    ///
    /// OJO: este objeto lleva tambien el trigger que empuja al ingrediente
    /// (AirCurrent). Aqui solo se toca el dibujo, nunca el collider ni el
    /// transform, para no mover la zona de empuje.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class AirCurrentArt : MonoBehaviour
    {
        [Tooltip("Dibujos que se van alternando. Normalmente aires.PNG y aires2.PNG.")]
        public Sprite[] fotogramas;

        [Tooltip("Cambios de dibujo por segundo.")]
        [Range(0.5f, 24f)]
        public float fotogramasPorSegundo = 6f;

        [Tooltip("Cada corriente empieza en un punto distinto de la animacion, para que no vayan todas a la vez.")]
        public bool desfaseAleatorio = true;

        SpriteRenderer m_Sr;
        float m_T;
        int m_Actual = -1;

        void Awake()
        {
            m_Sr = GetComponent<SpriteRenderer>();
            if (desfaseAleatorio) m_T = Random.value * 10f;
        }

        void Update()
        {
            if (m_Sr == null || fotogramas == null || fotogramas.Length == 0) return;

            // Tiempo real: la corriente sigue animandose aunque el juego se pause
            // al ganar o perder. Si prefieres que se congele, cambia a deltaTime.
            m_T += Time.deltaTime;

            int i = fotogramas.Length == 1
                ? 0
                : Mathf.Abs((int)(m_T * fotogramasPorSegundo)) % fotogramas.Length;

            if (i == m_Actual) return;
            m_Actual = i;

            if (fotogramas[i] != null) m_Sr.sprite = fotogramas[i];
        }
    }
}
