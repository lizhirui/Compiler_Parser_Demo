using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static Compiler_Parser_Demo_WPF.DFAGenerator;

namespace Compiler_Parser_Demo_WPF
{
    class DFAOptimizer : ITask
    {
        private bool Changed = false;
        private List<List<DFANode>> DFASplitSet;
        private Dictionary<DFANode,int> DFANodeSetID;

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

        private ResultInfo DFAInfo;
        private ResultItem CurDFA;

        private bool ImageOutputEnable = true;

        public DFAOptimizer()
        {
            
        }

        private DFANode DFANodeClone_DFS(Dictionary<DFANode,DFANode> NodeMap,DFANode CurNode)
        {
            if(NodeMap.ContainsKey(CurNode))
            {
                return NodeMap[CurNode];
            }

            var newnode = new DFANode{ID = CurNode.ID,IsEndNode = CurNode.IsEndNode,Edge = new List<DFAEdge>()};
            NodeMap[CurNode] = newnode;

            foreach(var item in CurNode.Edge)
            {
                var newedge = new DFAEdge{Condition = item.Condition,NextNode = null};

                if(item.NextNode != null)
                {
                    newedge.NextNode = DFANodeClone_DFS(NodeMap,item.NextNode);
                }

                newnode.Edge.Add(newedge);
            }

            return newnode;
        }

        private DFANode DFANodeClone(DFANode CurNode)
        {
            return DFANodeClone_DFS(new Dictionary<DFANode,DFANode>(),CurNode);
        }

        private ResultInfo DFAInfoClone(ResultInfo Origin)
        {
            for(var i = 0;i < Origin.Item.Length;i++)
            {
                Origin.Item[i].CharSet = Origin.Item[i].CharSet.ToArray();
                Origin.Item[i].StartNode = DFANodeClone(Origin.Item[i].StartNode);
            }

            return Origin;
        }

        private string GetTargetSet(DFANode Node)
        {
            var r = new StringBuilder();
            var nextnode = new Dictionary<char,DFANode>();

            foreach(var edge in Node.Edge)
            {
                nextnode[edge.Condition] = edge.NextNode;
            }

            foreach(var a in CurDFA.CharSet)
            {
                if(!nextnode.ContainsKey(a))
                {
                    r.Append("-1,");
                }
                else
                {
                    r.Append(DFANodeSetID[nextnode[a]] + ",");
                }
            }

            return r.ToString();
        }

        private void SplitSet(Queue<int> SetQueue,int DFANodeSet)
        {
            var setnodelist = new Dictionary<string,List<DFANode>>();

            foreach(var node in DFASplitSet[DFANodeSet])
            {
                var setstr = GetTargetSet(node);

                if(!setnodelist.ContainsKey(setstr))
                {
                    setnodelist[setstr] = new List<DFANode>();
                }

                setnodelist[setstr].Add(node);
            }

            if(setnodelist.Keys.Count > 1)
            {
                var keycnt = 0;

                foreach(var key in setnodelist.Keys)
                {
                    var setid = DFANodeSet;

                    if(keycnt == 0)
                    {
                        DFASplitSet[DFANodeSet] = setnodelist[key];

                        foreach(var item in setnodelist[key])
                        {
                            DFANodeSetID[item] = DFANodeSet;
                        }
                    }
                    else
                    {
                        setid = DFASplitSet.Count;

                        foreach(var item in setnodelist[key])
                        {
                            DFANodeSetID[item] = DFASplitSet.Count;
                        }

                        DFASplitSet.Add(setnodelist[key]);
                    }

                    if(setnodelist[key].Count > 1)
                    {
                        SetQueue.Enqueue(setid);
                    }

                    keycnt++;
                }
            }
        }

        public void SetImageOutputEnable(bool ImageOutputEnable)
        {
            this.ImageOutputEnable = ImageOutputEnable;
        }

        public bool GetImageOutputEnable()
        {
            return ImageOutputEnable;
        }

        private void AnalysisDFA()
        {
            DFASplitSet = new List<List<DFANode>>();
            DFANodeSetID = new Dictionary<DFANode,int>();

            var queue = new Queue<DFANode>();
            var visited = new HashSet<DFANode>();

            //Split Non-Z or Z node
            DFASplitSet.Add(new List<DFANode>());//Non-Z node set
            DFASplitSet.Add(new List<DFANode>());//Z node set

            queue.Enqueue(CurDFA.StartNode);
            visited.Add(CurDFA.StartNode);

            while(queue.Count > 0)
            {
                var curnode = queue.Dequeue();

                if(curnode.IsEndNode)
                {
                    DFASplitSet[1].Add(curnode);
                    DFANodeSetID[curnode] = 1;
                }
                else
                {
                    DFASplitSet[0].Add(curnode);
                    DFANodeSetID[curnode] = 0;
                }

                foreach(var item in curnode.Edge)
                {
                    if(!visited.Contains(item.NextNode))
                    {
                        queue.Enqueue(item.NextNode);
                        visited.Add(item.NextNode);
                    }
                }
            }

            //Split SubSet
            var setqueue = new Queue<int>();
            
            for(var i = 0;i < DFASplitSet.Count;i++)
            {
                if(DFASplitSet[i].Count > 1)
                {
                    setqueue.Enqueue(i);
                }
            }
            
            while(setqueue.Count > 0)
            {
                var curset = setqueue.Dequeue();
                SplitSet(setqueue,curset);
            }

            //Generate DFA MapTable
            var DFAMap = new Dictionary<DFANode,DFANode>();

            foreach(var setlist in DFASplitSet)
            {
                if(setlist.Count > 1)
                {
                    var mainnode = setlist[0];

                    for(var i = 1;i < setlist.Count;i++)
                    {
                        DFAMap[setlist[i]] = mainnode;
                    }
                }
            }

            //Modify DFA
            queue.Clear();
            visited.Clear();
            queue.Enqueue(CurDFA.StartNode);
            visited.Add(CurDFA.StartNode);
            
            while(queue.Count > 0)
            {
                var curnode = queue.Dequeue();

                for(var i = 0;i < curnode.Edge.Count;i++)
                {
                    if(DFAMap.ContainsKey(curnode.Edge[i].NextNode))
                    {
                        var t = curnode.Edge[i];
                        t.NextNode = DFAMap[curnode.Edge[i].NextNode];
                        curnode.Edge[i] = t;
                    }

                    if(!visited.Contains(curnode.Edge[i].NextNode))
                    {
                        queue.Enqueue(curnode.Edge[i].NextNode);
                        visited.Add(curnode.Edge[i].NextNode);
                    }
                }
            }
        }

        public void Analysis(ResultInfo DFAInfo)
        {
            DFAInfo = DFAInfoClone(DFAInfo);
            this.DFAInfo = DFAInfo;

            foreach(var item in DFAInfo.Item)
            {
                CurDFA = item;
                AnalysisDFA();
            }

            Result = DFAInfo;

            var rimage = new List<BitmapImage>();

            foreach(var item in DFAInfo.Item)
            {
                rimage.Add(!ImageOutputEnable ? new BitmapImage() : DFAGenerator_DiagramGenerator.ToImage(item));
            }

            ResultImage = rimage.ToArray();
        }

        public bool MoveFrom(object obj)
        {
            Changed = true;
            Analysis((ResultInfo)obj);
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