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
            int start = (int)editor.GetSelectionStart();
            int end = (int)editor.GetSelectionEnd();
            
            int prefixLen = prefix.Length;
            int suffixLen = suffix.Length;
            int docLength = editor.GetTextLength();

            string selectedText = editor.GetSelText();
            
            // Case A: The selection INCLUDES the tags
            if (selectedText.Length >= prefixLen + suffixLen && 
                selectedText.StartsWith(prefix) && selectedText.EndsWith(suffix))
            {
                editor.BeginUndoAction();
                editor.SetSel(end - suffixLen, end);
                editor.ReplaceSel("");
                editor.SetSel(start, start + prefixLen);
                editor.ReplaceSel("");
                editor.SetSel(start, end - prefixLen - suffixLen);
                editor.EndUndoAction();
                return;
            }

            // Case B: The selection is INSIDE the tags
            bool surrounded = false;
            if (start >= prefixLen && (docLength - end) >= suffixLen)
            {
                surrounded = true;
                for (int i = 0; i < prefixLen; i++)
                {
                    if (editor.GetCharAt(start - prefixLen + i) != prefix[i])
                    {
                        surrounded = false;
                        break;
                    }
                }
                if (surrounded)
                {
                    for (int i = 0; i < suffixLen; i++)
                    {
                        if (editor.GetCharAt(end + i) != suffix[i])
                        {
                            surrounded = false;
                            break;
                        }
                    }
                }
            }

            if (surrounded)
            {
                editor.BeginUndoAction();
                editor.SetSel(end, end + suffixLen);
                editor.ReplaceSel("");
                editor.SetSel(start - prefixLen, start);
                editor.ReplaceSel("");
                editor.SetSel(start - prefixLen, end - prefixLen);
                editor.EndUndoAction();
                return;
            }

            // Default: Apply tags
            editor.BeginUndoAction();
            editor.InsertText(end, suffix);
            editor.InsertText(start, prefix);
            editor.SetSel(start + prefixLen, end + prefixLen);
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
            
            // If the selection ends exactly at the start of a line (e.g. they selected one whole line including newline)
            // we don't want to prefix the empty line at the end unless it's the only line.
            if (endLine > startLine && editor.PositionFromLine(endLine) == end)
            {
                endLine--;
            }

            editor.BeginUndoAction();
            
            for (int i = endLine; i >= startLine; i--)
            {
                int lineStart = editor.PositionFromLine(i);
                int lineLength = editor.LineLength(i);
                
                bool startsWithPrefix = true;
                if (lineLength < prefix.Length) {
                    startsWithPrefix = false;
                } else {
                    for (int j = 0; j < prefix.Length; j++)
                    {
                        if (editor.GetCharAt(lineStart + j) != prefix[j])
                        {
                            startsWithPrefix = false;
                            break;
                        }
                    }
                }
                
                if (startsWithPrefix)
                {
                    // Remove prefix
                    editor.SetSel(lineStart, lineStart + prefix.Length);
                    editor.ReplaceSel("");
                }
                else
                {
                    // Insert prefix
                    editor.InsertText(lineStart, prefix);
                }
            }
            
            // Adjust selection to span the modified lines
            int newStart = editor.PositionFromLine(startLine);
            int newEndLineStart = editor.PositionFromLine(endLine);
            int newEnd = newEndLineStart + editor.LineLength(endLine);
            // Ignore trailing newlines from LineLength
            while (newEnd > newStart && (editor.GetCharAt(newEnd - 1) == '\r' || editor.GetCharAt(newEnd - 1) == '\n'))
                newEnd--;
            
            editor.SetSel(newStart, newEnd);
            
            editor.EndUndoAction();
        }
    }
}