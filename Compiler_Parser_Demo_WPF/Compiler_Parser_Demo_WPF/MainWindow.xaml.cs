using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace Compiler_Parser_Demo_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CompletionWindow completionWindow;
        private TaskFlowManager taskFlowManager = new TaskFlowManager();

        public MainWindow()
        {
            InitializeComponent();

            var asmb = Assembly.GetExecutingAssembly();
            var xshdfile = asmb.GetName().Name + ".Code.xshd";

            using(var s = asmb.GetManifestResourceStream(xshdfile))
            {
                using(var reader = new XmlTextReader(s))
                {
                    CodeEditor.SyntaxHighlighting = HighlightingLoader.Load(reader,HighlightingManager.Instance);
                    CodeEditor_Converted.SyntaxHighlighting = CodeEditor.SyntaxHighlighting;
                }
            }

            CodeEditor.TextArea.TextEntering += CodeEditor_TextArea_TextEntering;
            CodeEditor.TextArea.TextEntered += TextArea_TextEntered;

            taskFlowManager.TaskResultUpdated += TaskFlowManager_TaskResultUpdated;
            taskFlowManager.TaskResultCleared += TaskFlowManager_TaskResultCleared;

            taskFlowManager.AddTask<DataSource_FromCodeEditor>();
            taskFlowManager.AddTask<Production_Lexer>();
            taskFlowManager.AddTask<Production_Parser>();

            taskFlowManager.GetTask<DataSource_FromCodeEditor>().BindEditor(CodeEditor);
        }

        private void TaskFlowManager_TaskResultCleared(TaskFlowManager Sender,Type TaskType)
        {
            if(TaskType == typeof(Production_Lexer))
            {
                TextBox_Info.Text = Sender.GetTask<Production_Lexer>().ErrorMsg;
                CodeEditor_Converted.Text = "";
            }
            else if(TaskType == typeof(Production_Parser))
            {
                TextBox_Info.Text += Sender.GetTask<Production_Parser>().ErrorMsg;
                CodeEditor_Converted.Text = "";
            }
        }

        private void TaskFlowManager_TaskResultUpdated(TaskFlowManager Sender,Type TaskType)
        {
            if(TaskType == typeof(Production_Lexer))
            {
                TextBox_Info.Text = "Lexer Execute OK!\n";
            }
            else if(TaskType == typeof(Production_Parser))
            {
                TextBox_Info.Text += "Parser Execute OK!\n";
                CodeEditor_Converted.Text = Sender.GetTask<Production_Parser>().ProductionCode;
            }
        }

        private void TextArea_TextEntered(object sender,TextCompositionEventArgs e)
        {
            if(completionWindow.CompletionList.ListBox.Items.Count == 0)
            {
                completionWindow.Close();
            }
            else
            {
                completionWindow.Show();
            }
        }

        private void CodeEditor_TextArea_TextEntering(object sender,TextCompositionEventArgs e)
        {
            if(e.Text.Length > 0 && completionWindow is null)
            {
                completionWindow = new CompletionWindow(CodeEditor.TextArea);
                IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                data.Add(new ProductionCompletionData("->","Product Flag"));
                completionWindow.Background = CodeEditor.Background;
                completionWindow.Foreground = CodeEditor.Foreground;
                completionWindow.CompletionList.Background = CodeEditor.Background;
                completionWindow.CompletionList.Foreground = CodeEditor.Foreground;
                completionWindow.CompletionList.ListBox.Background = CodeEditor.Background;
                completionWindow.CompletionList.ListBox.Foreground = CodeEditor.Foreground;
                
                completionWindow.Closed += delegate 
                {
                    completionWindow = null;
                };
            }
        }

        private void Button_Convert_Click(object sender,RoutedEventArgs e)
        {
            taskFlowManager.RunTask<Production_Parser>();
        }
    }
}