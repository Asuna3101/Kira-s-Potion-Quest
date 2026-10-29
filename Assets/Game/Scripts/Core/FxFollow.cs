using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Hace que un efecto (llama de una vela, humo del caldero) siga a la pieza a la
    /// que pertenece. Asi, si el equipo mueve o cambia el tamano de la vela en la escena,
    /// el efecto viaja con ella. No hay que configurar nada a mano.
    /// </summary>
    public class FxFollow : MonoBehaviour
    {
        [Tooltip("Pieza que sigue este efecto (la vela, el caldero...).")]
        public Transform target;

        [Tooltip("Donde va el efecto respecto a la pieza, en unidades de la propia pieza.")]
        public Vector2 localOffset;

        [Tooltip("Escala que tenia la pieza al crearse. Si cambias su tamano, el efecto se ajusta.")]
        public Vector3 referenceScale = Vector3.one;

        [Tooltip("Escala propia del efecto (1 = normal).")]
        public float baseScale = 1f;

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 p = target.TransformPoint(new Vector3(localOffset.x, localOffset.y, 0f));
            transform.position = new Vector3(p.x, p.y, transform.position.z);

            float rx = Mathf.Approximately(referenceScale.x, 0f) ? 1f : target.localScale.x / referenceScale.x;
            float ry = Mathf.Approximately(referenceScale.y, 0f) ? 1f : target.localScale.y / referenceScale.y;
            float s = baseScale * Mathf.Sqrt(Mathf.Abs(rx * ry));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
