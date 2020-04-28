using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Compiler_Parser_Demo_WPF
{
    class DFAGenerator : ITask
    {
        public class DFANode
        {
            public int ID;
            public bool IsEndNode;
            public List<DFAEdge> Edge;
        }

        public struct DFAEdge
        {
            public char Condition;
            public DFANode NextNode;
        }

        public struct ResultInfo
        {
            public ResultItem[] Item;
            public Production_Parser.ResultInfo Production_ParserResult;
        }

        public struct ResultItem
        {
            public DFANode StartNode;
            public char[] CharSet;
        }

        public ResultInfo Result
        {
            get;
            private set;
        }

        public BitmapImage[] ResultImage
        {
            get;
            private set;
        }

        private NFAGenerator_Parser.ResultInfo NFAInfo;
        private int CurIndex;
        private Dictionary<NFAGenerator_Parser.NFANode,int> NFANodeNameMap;
        private int NewNFANodeID = 0;
        private Dictionary<string,DFANode> NFASetToDFANodeMap;
        private int NewDFANodeID = 0;
        private bool Changed = false;

        public DFAGenerator()
        {
        
        }

        private int GetNFANodeName(NFAGenerator_Parser.NFANode NFANode)
        {
            if(!NFANodeNameMap.ContainsKey(NFANode))
            {
                NFANodeNameMap[NFANode] = NewNFANodeID;
                return NewNFANodeID++;
            }
            else
            {
                return NFANodeNameMap[NFANode];
            }
        }

        private void NormalizeNFANodeSet(List<NFAGenerator_Parser.NFANode> NFANodeSet)
        {
            NFANodeSet.Sort((x,y) => GetNFANodeName(x).CompareTo(GetNFANodeName(y)));
        }

        private string NFANodeSetToString(List<NFAGenerator_Parser.NFANode> NFANodeSet)
        {
            NormalizeNFANodeSet(NFANodeSet);
            var stb = new StringBuilder();

            foreach(var item in NFANodeSet)
            {
                stb.Append(GetNFANodeName(item));
                stb.Append(',');
            }

            return stb.ToString();
        }

        private DFANode GetNewDFANode()
        {
            return new DFANode{ID = NewDFANodeID++,IsEndNode = false,Edge = new List<DFAEdge>()};
        }

        private DFANode GetDFANode(List<NFAGenerator_Parser.NFANode> NFANodeSet)
        {
            var nfanodesetstr = NFANodeSetToString(NFANodeSet);

            if(!NFASetToDFANodeMap.ContainsKey(nfanodesetstr))
            {
                var newnode = GetNewDFANode();
                newnode.IsEndNode = NFANodeSet.Contains(NFAInfo.Item[CurIndex].EndNode);
                NFASetToDFANodeMap[nfanodesetstr] = newnode;
                return newnode;
            }
            else
            {
                return NFASetToDFANodeMap[nfanodesetstr];
            }
        }

        private bool IsNFANodeSetEqual(List<NFAGenerator_Parser.NFANode> NFANodeSet1,List<NFAGenerator_Parser.NFANode> NFANodeSet2)
        {
            if(NFANodeSet1.Count != NFANodeSet2.Count)
            {
                return false;
            }

            NormalizeNFANodeSet(NFANodeSet1);
            NormalizeNFANodeSet(NFANodeSet2);

            for(var i = 0;i < NFANodeSet1.Count;i++)
            {
                if(NFANodeSet1[i] != NFANodeSet2[i])
                {
                    return false;
                }
            }

            return true;
        }

        private List<NFAGenerator_Parser.NFANode> GetEpsilonClosure(NFAGenerator_Parser.NFANode NFANode)
        {
            return GetEpsilonClosure(new NFAGenerator_Parser.NFANode[]{NFANode}.ToList());
        }

        private List<NFAGenerator_Parser.NFANode> GetEpsilonClosure(List<NFAGenerator_Parser.NFANode> NFANodeSet)
        {
            var waitqueue = new Queue<NFAGenerator_Parser.NFANode>();
            var rset = new HashSet<NFAGenerator_Parser.NFANode>();

            foreach(var item in NFANodeSet)
            {
                waitqueue.Enqueue(item);
                rset.Add(item);
            }

            while(waitqueue.Count > 0)
            {
                var curnode = waitqueue.Dequeue();

                foreach(var edge in curnode.Edge)
                {
                    if(edge.Epsilon && !rset.Contains(edge.NextNode))
                    {
                        rset.Add(edge.NextNode);
                        waitqueue.Enqueue(edge.NextNode);
                    }
                }
            }

            return rset.ToList();
        }

        private char[] GetNFACharSet()
        {
            var queue = new Queue<NFAGenerator_Parser.NFANode>();
            var visited = new HashSet<NFAGenerator_Parser.NFANode>();
            var r = new HashSet<char>();

            queue.Enqueue(NFAInfo.Item[CurIndex].StartNode);

            while(queue.Count > 0)
            {
                var curnode = queue.Dequeue();
                visited.Add(curnode);

                foreach(var edge in curnode.Edge)
                {
                    if(!edge.Epsilon)
                    {
                        r.Add(edge.Condition);
                    }

                    if(!visited.Contains(edge.NextNode))
                    {
                        queue.Enqueue(edge.NextNode);
                    }
                }
            }

            return r.ToArray();
        }

        private List<NFAGenerator_Parser.NFANode> Move(List<NFAGenerator_Parser.NFANode> T,char a)
        {
            var r = new HashSet<NFAGenerator_Parser.NFANode>();

            foreach(var node in T)
            {
                foreach(var edge in node.Edge)
                {
                    if(!edge.Epsilon && edge.Condition == a)
                    {
                        r.Add(edge.NextNode);
                    }
                }
            }
            
            return r.ToList();
        }

        private void AnalysisItem(List<ResultItem> rlist)
        {
            var Dstates = new HashSet<string>();
            var Visited = new HashSet<string>();
            var queue = new Queue<List<NFAGenerator_Parser.NFANode>>();
            var startset = GetEpsilonClosure(NFAInfo.Item[CurIndex].StartNode);
            var r = new ResultItem{CharSet = GetNFACharSet(),StartNode = GetDFANode(startset)};

            queue.Enqueue(startset);
            Dstates.Add(NFANodeSetToString(startset));

            while(queue.Count > 0)
            {
                var T = queue.Dequeue();
                var Tname = NFANodeSetToString(T);

                Visited.Add(Tname);

                foreach(var a in r.CharSet)
                {
                    var U = GetEpsilonClosure(Move(T,a));

                    if(U.Count > 0)
                    {
                        var U_name = NFANodeSetToString(U);

                        if(!Dstates.Contains(U_name))
                        {
                            Dstates.Add(U_name);
                            queue.Enqueue(U);
                        }

                        GetDFANode(T).Edge.Add(new DFAEdge{Condition = a,NextNode = GetDFANode(U)});
                    }
                }
            }

            rlist.Add(r);
        }

        public void Analysis(NFAGenerator_Parser.ResultInfo NFAInfo)
        {
            this.NFAInfo = NFAInfo;
            var rlist = new List<ResultItem>();

            for(CurIndex = 0;CurIndex < NFAInfo.Item.Length;CurIndex++)
            {
                NFANodeNameMap = new Dictionary<NFAGenerator_Parser.NFANode,int>();
                NewNFANodeID = 0;
                NFASetToDFANodeMap = new Dictionary<string,DFANode>();
                NewDFANodeID = 0;
                AnalysisItem(rlist);
            }

            Result = new ResultInfo{Item = rlist.ToArray(),Production_ParserResult = NFAInfo.Production_ParserResult};

            var rimage = new List<BitmapImage>();

            foreach(var item in rlist)
            {
                rimage.Add(DFAGenerator_DiagramGenerator.ToImage(item));
            }

            ResultImage = rimage.ToArray();
        }

        public bool MoveFrom(object obj)
        {
            Changed = true;
            Analysis((NFAGenerator_Parser.ResultInfo)obj);
            return true;
        }

        public object MoveTo()
        {
            return Result;
        }

        public bool ResultChanged()
        {
            return Changed;
        }
    }
}