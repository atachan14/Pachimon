namespace Pachimon.App
{
    public static class NewGameRequest
    {
        public const string GuestPlayerName = "ゲスト";

        private static string _pendingPlayerName;

        public static void Prepare(string playerName)
        {
            _pendingPlayerName = NormalizePlayerName(playerName);
        }

        public static string ConsumePlayerName()
        {
            var playerName = NormalizePlayerName(_pendingPlayerName);
            _pendingPlayerName = null;
            return playerName;
        }

        public static string NormalizePlayerName(string playerName)
        {
            var trimmedName = playerName?.Trim();
            return string.IsNullOrEmpty(trimmedName) ? GuestPlayerName : trimmedName;
        }
    }
}
