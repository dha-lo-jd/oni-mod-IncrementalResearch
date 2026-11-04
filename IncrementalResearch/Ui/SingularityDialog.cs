using IncrementalResearch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IncrementalResearch.Ui;

// Confirmation before a singularity: shows the Critical Constant gained, the Data Banks used, and what is reset and kept.
internal static class SingularityDialog
{
    public static void Show(int gain, int databanks, System.Action onConfirm)
    {
        var dialog = (InfoDialogScreen)GameScreenManager.Instance.StartScreen(
            ScreenPrefabs.Instance.InfoDialogScreen.gameObject,
            GameScreenManager.Instance.ssOverlayCanvas.gameObject);
        BringToFront(dialog);
        var body = string.Format(STRINGS.UI.SINGULARITY.CONFIRM_GAIN, gain, databanks) + "\n\n" +
                   STRINGS.UI.SINGULARITY.CONFIRM_RESET + "\n" + STRINGS.UI.SINGULARITY.CONFIRM_KEEP;
        dialog.SetHeader(STRINGS.UI.SINGULARITY.CONFIRM_TITLE)
            .AddPlainText(RubyText.Convert(body))
            .AddOption(global::STRINGS.UI.CONFIRMDIALOG.CANCEL, d => d.Deactivate())
            .AddOption(STRINGS.UI.SINGULARITY.CONFIRM_OK, d =>
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
