using System.Numerics;
using IncrementalResearch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IncrementalResearch.Ui;

// Confirmation before a paradigm shift: shows the Meta-Insight gained and what is reset and kept.
internal static class ShiftDialog
{
    public static void Show(BigInteger gain, string schoolName, System.Action onConfirm)
    {
        var dialog = (InfoDialogScreen)GameScreenManager.Instance.StartScreen(
            ScreenPrefabs.Instance.InfoDialogScreen.gameObject,
            GameScreenManager.Instance.ssOverlayCanvas.gameObject);
        BringToFront(dialog);
        var body = string.Format(STRINGS.UI.PARADIGM.CONFIRM_GAIN, NumberFormat.FormatWhole(gain)) + "\n\n" +
                   STRINGS.UI.PARADIGM.CONFIRM_RESET + "\n" + STRINGS.UI.PARADIGM.CONFIRM_KEEP;
        if (schoolName != null)
        {
            body += "\n\n" + string.Format(STRINGS.UI.PARADIGM.CONFIRM_SCHOOL, schoolName);
        }

        dialog.SetHeader(STRINGS.UI.PARADIGM.CONFIRM_TITLE)
            .AddPlainText(body)
            .AddOption(global::STRINGS.UI.CONFIRMDIALOG.CANCEL, d => d.Deactivate())
            .AddOption(STRINGS.UI.PARADIGM.CONFIRM_OK, d =>
            {
                d.Deactivate();
                onConfirm();
            }, rightSide: true);
    }

    // The dialog is opened from inside the research screen, which would otherwise be drawn over it.
    private static void BringToFront(InfoDialogScreen dialog)
    {
        dialog.transform.SetAsLastSibling();
        var canvas = dialog.GetComponent<Canvas>() ?? dialog.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;
        if (dialog.GetComponent<GraphicRaycaster>() == null)
        {
            dialog.gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}
