using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DFANode = Compiler_Parser_Demo_WPF.DFAGenerator.DFANode;
using DFAEdge = Compiler_Parser_Demo_WPF.DFAGenerator.DFAEdge;
using DFAInfoType = Compiler_Parser_Demo_WPF.DFAGenerator.ResultInfo;

namespace Compiler_Parser_Demo_WPF
{
    class DFAPriorityGenerator : ITask
    {
        private class DFAPriorityGeneratorException : Exception
        {
            
        }

        public struct ResultInfo
        {
            public ResultItem Item;
            public Production_Parser.ResultInfo Production_ParserResult;
        }

        public struct DFAItem
        {
            public int TerminalID;
            public DFANode StartNode;
            public char[] CharSet;
        }

        public struct ResultItem
        {
            public DFAItem[] NoLoopDFAList;
            public DFAItem[] LoopDFAList;
        }

        private DFAInfoType DFAInfo;

        public string ErrorMsg
        {
            get;
            private set;
        }

        public ResultInfo Result
        {
            get;
            private set;
        }

        private bool Changed = false;

        public DFAPriorityGenerator()
        {
            ErrorMsg = "";
        }

        private bool DFAHasLoop_DFS(DFANode CurNode,HashSet<DFANode> Visited)
        {
            Visited.Add(CurNode);

            foreach(var edge in CurNode.Edge)
            {
                if(Visited.Contains(edge.NextNode))
                {
                    return true; 
                }

                if(DFAHasLoop_DFS(edge.NextNode,Visited))
                {
                    return true;
                }
            }

            Visited.Remove(CurNode);
            return false;
        }

        /// <summary>
        /// 判断DFA是否有环，只需判断访问时是否重复访问某一个节点即可
        /// </summary>
        /// <param name="StartNode"></param>
        /// <returns></returns>
        private bool DFAHasLoop(DFANode StartNode)
        {
            return DFAHasLoop_DFS(StartNode,new HashSet<DFANode>());
        }

        private bool DFAAmbiguityCheck_DFS(DFANode DFA1,DFANode DFA2,HashSet<DFANode> Visited,Stack<char> ResultStack)
        {
            //两个DFA同时到达终节点，检测到二义性
            if(DFA1.IsEndNode && DFA2.IsEndNode)
            {
                return true;
            }

            //若两个DFA同时到达一个曾经访问过的点，且该点在回路中，则可认为对于这部分回路，两个DFA是无二义性的
            if(Visited.Contains(DFA1) && Visited.Contains(DFA2))
            {
                return false;
            }

            Visited.Add(DFA1);
            Visited.Add(DFA2);
            var dfa1charset = new HashSet<char>();
            var charset = new HashSet<char>();
            var dfa1nextnode = new Dictionary<char,DFANode>();
            var dfa2nextnode = new Dictionary<char,DFANode>();

            foreach(var edge in DFA1.Edge)
            {
                dfa1charset.Add(edge.Condition);
                dfa1nextnode[edge.Condition] = edge.NextNode;
            }

            foreach(var edge in DFA2.Edge)
            {
                if(dfa1charset.Contains(edge.Condition))
                {
                    charset.Add(edge.Condition);
                    dfa2nextnode[edge.Condition] = edge.NextNode;
                }
            }

            foreach(var ch in charset)
            {
                ResultStack.Push(ch);

                if(DFAAmbiguityCheck_DFS(dfa1nextnode[ch],dfa2nextnode[ch],Visited,ResultStack))
                {
                    //检测到二义性
                    return true;
                }

                ResultStack.Pop();
            }

            return false;
        }

        /// <summary>
        /// DFA二义性检查
        /// </summary>
        /// <returns></returns>
        private bool DFAAmbiguityCheck(DFANode DFA1,DFANode DFA2,out string ErrorLanguageString)
        {
            var s = new Stack<char>();

            if(DFAAmbiguityCheck_DFS(DFA1,DFA2,new HashSet<DFANode>(),s))
            {
                var s2 = new Stack<char>();

                while(s.Count > 0)
                {
                    s2.Push(s.Pop());
                }

                var stb = new StringBuilder();

                while(s2.Count > 0)
                {
                    stb.Append(s2.Pop());
                }

                ErrorLanguageString = stb.ToString();
                return true;
            }
            else
            {
                ErrorLanguageString = "";
                return false;
            }
        }

        private void DFAListAmbiguityCheck(List<DFAItem> DFAList)
        {
            for(var i = 0;i < DFAList.Count;i++)
            {
                for(var j = i + 1;j < DFAList.Count;j++)
                {
                    var errorlangstr = "";

                    if(DFAAmbiguityCheck(DFAList[i].StartNode,DFAList[j].StartNode,out errorlangstr))
                    {
                        ErrorMsg = "[DFAPriorityGenerator]:DFA二义性错误：\"" + DFAInfo.Production_ParserResult.tplist[DFAList[i].TerminalID].Name + "\"与\"" + DFAInfo.Production_ParserResult.tplist[DFAList[j].TerminalID].Name + "\"，二义性语言为：\"" + errorlangstr + "\"";
                        throw new DFAPriorityGeneratorException();
                    }
                }
            }
        }

        public bool Analysis(DFAInfoType DFAInfo)
        {
            this.DFAInfo = DFAInfo;
            var noloopdfalist = new List<DFAItem>();
            var loopdfalist = new List<DFAItem>();
            var id = 0;

            foreach(var dfa in DFAInfo.Item)
            {
                if(DFAHasLoop(dfa.StartNode))
                {
                    loopdfalist.Add(new DFAItem{StartNode = dfa.StartNode,CharSet = dfa.CharSet,TerminalID = id});
                }
                else
                {
                    noloopdfalist.Add(new DFAItem{StartNode = dfa.StartNode,CharSet = dfa.CharSet,TerminalID = id});
                }

                id++;
            }

            try
            {
                DFAListAmbiguityCheck(loopdfalist);
                DFAListAmbiguityCheck(noloopdfalist);
            }
            catch(DFAPriorityGeneratorException ex)
            {
                Result = new ResultInfo();
                return false;
            }
            catch(Exception ex)
            {
                Result = new ResultInfo();
                ErrorMsg = "[DFAPriorityGenerator]:" + ex.Message + "\n" + ex.StackTrace;
                return false;
            }

            Result = new ResultInfo{Production_ParserResult = DFAInfo.Production_ParserResult,Item = new ResultItem{NoLoopDFAList = noloopdfalist.ToArray(),LoopDFAList = loopdfalist.ToArray()}};
            return true;
        }

        public bool MoveFrom(object obj)
        {
            Changed = true;
            return Analysis((DFAInfoType)obj);
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
