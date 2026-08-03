using System;
using System.Collections.Generic;
using System.Linq;

namespace Pachimon.UI
{
    public sealed class DialogueLine
    {
        public DialogueLine(string text, Action onStarted = null)
        {
            Text = text ?? string.Empty;
            OnStarted = onStarted;
        }

        public string Text { get; }
        public Action OnStarted { get; }
    }

    public sealed class DialogueBlock
    {
        public DialogueBlock(IEnumerable<DialogueLine> lines)
        {
            Lines = lines?
                .Where(line => line != null)
                .ToArray()
                ?? Array.Empty<DialogueLine>();
        }

        public IReadOnlyList<DialogueLine> Lines { get; }
    }

    public sealed class DialoguePage
    {
        public DialoguePage(IEnumerable<DialogueBlock> blocks)
        {
            Blocks = blocks?
                .Where(block => block != null && block.Lines.Count > 0)
                .ToArray()
                ?? Array.Empty<DialogueBlock>();
        }

        public IReadOnlyList<DialogueBlock> Blocks { get; }
    }
}
