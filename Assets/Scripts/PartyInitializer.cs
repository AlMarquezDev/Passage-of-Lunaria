using System.Collections;
using UnityEngine;

public class PartyInitializer : MonoBehaviour
{
    private IEnumerator Start()
    {
        // Esperar 1 frame para que todo esté cargado
        yield return null;

        if (!GameLoadContext.IsLoadingFromSave || GameLoadContext.HasGameFinishedLoading)
        {
            PartyPanelUI.Instance?.GenerateRows();
            // CursorController.Instance?.ResetCursor(); // REMOVED OR COMMENTED OUT
        }
    }
}