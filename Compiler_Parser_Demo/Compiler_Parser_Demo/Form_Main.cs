using ICSharpCode.TextEditor.Document;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Compiler_Parser_Demo
{
    public partial class Form_Main : Form
    {
        public Form_Main()
        {
            InitializeComponent();
        }

        private void Form_Main_Load(object sender,EventArgs e)
        {
            textEditorControl1.ShowEOLMarkers = false;
            textEditorControl1.ShowHRuler = false;
            textEditorControl1.ShowInvalidLines = false;
            textEditorControl1.ShowMatchingBracket = true;
            textEditorControl1.ShowSpaces = false;
            textEditorControl1.ShowTabs = false;
            textEditorControl1.ShowVRuler = false;
            textEditorControl1.AllowCaretBeyondEOL = false;

            try
            {
                var file = new FileSyntaxModeProvider(".\\");
                HighlightingManager.Manager.AddSyntaxModeFileProvider(file);
                textEditorControl1.SetHighlighting("Production");
                textEditorControl1.Encoding = Encoding.Default;
                textEditorControl1.Text = "and -> aa\"55\" //123\n/*sds45*/";
            }
            catch(Exception ex)
            {
                ex = ex;
            }
        }
    }
}
