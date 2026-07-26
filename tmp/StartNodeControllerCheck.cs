using System;
using System.Linq;
using Pachimon.Run;

internal static class StartNodeControllerCheck
{
    private static int _commitCount;
    private static int _completeCount;

    private static int Main()
    {
        var candidates = Enumerable.Range(1, 9).Select(index => $"p{index}").ToArray();
        var controller = new StartNodeController(
            candidates,
            3,
            StartDialogueData.CreateDefault("Tester"),
            selected =>
            {
                _commitCount++;
                return selected.SequenceEqual(new[] { "p2", "p3", "p4" });
            },
            () => _completeCount++);

        Require(controller.State == StartNodeProgressState.IntroDialogue, "initial state");
        Require(controller.AdvanceIntro(), "advance intro");
        Require(controller.ToggleCandidate("p1"), "select p1");
        Require(controller.ToggleCandidate("p2"), "select p2");
        Require(controller.ToggleCandidate("p1"), "remove p1");
        Require(controller.GetSelectionOrder("p2") == 1, "selection order closes gap");
        Require(controller.ToggleCandidate("p3"), "select p3");
        Require(controller.State == StartNodeProgressState.Selecting, "less than three stays selecting");
        Require(controller.ToggleCandidate("p4"), "select p4");
        Require(controller.State == StartNodeProgressState.SelectionConfirmation, "third opens confirmation");
        Require(controller.RestartSelection(), "restart selection");
        Require(controller.SelectedIds.Count == 0, "restart clears selection");

        controller.ToggleCandidate("p2");
        controller.ToggleCandidate("p3");
        controller.ToggleCandidate("p4");
        Require(controller.ConfirmSelection(), "confirm selection");
        Require(controller.State == StartNodeProgressState.Completed, "completed state");
        Require(!controller.ConfirmSelection(), "second confirmation rejected");
        Require(_commitCount == 1 && _completeCount == 1, "commit and completion happen once");
        return 0;
    }

    private static void Require(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException($"Failed: {label}");
    }
}
