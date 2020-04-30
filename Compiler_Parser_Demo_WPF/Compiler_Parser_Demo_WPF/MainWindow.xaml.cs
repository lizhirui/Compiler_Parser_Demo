using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using DFANode = Compiler_Parser_Demo_WPF.DFAGenerator.DFANode;
using DFAEdge = Compiler_Parser_Demo_WPF.DFAGenerator.DFAEdge;
using DFAItem = Compiler_Parser_Demo_WPF.DFAPriorityGenerator.DFAItem;

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
        private BitmapImage[] DFAImage = new BitmapImage[0];
        private BitmapImage[] DFAOptimizedImage = new BitmapImage[0];

        private class DataGrid_DFAPriorityTable_Item : INotifyPropertyChanged
        {
            public string Name
            {
                get
                {
                    return _Name;
                }

                set
                {
                    _Name = value;
                    OnPropertyChanged("Name");
                    
                }
                
            }
            public string RegularExpression
            {
                get
                {
                    return _RegularExpression;
                }

                set
                {
                    _RegularExpression = value;
                    OnPropertyChanged("RegularExpression");
                }
            }

            public string IsLoop
            {
                get
                {
                    return _IsLoop;
                }

                set
                {
                    _IsLoop = value;
                    OnPropertyChanged("IsLoop");
                }
            }
            public string Priority
            {
                get
                {
                    return _Priority;
                }

                set
                {
                    _Priority = value;
                    OnPropertyChanged("Priority");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(propertyName));
            }

            private string _Name;
            private string _RegularExpression;
            private string _IsLoop;
            private string _Priority;
        }

        private class DataGrid_LexerTestResult_Item : INotifyPropertyChanged
        {
            public string String
            {
                get
                {
                    return _String;
                }

                set
                {
                    _String = value;
                    OnPropertyChanged("String");
                }
            }

            public string Name
            {
                get
                {
                    return _Name;
                }

                set
                {
                    _Name = value;
                    OnPropertyChanged("Name");
                    
                }
                
            }
            public string RegularExpression
            {
                get
                {
                    return _RegularExpression;
                }

                set
                {
                    _RegularExpression = value;
                    OnPropertyChanged("RegularExpression");
                }
            }
            
            public string Priority
            {
                get
                {
                    return _Priority;
                }

                set
                {
                    _Priority = value;
                    OnPropertyChanged("Priority");
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(propertyName));
            }

            private string _String;
            private string _Name;
            private string _RegularExpression;
            private string _Priority;
        }

        ObservableCollection<DataGrid_DFAPriorityTable_Item> DataGrid_DFAPriorityTableData = new ObservableCollection<DataGrid_DFAPriorityTable_Item>();
        ObservableCollection<DataGrid_LexerTestResult_Item> DataGrid_LexerTestResultData = new ObservableCollection<DataGrid_LexerTestResult_Item>();

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

            DataGrid_DFAPriorityTable.ItemsSource = DataGrid_DFAPriorityTableData;
            DataGrid_LexerTestResult.ItemsSource = DataGrid_LexerTestResultData;

            CodeEditor.TextArea.TextEntering += CodeEditor_TextArea_TextEntering;
            CodeEditor.TextArea.TextEntered += TextArea_TextEntered;

            taskFlowManager.TaskResultUpdated += TaskFlowManager_TaskResultUpdated;
            taskFlowManager.TaskResultCleared += TaskFlowManager_TaskResultCleared;

            taskFlowManager.AddTask<DataSource_FromCodeEditor>();
            taskFlowManager.AddTask<Production_Lexer>();
            taskFlowManager.AddTask<Production_Parser>();
            taskFlowManager.AddTask<NFAGenerator_Lexer>();
            taskFlowManager.AddTask<NFAGenerator_Parser>();
            taskFlowManager.AddTask<DFAGenerator>();
            taskFlowManager.AddTask<DFAOptimizer>();
            taskFlowManager.AddTask<DFAPriorityGenerator>();

            taskFlowManager.GetTask<DataSource_FromCodeEditor>().BindEditor(CodeEditor);

            CheckBox_NFAGeneratorImageEnable.IsChecked = taskFlowManager.GetTask<NFAGenerator_Parser>().GetImageOutputEnable();
            CheckBox_DFAGeneratorImageEnable.IsChecked = taskFlowManager.GetTask<DFAGenerator>().GetImageOutputEnable();
            CheckBox_DFAOptimizerImageEnable.IsChecked = taskFlowManager.GetTask<DFAOptimizer>().GetImageOutputEnable();
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
                ComboBox_NFA_RegularExpress.Items.Clear();
                Image_NFA_Diagram.Source = null;
            }
            else if(TaskType == typeof(DFAPriorityGenerator))
            {
                TextBox_Info.Text += Sender.GetTask<DFAPriorityGenerator>().ErrorMsg;
                DataGrid_DFAPriorityTableData.Clear();
            }
        }

        private void TaskFlowManager_TaskResultUpdated(TaskFlowManager Sender,Type TaskType)
        {
            if(TaskType == typeof(Production_Lexer))
            {
                TextBox_Info.Text = "[Production_Lexer]:Execute OK!\n";
            }
            else if(TaskType == typeof(Production_Parser))
            {
                TextBox_Info.Text += "[Production_Parser]:Execute OK!\n";
                CodeEditor_Converted.Text = Sender.GetTask<Production_Parser>().ProductionCode;
            }
            else if(TaskType == typeof(NFAGenerator_Lexer))
            {
                TextBox_Info.Text += "[NFAGenerator_Lexer]:Execute OK!\n";
            }
            else if(TaskType == typeof(NFAGenerator_Parser))
            {
                ComboBox_NFA_RegularExpress.Items.Clear();
                Image_NFA_Diagram.Source = null;
                var nfaparser = Sender.GetTask<NFAGenerator_Parser>();
                var resultimage = nfaparser.ResultImage;
                var resultdata = nfaparser.Result;
                
                foreach(var item in resultdata.Production_ParserResult.tplist)
                {
                    ComboBox_NFA_RegularExpress.Items.Add("<" + item.Name + "> -> \"" + item.RegularExpression + "\"");
                }

                NFAImage = resultimage;

                if(ComboBox_NFA_RegularExpress.Items.Count > 0)
                {
                    ComboBox_NFA_RegularExpress.SelectedIndex = 0;
                }

                TextBox_Info.Text += "[NFAGenerator_Parser]:Execute OK!\n";
            }
            else if(TaskType == typeof(DFAGenerator))
            {
                ComboBox_DFA_RegularExpress.Items.Clear();
                Image_DFA_Diagram.Source = null;
                var dfagenerator = Sender.GetTask<DFAGenerator>();
                var resultimage = dfagenerator.ResultImage;
                var resultdata = dfagenerator.Result;
                
                foreach(var item in resultdata.Production_ParserResult.tplist)
                {
                    ComboBox_DFA_RegularExpress.Items.Add("<" + item.Name + "> -> \"" + item.RegularExpression + "\"");
                }

                DFAImage = resultimage;

                if(ComboBox_DFA_RegularExpress.Items.Count > 0)
                {
                    ComboBox_DFA_RegularExpress.SelectedIndex = 0;
                }

                TextBox_Info.Text += "[DFAGenerator]:Execute OK!\n";
            }
            else if(TaskType == typeof(DFAOptimizer))
            {
                ComboBox_DFAOptimized_RegularExpress.Items.Clear();
                Image_DFAOptimized_Diagram.Source = null;
                var dfaoptimizer = Sender.GetTask<DFAOptimizer>();
                var resultimage = dfaoptimizer.ResultImage;
                var resultdata = dfaoptimizer.Result;
                
                foreach(var item in resultdata.Production_ParserResult.tplist)
                {
                    ComboBox_DFAOptimized_RegularExpress.Items.Add("<" + item.Name + "> -> \"" + item.RegularExpression + "\"");
                }

                DFAOptimizedImage = resultimage;

                if(ComboBox_DFAOptimized_RegularExpress.Items.Count > 0)
                {
                    ComboBox_DFAOptimized_RegularExpress.SelectedIndex = 0;
                }

                TextBox_Info.Text += "[DFAOptimizer]:Execute OK!\n";
            }
            else if(TaskType == typeof(DFAPriorityGenerator))
            {
                TextBox_Info.Text += "[DFAPriorityGenerator]:Execute OK!\n";
                var dfaprioritygenerator = Sender.GetTask<DFAPriorityGenerator>();
                DataGrid_DFAPriorityTableData.Clear();

                foreach(var dfa in dfaprioritygenerator.Result.Item.NoLoopDFAList)
                {
                    var tinfo = dfaprioritygenerator.Result.Production_ParserResult.tplist[dfa.TerminalID];
                    DataGrid_DFAPriorityTableData.Add(new DataGrid_DFAPriorityTable_Item{Name = tinfo.Name,RegularExpression = tinfo.RegularExpression,IsLoop = "否",Priority = "高"});
                }

                foreach(var dfa in dfaprioritygenerator.Result.Item.LoopDFAList)
                {
                    var tinfo = dfaprioritygenerator.Result.Production_ParserResult.tplist[dfa.TerminalID];
                    DataGrid_DFAPriorityTableData.Add(new DataGrid_DFAPriorityTable_Item{Name = tinfo.Name,RegularExpression = tinfo.RegularExpression,IsLoop = "是",Priority = "低"});
                }
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

        private void ComboBox_NFA_RegularExpress_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {
            var index = ComboBox_NFA_RegularExpress.SelectedIndex;

            if(index >= 0 && index < NFAImage.Length)
            {
                Image_NFA_Diagram.Source = NFAImage[index];
            }
        }

        private void ComboBox_DFA_RegularExpress_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {
            var index = ComboBox_DFA_RegularExpress.SelectedIndex;

            if(index >= 0 && index < DFAImage.Length)
            {
                Image_DFA_Diagram.Source = DFAImage[index];
            }
        }

        private void ComboBox_DFAOptimized_RegularExpress_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {
            var index = ComboBox_DFAOptimized_RegularExpress.SelectedIndex;

            if(index >= 0 && index < DFAOptimizedImage.Length)
            {
                Image_DFAOptimized_Diagram.Source = DFAOptimizedImage[index];
            }
        }

        private void Button_DFAGenerate_Click(object sender,RoutedEventArgs e)
        {
            taskFlowManager.RunTask<DFAGenerator>();
        }

        private void Button_DFAOptimise_Click(object sender,RoutedEventArgs e)
        {
            taskFlowManager.RunTask<DFAOptimizer>();
        }

        private void Button_DFAPriorityGenerate_Click(object sender,RoutedEventArgs e)
        {
            taskFlowManager.RunTask<DFAPriorityGenerator>();
        }

        private void CheckBox_NFAGeneratorImageEnable_Click(object sender,RoutedEventArgs e)
        {
            taskFlowManager.GetTask<NFAGenerator_Parser>().SetImageOutputEnable((bool)(sender as CheckBox).IsChecked);
        }

        private void CheckBox_DFAGeneratorImageEnable_Click(object sender,RoutedEventArgs e)
        {
            taskFlowManager.GetTask<DFAGenerator>().SetImageOutputEnable((bool)(sender as CheckBox).IsChecked);
        }

        private void CheckBox_DFAOptimizerImageEnable_Click(object sender,RoutedEventArgs e)
        {
            taskFlowManager.GetTask<DFAOptimizer>().SetImageOutputEnable((bool)(sender as CheckBox).IsChecked);
        }

        private void LexerTest_Error(string ErrorText)
        {
            MessageBox.Show(ErrorText,"词法分析错误",MessageBoxButton.OK,MessageBoxImage.Error);
        }

        struct LexerWordInfo
        {
            public string String;
            public DFAItem DFA;
            public bool IsHighPriorityDFA;
        }

        private bool MatchDFA(string Code,int StartIndex,DFANode StartNode,out int Length)
        {
            var succlength = 0;
            var curnode = StartNode;
            var curlength = 0;

            while(true)
            {
                if(curnode.IsEndNode)
                {
                    succlength = curlength;
                }

                if((StartIndex + curlength) >= Code.Length)
                {
                    break;
                }

                var matchedge = new DFAEdge();

                foreach(var edge in curnode.Edge)
                {
                    if(edge.Condition == Code[StartIndex + curlength])
                    {
                        matchedge = edge;
                        break;
                    }
                }

                if(matchedge.NextNode == null)
                {
                    Length = succlength;
                    break;
                }

                curnode = matchedge.NextNode;
                curlength++;
            }
            
            Length = succlength;
            return succlength == 0 ? false : true;
        }

        private bool LexerAnalysis(string Code,out List<LexerWordInfo> WordInfo,out string ErrorText)
        {
            if(Code == "")
            {
                WordInfo = new List<LexerWordInfo>();
                ErrorText = "";
                return true;
            }

            WordInfo = new List<LexerWordInfo>();
            var dfapg = taskFlowManager.GetTask<DFAPriorityGenerator>();
            var nolooplist = dfapg.Result.Item.NoLoopDFAList;
            var looplist = dfapg.Result.Item.LoopDFAList;
            var tplist = dfapg.Result.Production_ParserResult.tplist;

            var curindex = 0;

            while(curindex < Code.Length)
            {
                var length = 0;
                var dfaitem = new DFAItem();
                var ishigh = true;
                var matched = false;

                foreach(var dfa in nolooplist)
                {
                    if(MatchDFA(Code,curindex,dfa.StartNode,out length))
                    {
                        matched = true;
                        ishigh = true;
                        dfaitem = dfa;
                        break;
                    }
                }

                if(!matched)
                {
                    foreach(var dfa in looplist)
                    {
                        if(MatchDFA(Code,curindex,dfa.StartNode,out length))
                        {
                            matched = true;
                            ishigh = false;
                            dfaitem = dfa;
                            break;
                        }
                    }
                }

                if(!matched)
                {
                    ErrorText = "在字符" + curindex + "\"" + Code[curindex] + "\"处分析失败：无匹配DFA！";
                    return false;
                }
                else
                {
                    WordInfo.Add(new LexerWordInfo{String = Code.Substring(curindex,length),IsHighPriorityDFA = ishigh,DFA = dfaitem});
                    curindex += length;
                }
            }

            ErrorText = "";
            return true;
        }

        private void Button_LexerTest_Click(object sender,RoutedEventArgs e)
        {
            var dfapg = taskFlowManager.GetTask<DFAPriorityGenerator>();
            var tplist = dfapg.Result.Production_ParserResult.tplist;

            if(dfapg.Result.Item.LoopDFAList == null)
            {
                LexerTest_Error("未完成DFA优先级生成任务！");
                return;
            }

            List<LexerWordInfo> result;
            var errorstr = "";

            if(!LexerAnalysis(CodeEditor_LexerTest.Text,out result,out errorstr))
            {
                LexerTest_Error(errorstr);
            }

            DataGrid_LexerTestResultData.Clear();

            foreach(var item in result)
            {
                DataGrid_LexerTestResultData.Add(new DataGrid_LexerTestResult_Item{String = item.String,Name = tplist[item.DFA.TerminalID].Name,RegularExpression = tplist[item.DFA.TerminalID].RegularExpression,Priority = item.IsHighPriorityDFA ? "高" : "低"});
            }
        }
    }
}