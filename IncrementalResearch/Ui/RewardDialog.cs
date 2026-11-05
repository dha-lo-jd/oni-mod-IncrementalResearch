namespace IncrementalResearch.Ui;

// The notice shown when a paradigm shift milestone is reached.
internal static class RewardDialog
{
    public static UnityEngine.Object ShowMilestone(int shifts, int attrTickets, int interestTickets)
    {
        var body = string.Format(STRINGS.UI.REWARDS.MILESTONE_BODY, shifts, attrTickets, interestTickets);
        var dialog = (InfoDialogScreen)GameScreenManager.Instance.StartScreen(
            ScreenPrefabs.Instance.InfoDialogScreen.gameObject,
            GameScreenManager.Instance.ssOverlayCanvas.gameObject);
        dialog.SetHeader(STRINGS.UI.REWARDS.MILESTONE_TITLE).AddPlainText(RubyText.Convert(body)).AddDefaultOK();
        return dialog;
    }
}
