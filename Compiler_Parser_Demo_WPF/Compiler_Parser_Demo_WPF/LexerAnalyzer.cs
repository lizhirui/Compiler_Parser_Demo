using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Compiler_Parser_Demo_WPF.DFAGenerator;
using static Compiler_Parser_Demo_WPF.DFAPriorityGenerator;

namespace Compiler_Parser_Demo_WPF
{
    struct LexerWordInfo
    {
        public string String;
        public DFAItem DFA;
        public bool IsHighPriorityDFA;
    }

    class LexerAnalyzer
    {
        private TaskFlowManager taskFlowManager = null;

        public LexerAnalyzer(TaskFlowManager taskFlowManager)
        {
            this.taskFlowManager = taskFlowManager;
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

        public bool LexerAnalysis(string Code,out List<LexerWordInfo> WordInfo,out string ErrorText)
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
    }
}
