using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Compiler_Parser_Demo_WPF
{
    class DataSource_FromGrammarComboBoxs : ITask
    {
        private List<GrammarCompilerCollection> grammarCompilerList;
        private ComboBox GrammarMajorType;
        private ComboBox GrammarMinorType;
        private bool Changed = false;

        public struct ResultInfo
        {
            public IGrammarCompiler GrammarCompiler;
            public DFAPriorityGenerator.ResultInfo DFAInfo;
        }

        ResultInfo Result;

        public void Init(GrammarCompilerCollection grammarCompilerList,ComboBox GrammarMajorType,ComboBox GrammarMinorType)
        {
            var majortypemap = new Dictionary<string,int>();
            var majortypelist = new List<string>();
            this.grammarCompilerList = new List<GrammarCompilerCollection>();
            this.GrammarMajorType = GrammarMajorType;
            this.GrammarMinorType = GrammarMinorType;

            //find out all major type
            foreach(var item in grammarCompilerList)
            {
                if(!majortypemap.ContainsKey(item.GetMajorType()))
                {
                    this.grammarCompilerList.Add(new GrammarCompilerCollection());
                    majortypemap[item.GetMajorType()] = this.grammarCompilerList.Count - 1;
                    majortypelist.Add(item.GetMajorType());
                }
            }

            //add all grammar compiler to list
            foreach(var item in grammarCompilerList)
            {
                this.grammarCompilerList[majortypemap[item.GetMajorType()]].Add(item);
            }

            //add major and minor type to combobox control
            GrammarMajorType.Items.Clear();
            GrammarMinorType.Items.Clear();

            foreach(var item in majortypelist)
            {
                GrammarMajorType.Items.Add(item);
            }

            WeakEventManager<ComboBox,SelectionChangedEventArgs>.AddHandler(GrammarMajorType,"SelectionChanged",GrammarMajorType_SelectionChanged);
            GrammarMajorType.SelectedIndex = 0;
            WeakEventManager<ComboBox,SelectionChangedEventArgs>.AddHandler(GrammarMinorType,"SelectionChanged",GrammarMinorType_SelectionChanged); 
            Result.GrammarCompiler = this.grammarCompilerList[0][0];
            Changed = true;
        }

        private void GrammarMajorType_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {
            GrammarMinorType.Items.Clear();

            foreach(var item in grammarCompilerList[GrammarMajorType.SelectedIndex])
            {
                GrammarMinorType.Items.Add(item.GetMinorType());
            }

            GrammarMinorType.SelectedIndex = 0;
        }

        private void GrammarMinorType_SelectionChanged(object sender,SelectionChangedEventArgs e)
        {
            Changed = true;
            Result.GrammarCompiler = grammarCompilerList[GrammarMajorType.SelectedIndex][GrammarMinorType.SelectedIndex];
        }

        public bool MoveFrom(object obj)
        {
            Result.DFAInfo = (DFAPriorityGenerator.ResultInfo)obj;
            return true;
        }

        public object MoveTo()
        {
            Changed = false;
            return Result;
        }

        public bool ResultChanged()
        {
            return Changed;
        }

        public void SetChanged()
        {
            Changed = true;
        }
    }
}
