using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static Compiler_Parser_Demo_WPF.DFAGenerator;

namespace Compiler_Parser_Demo_WPF
{

    static class DFAGenerator_DiagramGenerator
    {
        private static List<string> VertexList;
        private static List<string[]> EdgeList;
        private static int NewStateID = 0;
        private static Dictionary<DFANode,string> StateNameMap;

        private static void DFS(DFANode CurNode)
        {
            var curname = StateNameMap[CurNode];

            foreach(var item in CurNode.Edge)
            {
                if(!StateNameMap.ContainsKey(item.NextNode))
                {
                    var newname = (item.NextNode.IsEndNode ? "Z" : "Q") + (NewStateID++);
                    StateNameMap.Add(item.NextNode,newname);
                    VertexList.Add(newname);
                    EdgeList.Add(new string[3]{curname,newname,item.Condition + ""});
                    DFS(item.NextNode);
                }
                else
                {
                    EdgeList.Add(new string[3]{curname,StateNameMap[item.NextNode],item.Condition + ""});
                }
            }
        }
        
        public static BitmapImage ToImage(ResultItem NFAInfo)
        {
            VertexList = new List<string>();
            EdgeList = new List<string[]>();
            NewStateID = 0;
            StateNameMap = new Dictionary<DFANode,string>();

            //Add StartNode
            VertexList.Add("S");
            StateNameMap.Add(NFAInfo.StartNode,"S");

            DFS(NFAInfo.StartNode);

            return StatechartDiagram.GetDiagram(VertexList,EdgeList);
        }
    }
}