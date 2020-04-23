using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace Compiler_Parser_Demo_WPF
{
    class ProductionCompletionData : ICompletionData
    {
        public ProductionCompletionData(string text,string description)
        {
            this.Text = text;
            this.Description = description;
        }

        public ImageSource Image
        {
            get
            {
                return null;
            }
        }
        
        public string Text
        {
            get;
            private set;
        }

        public object Content
        {
            get
            {
                return this.Text;
            }
        }

        public object Description
        {
            get;
            private set;
        }

        public double Priority
        {
            get
            {
                return 1;
            }
        }

        public void Complete(TextArea textArea,ISegment completionSegment,EventArgs insertionRequestEventArgs)
        {
            textArea.Document.Replace(completionSegment,this.Text + " ");
        }
    }
}
