

namespace IncrementalResearch.Ui;

// The short guided introduction: one dialog per stage, shown as the player reaches each milestone.
internal static class TutorialDialog
{
    public const int StageCount = 3;

    public static void Show(int stage, System.Action onClosed)
    {
        string header;
        string body;
        switch (stage)
        {
            case 0:
                header = STRINGS.UI.TUTORIAL.STAGE1_TITLE;
                body = STRINGS.UI.TUTORIAL.STAGE1_BODY;
                break;
            case 1:
                header = STRINGS.UI.TUTORIAL.STAGE2_TITLE;
                body = STRINGS.UI.TUTORIAL.STAGE2_BODY;
                break;
            default:
                header = STRINGS.UI.TUTORIAL.STAGE3_TITLE;
                body = STRINGS.UI.TUTORIAL.STAGE3_BODY;
                break;
        }

        var dialog = (InfoDialogScreen)GameScreenManager.Instance.StartScreen(
            ScreenPrefabs.Instance.InfoDialogScreen.gameObject,
            GameScreenManager.Instance.ssOverlayCanvas.gameObject);
        dialog.onDeactivateFn = onClosed;
        dialog.SetHeader(header).AddPlainText(body).AddDefaultOK();
    }
}
