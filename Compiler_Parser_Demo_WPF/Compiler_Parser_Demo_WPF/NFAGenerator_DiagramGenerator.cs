using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static Compiler_Parser_Demo_WPF.NFAGenerator_Parser;

namespace Compiler_Parser_Demo_WPF
{
    static class NFAGenerator_DiagramGenerator
    {
        private static List<string> VertexList;
        private static List<string[]> EdgeList;
        private static int NewStateID = 0;
        private static Dictionary<NFANode,string> StateNameMap;

        private static void DFS(NFANode CurNode)
        {
            var curname = StateNameMap[CurNode];

            foreach(var item in CurNode.Edge)
            {
                if(!StateNameMap.ContainsKey(item.NextNode))
                {
                    var newname = "Q" + (NewStateID++);
                    StateNameMap.Add(item.NextNode,newname);
                    VertexList.Add(newname);
                    EdgeList.Add(new string[3]{curname,newname,item.Epsilon ? "" : item.Condition + ""});
                    DFS(item.NextNode);
                }
                else
                {
                    EdgeList.Add(new string[3]{curname,StateNameMap[item.NextNode],item.Epsilon ? "" : item.Condition + ""});
                }
            }
        }
        
        public static BitmapImage ToImage(ResultItem NFAInfo)
        {
            VertexList = new List<string>();
            EdgeList = new List<string[]>();
            NewStateID = 0;
            StateNameMap = new Dictionary<NFANode,string>();

            //Add StartNode
            VertexList.Add("S");
            StateNameMap.Add(NFAInfo.StartNode,"S");

            //Add EndNode
            VertexList.Add("Z");
            StateNameMap.Add(NFAInfo.EndNode,"Z");

            DFS(NFAInfo.StartNode);

            return StatechartDiagram.GetDiagram(VertexList,EdgeList);
        }
    }
}
