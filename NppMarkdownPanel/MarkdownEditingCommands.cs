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
        public static void InsertLink()
        {
            var editor = GetEditor();
            int sel1 = (int)editor.GetSelectionStart();
            int sel2 = (int)editor.GetSelectionEnd();
            int start = Math.Min(sel1, sel2);
            int end = Math.Max(sel1, sel2);
            
            editor.BeginUndoAction();
            
            string linkText = "text";
            string url = "https://";

            if (start != end)
            {
                editor.SetSel(start, end);
                string selectedText = editor.GetSelText();
                if (selectedText.StartsWith("http://") || selectedText.StartsWith("https://") || selectedText.StartsWith("www."))
                {
                    url = selectedText;
                }
                else
                {
                    linkText = selectedText;
                }
                editor.DeleteRange(start, end - start);
            }
            
            string result = $"[{linkText}]({url})";
            editor.InsertText(start, result);
            
            if (url == "https://")
            {
                int urlStart = start + linkText.Length + 3;
                editor.SetSel(urlStart, urlStart + url.Length);
            }
            else
            {
                int textStart = start + 1;
                editor.SetSel(textStart, textStart + linkText.Length);
            }
            
            editor.EndUndoAction();
        }

        public static bool IsWrapActive(string prefix, string suffix)
        {
            var editor = GetEditor();
            int sel1 = (int)editor.GetSelectionStart();
            int sel2 = (int)editor.GetSelectionEnd();
            int start = Math.Min(sel1, sel2);
            int end = Math.Max(sel1, sel2);
            
            int prefixLen = prefix.Length;
            int suffixLen = suffix.Length;
            int docLength = editor.GetTextLength();

            int selLength = end - start;
            if (selLength >= prefixLen + suffixLen)
            {
                bool includesTags = true;
                for (int i = 0; i < prefixLen; i++)
                    if (editor.GetCharAt(start + i) != prefix[i]) { includesTags = false; break; }
                if (includesTags)
                    for (int i = 0; i < suffixLen; i++)
                        if (editor.GetCharAt(end - suffixLen + i) != suffix[i]) { includesTags = false; break; }
                if (includesTags) return true;
            }

            if (start >= prefixLen && (docLength - end) >= suffixLen)
            {
                bool surrounded = true;
                for (int i = 0; i < prefixLen; i++)
                    if (editor.GetCharAt(start - prefixLen + i) != prefix[i]) { surrounded = false; break; }
                if (surrounded)
                    for (int i = 0; i < suffixLen; i++)
                        if (editor.GetCharAt(end + i) != suffix[i]) { surrounded = false; break; }
                if (surrounded) return true;
            }
            
            return false;
        }

        public static bool IsPrefixActive(string prefix)
        {
            var editor = GetEditor();
            int sel1 = (int)editor.GetSelectionStart();
            int sel2 = (int)editor.GetSelectionEnd();
            int start = Math.Min(sel1, sel2);
            int end = Math.Max(sel1, sel2);
            
            int startLine = editor.LineFromPosition(start);
            int endLine = editor.LineFromPosition(end);
            if (endLine > startLine && editor.PositionFromLine(endLine) == end) endLine--;
            
            for (int i = endLine; i >= startLine; i--)
            {
                int lineStart = editor.PositionFromLine(i);
                int lineLength = editor.LineLength(i);
                
                if (lineLength < prefix.Length) return false;
                
                for (int j = 0; j < prefix.Length; j++)
                    if (editor.GetCharAt(lineStart + j) != prefix[j]) return false;
            }
            return true;
        }

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
            int sel1 = (int)editor.GetSelectionStart();
            int sel2 = (int)editor.GetSelectionEnd();
            int start = Math.Min(sel1, sel2);
            int end = Math.Max(sel1, sel2);
            
            int prefixLen = prefix.Length;
            int suffixLen = suffix.Length;
            int docLength = editor.GetTextLength();

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
                editor.DeleteRange(end, suffixLen);
                editor.DeleteRange(start - prefixLen, prefixLen);
                editor.SetSel(start - prefixLen, end - prefixLen);
                editor.EndUndoAction();
                return;
            }
            
            // Case A: The selection INCLUDES the tags
            int selLength = end - start;
            if (selLength >= prefixLen + suffixLen)
            {
                bool includesTags = true;
                for (int i = 0; i < prefixLen; i++)
                {
                    if (editor.GetCharAt(start + i) != prefix[i])
                    {
                        includesTags = false;
                        break;
                    }
                }
                if (includesTags)
                {
                    for (int i = 0; i < suffixLen; i++)
                    {
                        if (editor.GetCharAt(end - suffixLen + i) != suffix[i])
                        {
                            includesTags = false;
                            break;
                        }
                    }
                }
                
                if (includesTags)
                {
                    editor.BeginUndoAction();
                    editor.DeleteRange(end - suffixLen, suffixLen);
                    editor.DeleteRange(start, prefixLen);
                    editor.SetSel(start, end - prefixLen - suffixLen);
                    editor.EndUndoAction();
                    return;
                }
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
            int sel1 = (int)editor.GetSelectionStart();
            int sel2 = (int)editor.GetSelectionEnd();
            int start = Math.Min(sel1, sel2);
            int end = Math.Max(sel1, sel2);
            
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
                    editor.DeleteRange(lineStart, prefix.Length);
                }
                else
                {
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