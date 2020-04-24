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
            Production_Lexer lexer = new Production_Lexer();

            if(lexer.Analysis(CodeEditor.Text))
            {
                CodeEditor_Converted.Text = "Lexer Execute OK!\n";

                Production_Parser parser = new Production_Parser();

                if(parser.Analysis(lexer.Result))
                {
                    CodeEditor_Converted.Text += "Parser Execute OK!\n";
                    var stb = new StringBuilder();

                    foreach(var item in parser.NonTerminalProductionResult)
                    {
                        stb.Append(item.Name + " -> ");
                        var first = true;

                        foreach(var item2 in item.Item)
                        {
                            if(first)
                            {
                                first = false;
                            }
                            else
                            {
                                stb.Append("\n" + new string(' ',item.Name.Length + 2) + "| ");
                            }

                            var first2 = true;

                            foreach(var item3 in item2.Content)
                            {
                                if(first2)
                                {
                                    first2 = false;
                                }
                                else
                                {
                                    stb.Append(" ");
                                }

                                stb.Append("<" + item3 + ">");
                            }
                        }

                        stb.Append(";\n");

                        if(item.Item.Length > 0)
                        {
                            stb.Append("\n");
                        }
                    }

                    foreach(var item in parser.TerminalProductionResult)
                    {
                        stb.Append(item.Name + " -> \"" + item.RegularExpression + "\";\n");
                    }

                    CodeEditor_Converted.Text += "\n" + stb.ToString();
                }
                else
                {
                    CodeEditor_Converted.Text += parser.ErrorMsg;
                }
            }
            else
            {
                CodeEditor_Converted.Text = lexer.ErrorMsg;
            }
        }
    }
}