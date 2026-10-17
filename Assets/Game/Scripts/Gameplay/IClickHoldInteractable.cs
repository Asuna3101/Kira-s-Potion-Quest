namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Control "Interactuar: click y mantener" del GDD.
    /// Lo implementan los elementos del tablero con los que se puede interactuar
    /// directamente con el mouse (palanca, caldero, portales).
    /// </summary>
    public interface IClickHoldInteractable
    {
        /// <summary>Tiempo que hay que mantener pulsado para completar la interaccion.</summary>
        float HoldSeconds { get; }

        /// <summary>Progreso 0..1 mientras se mantiene el click.</summary>
        void OnHoldProgress(float normalized);

        /// <summary>Se completo el tiempo de mantenido.</summary>
        void OnHoldCompleted();

        /// <summary>Se solto el click antes de completar.</summary>
        void OnHoldCancelled();
    }
}
