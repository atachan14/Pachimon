using System;
using Pachimon.App;

internal static class NewGameRequestCheck
{
    private static void Main()
    {
        Expect(NewGameRequest.ConsumePlayerName(), "ゲスト");
        NewGameRequest.Prepare("   ");
        Expect(NewGameRequest.ConsumePlayerName(), "ゲスト");
        NewGameRequest.Prepare("  タクヤ  ");
        Expect(NewGameRequest.ConsumePlayerName(), "タクヤ");
        Expect(NewGameRequest.ConsumePlayerName(), "ゲスト");
        Console.WriteLine("NewGameRequest checks passed.");
    }

    private static void Expect(string actual, string expected)
    {
        if (actual != expected) throw new Exception($"Expected '{expected}', got '{actual}'.");
    }
}
