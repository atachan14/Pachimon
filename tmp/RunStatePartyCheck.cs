using System;
using System.Linq;
using Pachimon.Run;

internal static class RunStatePartyCheck
{
    private static void Main()
    {
        var state = new RunState(123456, "テスト");
        Expect(!state.IsPartyConfirmed, "party must start unconfirmed");
        Expect(!state.TrySetInitialParty(null), "null must fail");
        Expect(!state.TrySetInitialParty(new[] { "a", "b" }), "two members must fail");
        Expect(!state.TrySetInitialParty(new[] { "a", "a", "b" }), "duplicates must fail");
        Expect(!state.TrySetInitialParty(new[] { "a", " ", "c" }), "blank id must fail");
        Expect(state.TrySetInitialParty(new[] { "third", "first", "second" }), "valid party must succeed");
        Expect(state.IsPartyConfirmed, "party must be confirmed");
        Expect(state.PlayerPachimonIds.SequenceEqual(new[] { "third", "first", "second" }), "selection order must be preserved");
        Expect(!state.TrySetInitialParty(new[] { "x", "y", "z" }), "second confirmation must fail");
        Expect(state.PlayerPachimonIds.SequenceEqual(new[] { "third", "first", "second" }), "failed overwrite must not mutate party");
        Console.WriteLine("RunState party checks passed.");
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
