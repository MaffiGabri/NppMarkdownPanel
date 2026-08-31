using System;
using System.Windows.Forms;
using Kbg.NppPluginNET;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace NppMarkdownPanel
{
    /// <summary>
    /// Provides Markdown text formatting commands via the Scintilla Gateway.
    /// </summary>
    public static class MarkdownEditingCommands
    {
        private static IScintillaGateway GetEditor()
        {
            return new ScintillaGateway(PluginBase.GetCurrentScintilla());
        }

        // --- TEXT FORMATTING ---
        public static void ToggleBold() => WrapSelection("**", "**");
        public static void ToggleItalic() => WrapSelection("*", "*");
        public static void ToggleStrikethrough() => WrapSelection("~~", "~~");
        public static void ToggleUnderline() => WrapSelection("<u>", "</u>");
        public static void ToggleHighlight() => WrapSelection("==", "==");

        // --- HEADINGS AND BLOCKS ---
        public static void SetHeading1() => PrefixLines("# ");
        public static void SetHeading2() => PrefixLines("## ");
        public static void ToggleBlockquote() => PrefixLines("> ");
        
        // --- LISTS ---
        public static void InsertList() => PrefixLines("- ");
        public static void InsertTaskEmpty() => PrefixLines("- [ ] ");
        public static void InsertTaskCompleted() => PrefixLines("- [x] ");

        // --- MEDIA ---
        public static void InsertLink() => WrapSelection("[", "]()");
        public static void InsertImage() => WrapSelection("![", "]()");
        public static void InsertEmptyImage() => GetEditor().ReplaceSel("![Description](Image_URL)");

        // --- STRUCTURAL ---
        public static void InsertSeparator() => GetEditor().ReplaceSel("\r\n---\r\n");
        public static void InsertTable3x2()
        {
            string table = "\r\n| Header 1 | Header 2 | Header 3 |\r\n| --- | --- | --- |\r\n| Cell 1 | Cell 2 | Cell 3 |\r\n";
            GetEditor().ReplaceSel(table);
        }

        /// <summary>
        /// Wraps the current selection with a prefix and suffix.
        /// </summary>
        private static void WrapSelection(string prefix, string suffix)
        {
            var editor = GetEditor();
            // Cast to int in case of older Scintilla wrappers returning a Position struct
            int start = (int)editor.GetSelectionStart();
            int end = (int)editor.GetSelectionEnd();

            editor.BeginUndoAction();
            
            // Insert suffix first (at the end) so the 'start' position index doesn't shift!
            editor.InsertText(end, suffix);
            // Then insert prefix
            editor.InsertText(start, prefix);
            
            // Restore selection around the wrapped text
            editor.SetSel(start + prefix.Length, end + prefix.Length);
            
            editor.EndUndoAction();
        }

        /// <summary>
        /// Inserts a prefix at the beginning of each selected line.
        /// </summary>
        private static void PrefixLines(string prefix)
        {
            var editor = GetEditor();
            int start = (int)editor.GetSelectionStart();
            int end = (int)editor.GetSelectionEnd();
            
            int startLine = editor.LineFromPosition(start);
            int endLine = editor.LineFromPosition(end);

            editor.BeginUndoAction();
            
            // Iterate backwards! 
            // If we insert text top-down, the starting positions of subsequent lines would shift.
            // By going bottom-up, line starting positions remain completely stable.
            for (int i = endLine; i >= startLine; i--)
            {
                int lineStart = editor.PositionFromLine(i);
                editor.InsertText(lineStart, prefix);
            }
            
            editor.EndUndoAction();
        }
    }
}