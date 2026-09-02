namespace Pachimon.UI
{
    public enum LayoutMode
    {
        Compact,
        Expanded,
    }

    public enum CompactPane
    {
        Main,
        Left,
        Right,
    }

    public static class LayoutModePolicy
    {
        public static LayoutMode Resolve(
            LayoutMode preferredMode,
            bool expandedLayoutSupported)
        {
            return preferredMode == LayoutMode.Expanded
                   && expandedLayoutSupported
                ? LayoutMode.Expanded
                : LayoutMode.Compact;
        }
    }
}
