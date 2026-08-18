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
            if (page == null)
            {
                return Array.Empty<DialoguePlaybackSegment>();
            }

            var capacity = Math.Max(1, visibleLineCount);
            var segments = new List<DialoguePlaybackSegment>();
            var visibleHistory = new List<DialogueLine>(capacity);
            foreach (var block in page.Blocks)
            {
                if (block?.Lines == null || block.Lines.Count == 0)
                {
                    continue;
                }

                if (block.Lines.Count <= capacity)
                {
                    var visibleLines = visibleHistory
                        .Concat(block.Lines)
                        .TakeLast(capacity)
                        .ToArray();
                    var firstNewLine = visibleLines.Length - block.Lines.Count;
                    segments.Add(new DialoguePlaybackSegment(
                        visibleLines,
                        Enumerable.Range(firstNewLine, block.Lines.Count)
                            .ToArray(),
                        firstNewLine));
                    ReplaceHistory(visibleHistory, visibleLines);
                    continue;
                }

                // Fill the window once, then reveal one new line per advance.
                for (var lineIndex = 0;
                    lineIndex < block.Lines.Count;
                    lineIndex += lineIndex == 0 ? capacity : 1)
                {
                    var addedLineCount = lineIndex == 0 ? capacity : 1;
                    var addedLines = block.Lines
                        .Skip(lineIndex)
                        .Take(addedLineCount)
                        .ToArray();
                    var visibleLines = visibleHistory
                        .Concat(addedLines)
                        .TakeLast(capacity)
                        .ToArray();
                    var firstNewLine = visibleLines.Length - addedLines.Length;
                    segments.Add(new DialoguePlaybackSegment(
                        visibleLines,
                        Enumerable.Range(firstNewLine, addedLines.Length)
                            .ToArray(),
                        firstNewLine));
                    ReplaceHistory(visibleHistory, visibleLines);
                }
            }

            return segments;
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
