using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler_Parser_Demo_WPF
{
    class DataSource_FromCodeEditor : ITask
    {
        private TextEditor textEditor = null;
        private bool TextChanged = true;

        public void BindEditor(TextEditor textEditor)
        {
            this.textEditor = textEditor;
            textEditor.TextChanged += TextEditor_TextChanged;
        }

        private void TextEditor_TextChanged(object sender,EventArgs e)
        {
            TextChanged = true;
        }

        public bool MoveFrom(object obj)
        {
            return true;
        }

        public object MoveTo()
        {
            TextChanged = false;
            return textEditor.Text;
        }

        public bool ResultChanged()
        {
            return TextChanged;
        }

        public void SetChanged()
        {
            TextChanged = true;
        }
    }
}
