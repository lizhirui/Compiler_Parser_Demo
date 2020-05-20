using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler_Parser_Demo_WPF
{
    class GrammarCompiler : ITask
    {
        public DataSource_FromGrammarComboBoxs.ResultInfo GrammarPreInfo
        {
            private set;
            get;
        }

        public string ErrorText
        {
            get;
            private set;
        }

        private bool Changed = false;

        private string GetStartSymbol()
        {
            //必须存在非终结符，且要求未被引用的产生式唯一，并且该产生式的左侧符号即为开始符号
            var ntplist = GrammarPreInfo.DFAInfo.Production_ParserResult.ntplist;

            if(ntplist.Length == 0)
            {
                ErrorText = "不存在非终结符，无法预测开始符号！";
                return "";
            }
            else
            {
                return ntplist[0].Name;//默认第一个产生式的符号为开始符号
                var refcount = new Dictionary<string,int>();

                foreach(var pitem in ntplist)
                {
                    refcount[pitem.Name] = 0;
                }

                foreach(var pitem in ntplist)
                {
                    foreach(var item in pitem.Item)
                    {
                        foreach(var symbol in item.Content)
                        {
                            if(refcount.ContainsKey(symbol))
                            {
                                refcount[symbol]++;
                            }
                        }
                    }
                }

                var startsymbol = "";

                foreach(var pitem in refcount)
                {
                    if(pitem.Value == 0)
                    {
                        if(startsymbol == "")
                        {
                            startsymbol = pitem.Key;
                        }
                        else
                        {
                            ErrorText = "发现超过一个未被引用的产生式，包括\"" + startsymbol + "\"与\"" + pitem.Key + "\"，无法预测开始符号！";
                            return "";
                        }
                    }
                }

                return startsymbol;
            }
        }

        public bool MoveFrom(object obj)
        {
            GrammarPreInfo = (DataSource_FromGrammarComboBoxs.ResultInfo)obj;
            ErrorText = "";
            var startsymbol = GetStartSymbol();
            var r = true;

            if(startsymbol != "")
            {
                r = GrammarPreInfo.GrammarCompiler.Compile(GrammarPreInfo.DFAInfo,startsymbol,out var errmsg);
                ErrorText = errmsg; 
            }
            else
            {
                r = false;
            }
            
            Changed = true;
            return r;
        }

        public object MoveTo()
        {
            Changed = false;
            return GrammarPreInfo.GrammarCompiler;
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
