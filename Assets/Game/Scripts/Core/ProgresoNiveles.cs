using UnityEngine;

namespace Chocoflan.KiraPotionQuest
{
    /// <summary>
    /// Progreso guardado: que niveles ya se superaron.
    ///
    /// Se apoya en PlayerPrefs (tambien funciona en la build de WebGL / itch).
    /// Un nivel esta DESBLOQUEADO si es el primero de la lista o si el anterior
    /// ya se supero. Esto solo lo consulta la pantalla de seleccion: al ganar un
    /// nivel el boton NEXT sigue llevando al siguiente de inmediato.
    /// </summary>
    public static class ProgresoNiveles
    {
        const string Prefijo = "Chocoflan.NivelSuperado.";

        public static bool EstaSuperado(string escena)
        {
            return !string.IsNullOrEmpty(escena) && PlayerPrefs.GetInt(Prefijo + escena, 0) == 1;
        }

        public static void MarcarSuperado(string escena)
        {
            if (string.IsNullOrEmpty(escena)) return;
            if (EstaSuperado(escena)) return;
            PlayerPrefs.SetInt(Prefijo + escena, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Borra el progreso de las escenas indicadas (para pruebas o "nueva partida").</summary>
        public static void Borrar(System.Collections.Generic.IEnumerable<string> escenas)
        {
            foreach (string e in escenas)
                if (!string.IsNullOrEmpty(e)) PlayerPrefs.DeleteKey(Prefijo + e);
            PlayerPrefs.Save();
        }

        public static void Marcar(System.Collections.Generic.IEnumerable<string> escenas)
        {
            foreach (string e in escenas)
                if (!string.IsNullOrEmpty(e)) PlayerPrefs.SetInt(Prefijo + e, 1);
            PlayerPrefs.Save();
        }
    }
}
