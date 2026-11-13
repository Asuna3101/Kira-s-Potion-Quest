using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Chocoflan.KiraPotionQuest.EditorTools
{
    /// <summary>
    /// Hornea la pantalla de seleccion de niveles en la escena.
    ///
    /// "Hornear" quiere decir que los objetos quedan creados de verdad en la
    /// jerarquia, no generados al dar Play: asi se ven en el editor y se pueden
    /// mover, escalar y cambiar a mano, que es como trabaja el equipo de arte.
    ///
    /// Se usa cada vez que cambie el arte del pergamino, la lista de niveles o
    /// las vistas previas.
    /// </summary>
    public static class LevelSelectTools
    {
        const string Escena = "Assets/Game/Scenes/LevelSelect.unity";

        [MenuItem("Chocoflan/10 - Hornear selector de niveles", false, 18)]
        public static void Hornear()
        {
            LevelSelectController ctrl = Preparar();
            if (ctrl == null) return;

            Limpiar(ctrl, false);

            // Construir() es privado a proposito: lo llama el propio componente al
            // arrancar. Aqui se invoca por reflexion para no abrirlo a todo el mundo.
            var metodo = typeof(LevelSelectController).GetMethod("Construir",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (metodo == null)
            {
                Debug.LogError("[Chocoflan] No encontre LevelSelectController.Construir().");
                return;
            }
            metodo.Invoke(ctrl, null);

            Marcar(ctrl);
            Debug.Log("[Chocoflan] Selector horneado con " + ctrl.niveles.Count +
                      " niveles. Ya se ve en la Hierarchy y se puede mover a mano.");
        }

        [MenuItem("Chocoflan/11 - Limpiar selector de la escena", false, 19)]
        public static void LimpiarMenu()
        {
            LevelSelectController ctrl = Preparar();
            if (ctrl != null) Limpiar(ctrl, true);
        }

        static LevelSelectController Preparar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != Escena)
                EditorSceneManager.OpenScene(Escena, OpenSceneMode.Single);

            var ctrl = Object.FindFirstObjectByType<LevelSelectController>();
            if (ctrl == null)
                Debug.LogError("[Chocoflan] La escena LevelSelect no tiene LevelSelectController.");
            return ctrl;
        }

        static void Limpiar(LevelSelectController ctrl, bool avisar)
        {
            int borrados = 0;
            for (int i = ctrl.transform.childCount - 1; i >= 0; i--)
            {
                Transform hijo = ctrl.transform.GetChild(i);
                if (hijo.name != LevelSelectController.NombreCanvas) continue;
                Object.DestroyImmediate(hijo.gameObject);
                borrados++;
            }
            if (avisar)
            {
                Marcar(ctrl);
                Debug.Log("[Chocoflan] Canvas del selector borrado (" + borrados + ").");
            }
        }

        static void Marcar(LevelSelectController ctrl)
        {
            EditorUtility.SetDirty(ctrl);
            EditorSceneManager.MarkSceneDirty(ctrl.gameObject.scene);
            EditorSceneManager.SaveScene(ctrl.gameObject.scene);
        }
    }
}
