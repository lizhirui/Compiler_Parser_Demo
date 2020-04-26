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
        private BitmapImage[] NFAImage = new BitmapImage[0];

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
            taskFlowManager.AddTask<NFAGenerator_Lexer>();
            taskFlowManager.AddTask<NFAGenerator_Parser>();

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
            else if(TaskType == typeof(NFAGenerator_Lexer))
            {
                TextBox_Info.Text += Sender.GetTask<NFAGenerator_Lexer>().ErrorMsg;
            }
            else if(TaskType == typeof(NFAGenerator_Parser))
            {
                TextBox_Info.Text += Sender.GetTask<NFAGenerator_Parser>().ErrorMsg;
                ComboBox_RegularExpress.Items.Clear();
                Image_Diagram.Source = null;
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
            else if(TaskType == typeof(NFAGenerator_Lexer))
            {
                TextBox_Info.Text += "NFAGenerator_Lexer Execute OK!\n";
            }
            else if(TaskType == typeof(NFAGenerator_Parser))
            {
                ComboBox_RegularExpress.Items.Clear();
                Image_Diagram.Source = null;
                var nfaparser = Sender.GetTask<NFAGenerator_Parser>();
                var resultimage = nfaparser.ResultImage;
                var resultdata = nfaparser.Result;
                
                foreach(var item in resultdata.Production_ParserResult.tplist)
                {
                    ComboBox_RegularExpress.Items.Add("<" + item.Name + "> -> \"" + item.RegularExpression + "\"");
                }

                NFAImage = resultimage;

                if(ComboBox_RegularExpress.Items.Count > 0)
                {
                    ComboBox_RegularExpress.SelectedIndex = 0;
                }

                TextBox_Info.Text += "NFAGenerator_Parser Execute OK!\n";
            }
        }

        private void TextArea_TextEntered(object sender,TextCompositionEventArgs e)
        {
            if(completionWindow is null)
            {
                return;
            }

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

        private void Button_NFAGenerate_Click(object sender,RoutedEventArgs e)
        {
            taskFlowManager.RunTask<NFAGenerator_Parser>();
        }

        private void ComboBox_RegularExpress_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {
            var index = ComboBox_RegularExpress.SelectedIndex;

            if(index >= 0 && index < NFAImage.Length)
            {
                Image_Diagram.Source = NFAImage[index];
            }
        }
    }
}