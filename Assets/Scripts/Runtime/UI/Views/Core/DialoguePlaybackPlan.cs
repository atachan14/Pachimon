using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.UI
{
    public sealed class DialoguePlaybackSegment
    {
        public DialoguePlaybackSegment(
            IReadOnlyList<DialogueLine> lines,
            IReadOnlyList<int> startedLineIndices,
            int revealFromLineIndex)
        {
            Lines = lines;
            StartedLineIndices = startedLineIndices;
            RevealFromLineIndex = revealFromLineIndex;
            Text = string.Join("\n", lines.Select(line => line.Text));
        }

        public IReadOnlyList<DialogueLine> Lines { get; }
        public IReadOnlyList<int> StartedLineIndices { get; }
        public int RevealFromLineIndex { get; }
        public string Text { get; }
    }

    public static class DialoguePlaybackPlan
    {
        public static IReadOnlyList<DialoguePlaybackSegment> Create(
            DialoguePage page,
            int visibleLineCount)
        {
            return Create(page, visibleLineCount, null);
        }

        public static IReadOnlyList<DialoguePlaybackSegment> Create(
            DialoguePage page,
            int visibleLineCount,
            Func<DialogueLine, int> getVisibleRowCount)
        {
            if (page == null)
            {
                return Array.Empty<DialoguePlaybackSegment>();
            }

            var capacity = Math.Max(1, visibleLineCount);
            int GetRowCount(DialogueLine line) => Math.Max(
                1,
                getVisibleRowCount?.Invoke(line) ?? 1);
            var segments = new List<DialoguePlaybackSegment>();
            var visibleHistory = new List<DialogueLine>(capacity);
            foreach (var block in page.Blocks)
            {
                if (block?.Lines == null || block.Lines.Count == 0)
                {
                    continue;
                }

                if (block.Lines.Sum(GetRowCount) <= capacity)
                {
                    var visibleLines = TrimToVisibleRows(
                        visibleHistory.Concat(block.Lines),
                        capacity,
                        GetRowCount);
                    var firstNewLine = visibleLines.Length - block.Lines.Count;
                    segments.Add(new DialoguePlaybackSegment(
                        visibleLines,
                        Enumerable.Range(firstNewLine, block.Lines.Count)
                            .ToArray(),
                        firstNewLine));
                    ReplaceHistory(visibleHistory, visibleLines);
                    continue;
                }

                // Fill the window once, then reveal one semantic line per advance.
                for (var lineIndex = 0; lineIndex < block.Lines.Count;)
                {
                    var addedLineCount = lineIndex == 0
                        ? CountLinesThatFit(
                            block.Lines,
                            capacity,
                            GetRowCount)
                        : 1;
                    var addedLines = block.Lines
                        .Skip(lineIndex)
                        .Take(addedLineCount)
                        .ToArray();
                    var visibleLines = TrimToVisibleRows(
                        visibleHistory.Concat(addedLines),
                        capacity,
                        GetRowCount);
                    var firstNewLine = visibleLines.Length - addedLines.Length;
                    segments.Add(new DialoguePlaybackSegment(
                        visibleLines,
                        Enumerable.Range(firstNewLine, addedLines.Length)
                            .ToArray(),
                        firstNewLine));
                    ReplaceHistory(visibleHistory, visibleLines);
                    lineIndex += addedLineCount;
                }
            }

            return segments;
        }

        private static int CountLinesThatFit(
            IReadOnlyList<DialogueLine> lines,
            int capacity,
            Func<DialogueLine, int> getRowCount)
        {
            var rowCount = 0;
            var lineCount = 0;
            foreach (var line in lines)
            {
                var nextRowCount = getRowCount(line);
                if (lineCount > 0 && rowCount + nextRowCount > capacity)
                {
                    break;
                }

                rowCount += nextRowCount;
                lineCount++;
                if (rowCount >= capacity)
                {
                    break;
                }
            }

            return Math.Max(1, lineCount);
        }

        private static DialogueLine[] TrimToVisibleRows(
            IEnumerable<DialogueLine> lines,
            int capacity,
            Func<DialogueLine, int> getRowCount)
        {
            var visibleLines = lines.ToList();
            var rowCount = visibleLines.Sum(getRowCount);
            while (visibleLines.Count > 1 && rowCount > capacity)
            {
                rowCount -= getRowCount(visibleLines[0]);
                visibleLines.RemoveAt(0);
            }

            return visibleLines.ToArray();
        }

        private static void ReplaceHistory(
            ICollection<DialogueLine> history,
            IEnumerable<DialogueLine> lines)
        {
            history.Clear();
            foreach (var line in lines)
            {
                history.Add(line);
            }
        }
    }
}
