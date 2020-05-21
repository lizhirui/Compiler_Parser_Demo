using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Compiler_Parser_Demo_WPF
{
    class GrammarCompiler_LL_1 : IGrammarCompiler
    {
        struct NonTerminalProductionInfo
        {
            public string Name;
            public List<NonTerminalProductionItemInfo> Item;
        }

        struct NonTerminalProductionItemInfo
        {
            public List<string> Content;
        }

        struct ProductionItem
        {
            public string Name;
            public string[] Content;
        }

        private DFAPriorityGenerator.ResultInfo ProductionInfo;
        private List<NonTerminalProductionInfo> NewNTPList;
        private string StartSymbol;
        private Dictionary<string,HashSet<string>> FirstSet = new Dictionary<string,HashSet<string>>();
        private Dictionary<string,HashSet<string>> FollowSet = new Dictionary<string,HashSet<string>>();
        private ProductionItem[,] PredictAnalysisTable = null;
        private Dictionary<string,int> TPMap = new Dictionary<string,int>();
        private Dictionary<string,int> NTPMap = new Dictionary<string,int>();
        private string CompileResult = "";
        private string TestResult = "";

        public string GetMajorType()
        {
            return "自顶向下分析法";
        }

        public string GetMinorType()
        {
            return "LL(1)";
        }

        /// <summary>
        /// 消除公因子
        /// </summary>
        private void EliminateCommonFactor()
        {
            var n = NewNTPList.Count;
            var newntpid = 0;
            var newntpnameprefix = Production_Parser.SysNamePrefix + "U_";

            for(var i = 0;i < n;i++)
            {
                var lastdiff = 0;
                var hasdiff = false;
                var minn = NewNTPList[i].Item[0].Content.Count;

                for(var j = 1;j < NewNTPList[i].Item.Count;j++)
                {
                    minn = Math.Min(minn,NewNTPList[i].Item[j].Content.Count);
                }

                while(!hasdiff && lastdiff < minn)
                {
                    var curitem = NewNTPList[i].Item[0].Content[lastdiff];

                    for(var j = 1;j < NewNTPList[i].Item.Count;j++)
                    {
                        if(curitem != NewNTPList[i].Item[j].Content[lastdiff])
                        {
                            hasdiff = true;
                            break;
                        }
                    }

                    if(!hasdiff)
                    {
                        lastdiff++;
                    }
                }

                if(lastdiff > 0)
                {
                    //存在公共部分，需要分离
                    var needepsilon = false;
                    //存在重复项，意味着分离出的非公共部分需要包含epsilon
                    if(lastdiff == minn)
                    {
                        needepsilon = true;
                    }

                    var newntp = new NonTerminalProductionInfo();
                    newntp.Name = newntpnameprefix + (newntpid++);
                    newntp.Item = new List<NonTerminalProductionItemInfo>();
                    var hadepsilon = false;

                    //加入非公共部分
                    foreach(var pitem in NewNTPList[i].Item)
                    {
                        if(pitem.Content.Count > lastdiff)
                        {
                            var isepsilon = true;

                            foreach(var item in pitem.Content)
                            {
                                if(item != "epsilon")
                                {
                                    isepsilon = false;
                                }
                            }

                            //判断是否为epsilon项，若是，则设定needepsilon标记留作后面处理
                            if(isepsilon)
                            {
                                if(!hadepsilon)
                                {
                                    hadepsilon = true;
                                    needepsilon = true;
                                }
                            }
                            else
                            {
                                var newcontent = new NonTerminalProductionItemInfo();
                                newcontent.Content = new List<string>();

                                for(var j = lastdiff;j < pitem.Content.Count;j++)
                                {
                                    newcontent.Content.Add(pitem.Content[j]);
                                }

                                newntp.Item.Add(newcontent);
                            }
                        }
                    }

                    //加入epsilon项
                    if(needepsilon)
                    {
                        newntp.Item.Add(new NonTerminalProductionItemInfo{Content = new string[]{"epsilon"}.ToList()});
                    }

                    //清空原产生式所有项，仅保留公共项，并在最后追加到该新产生式的引用
                    var content = NewNTPList[i].Item[0];
                    NewNTPList[i].Item.Clear();
                    content.Content.RemoveRange(lastdiff,content.Content.Count - lastdiff);
                    content.Content.Add(newntp.Name);
                    NewNTPList[i].Item.Add(content);
                    NewNTPList.Add(newntp);
                }
            }
        }

        /// <summary>
        /// 消除左递归
        /// </summary>
        private bool EliminateLeftRecursion(out string ErrorText)
        {
            ErrorText = "";
            var ntplist = ProductionInfo.Production_ParserResult.ntplist;
            //首先建立矩阵方程X = XA + B，其中X是1 * n的，A是n * n的，B是1 * n的，n为非终结符的总数
            var n = ntplist.Length;
            var ntpmap = new Dictionary<string,int>();//非终结符映射表
            var X = new string[n];

            for(var i = 0;i < n;i++)
            {
                X[i] = ntplist[i].Name;
                ntpmap[X[i]] = i;
            }

            var A = new List<List<string>>[n,n];
            var B = new List<string[]>[n];

            for(var i = 0;i < n;i++)
            {
                for(var j = 0;j < n;j++)
                {
                    A[i,j] = null;
                }
            }

            for(var i = 0;i < n;i++)
            {
                foreach(var item in ntplist[i].Item)
                {
                    var headerindex = 0;

                    while(headerindex < item.Content.Length && item.Content[headerindex] == "epsilon")
                    {
                        headerindex++;
                    }

                    if(headerindex >= item.Content.Length)
                    {
                        headerindex = item.Content.Length - 1;//全为epsilon，留出一项即可
                    }

                    if(ntpmap.ContainsKey(item.Content[headerindex]))
                    {
                        //开头为非终结符，表明该项应该属于A矩阵
                        var line = ntpmap[item.Content[headerindex]];

                        //第line行第i列表示第i个产生式的第line项
                        if(A[line,i] == null)
                        {
                            A[line,i] = new List<List<string>>();
                        }

                        //创建一个新列表，将头非终结符之后的所有的符号全部加入该列表并追加到A矩阵当前项中
                        var list = new List<string>();

                        for(var j = headerindex + 1;j < item.Content.Length;j++)
                        {
                            list.Add(item.Content[j]);
                        }

                        //防止程序崩溃的保护措施
                        if(list.Count == 0)
                        {
                            list.Add("epsilon");
                        }

                        A[line,i].Add(list);
                    }
                    else
                    {
                        //开头为终结符，表示该项应该属于B矩阵
                        if(B[i] == null)
                        {
                            B[i] = new List<string[]>();
                        }

                        B[i].Add(item.Content);
                    }
                }
            }

            //方程转化为X = BZ,Z = I + AZ
            //将方程转化为产生式序列
            var newntplist = new List<NonTerminalProductionInfo>();
            var newntpitemprefex = Production_Parser.SysNamePrefix + "Z_";
            var nullZ = new HashSet<string>();

            //生成Z产生式
            for(var i = 0;i < n;i++)
            {
                for(var j = 0;j < n;j++)
                {
                    var newntp = new NonTerminalProductionInfo();

                    newntp.Name = newntpitemprefex + (i + 1) + "_" + (j + 1);
                    newntp.Item = new List<NonTerminalProductionItemInfo>();

                    for(var k = 0;k < n;k++)
                    {
                        if(A[i,k] != null)
                        {
                            foreach(var pitem in A[i,k])
                            {
                                var newcontent = new NonTerminalProductionItemInfo();
                                newcontent.Content = pitem.ToList();
                                newcontent.Content.Add(newntpitemprefex + (k + 1) + "_" + (j + 1));
                                newntp.Item.Add(newcontent);
                            }
                        }
                    }

                    //+ I
                    if(i == j)
                    {
                        newntp.Item.Add(new NonTerminalProductionItemInfo{Content = new string[]{"epsilon"}.ToList()});                        
                    }

                    if(newntp.Item.Count == 0)
                    {
                        nullZ.Add(newntp.Name);
                    }
                    else
                    {
                        newntplist.Add(newntp);
                    }
                }
            }

            //移除所有无效的Z产生式
            var updated = true;

            while(updated)
            {
                updated = false;

                for(var i = newntplist.Count - 1;i >= 0;i--)
                {
                    for(var j = newntplist[i].Item.Count - 1;j >= 0;j--)
                    {
                        var hasunknownitem = false;

                        foreach(var item in newntplist[i].Item[j].Content)
                        {
                            if(nullZ.Contains(item))
                            {
                                hasunknownitem = true;
                                break;
                            }
                        }

                        if(hasunknownitem)
                        {
                            newntplist[i].Item.RemoveAt(j);   
                            updated = true;
                        }
                    }

                    if(newntplist[i].Item.Count == 0)
                    {
                        nullZ.Add(newntplist[i].Name);
                        newntplist.RemoveAt(i);
                        updated = true;
                    }
                }
            }

            //产生式选择
            for(var i = 0;i < n;i++)
            {
                var newntp = new NonTerminalProductionInfo();

                newntp.Name = X[i];
                newntp.Item = new List<NonTerminalProductionItemInfo>();

                //遍历B矩阵的每一项，与Z矩阵进行相乘操作
                var hasnonnull = false;

                for(var j = 0;j < n;j++)
                {
                    if(B[j] != null && !nullZ.Contains(newntpitemprefex + (j + 1) + "_" + (i + 1)))
                    {
                        hasnonnull = true;

                        foreach(var pitem in B[j])
                        {
                            var newcontent = new NonTerminalProductionItemInfo();
                            newcontent.Content = pitem.ToList();
                            newcontent.Content.Add(newntpitemprefex + (j + 1) + "_" + (i + 1));
                            newntp.Item.Add(newcontent);
                        }
                    }
                }

                if(!hasnonnull)
                {
                    ErrorText = "[GrammarCompiler_LL_1]:该文法不是LL(1)文法！在进行左递归消除时，对于非终结符\"" + newntp.Name + "\"而言，B矩阵各项全为null，疑似存在循环引用！\n";
                    return false;
                }

                newntplist.Add(newntp);
            }

            //删除多余产生式，采用从开始符号的引用分析方法
            var visited = new Dictionary<string,bool>();

            for(var i = 0;i < newntplist.Count;i++)
            {
                visited[newntplist[i].Name] = false;
                ntpmap[newntplist[i].Name] = i;
            }

            var queue = new Queue<string>();

            queue.Enqueue(StartSymbol);
            visited[StartSymbol] = true;

            while(queue.Count > 0)
            {
                var curntp = queue.Dequeue();

                foreach(var pitem in newntplist[ntpmap[curntp]].Item)
                {
                    foreach(var item in pitem.Content)
                    {
                        if(visited.ContainsKey(item) && !visited[item])
                        {
                            visited[item] = true;
                            queue.Enqueue(item);
                        }
                    }
                }
            }

            for(var i = newntplist.Count - 1;i >= 0;i--)
            {
                if(!visited[newntplist[i].Name])
                {
                    newntplist.RemoveAt(i);
                }
            }

            NewNTPList = newntplist;
            return true;
        }

        /// <summary>
        /// 生成产生式代码
        /// </summary>
        private string GenerateProductionCode()
        {
            var stb = new StringBuilder();

            foreach(var item in NewNTPList)
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

                if(item.Item.Count > 0)
                {
                    stb.Append("\n");
                }
            }

            foreach(var item in ProductionInfo.Production_ParserResult.tplist)
            {
                stb.Append(item.Name + " -> \"" + item.RegularExpression + "\";\n");
            }

            return stb.ToString();
        }

        private void EliminateEpsilon()
        {
            //1、若对于某个产生式，其有多项为epsilon，则先只保留一项，或对于某一项有连续若干个epsilon，则只保留一个epsilon
            //2、若对于某个产生式，其某一项中包含epsilon但该项又不是仅含有epsilon的，则删除其中所有的epsilon
            //3、若对于某个产生式，仅有一项且该项为epsilon，则将其他引用该项的产生式的部分或式中删除该产生式符号，最后删除该产生式中的epsilon
            //4、若对于某个产生式，其中已无任何项，则该产生式一定无任何引用，直接删除
            //以上4个步骤连续执行直到无更新为止
            
            //先进行前两步操作，同时创建epsilon项的初始表，同时删除加入初始表中的项的所有epsilon
            var updated = true;

            while(updated)
            {
                updated = false;
                var epsilonsymbol = new HashSet<string>();

                foreach(var pitem in NewNTPList)
                {
                    var epsiloncount = 0;
                    var curitemindex = 0;
                    var firstepsilonindex = 0;

                    foreach(var item in pitem.Item)
                    {
                        var has_epsilon = false;
                        var only_has_epsilon = true;

                        foreach(var titem in item.Content)
                        {
                            if(titem == "epsilon")
                            {
                                has_epsilon = true;
                            }
                            else
                            {
                                only_has_epsilon = false;
                            }
                        }

                        if(has_epsilon)
                        {
                            if(only_has_epsilon)
                            {
                                if(epsiloncount == 0)
                                {
                                    firstepsilonindex = curitemindex;
                                }

                                epsiloncount++;

                                if(item.Content.Count > 1)
                                {
                                    item.Content.RemoveRange(1,item.Content.Count - 1);
                                    updated = true;
                                }
                            }
                            else
                            {
                                for(var i = item.Content.Count - 1;i >= 0;i--)
                                {
                                    if(item.Content[i] == "epsilon")
                                    {
                                        item.Content.RemoveAt(i);
                                        updated = true;
                                    }
                                }
                            }
                        }

                        curitemindex++;
                    }

                    if(epsiloncount > 0)
                    {
                        for(var i = pitem.Item.Count - 1;i > firstepsilonindex;i--)
                        {
                            if(pitem.Item[i].Content[0] == "epsilon")
                            {
                                pitem.Item.RemoveAt(i);
                                updated = true;
                            }
                        }

                        epsilonsymbol.Add(pitem.Name);
                    }
                }

                //进行第三步操作
                var nullset = new HashSet<string>();

                for(var i = NewNTPList.Count - 1;i >= 0;i--)
                {
                    if(NewNTPList[i].Item.Count == 1 && NewNTPList[i].Item[0].Content[0] == "epsilon")
                    {
                        nullset.Add(NewNTPList[i].Name);
                        NewNTPList.RemoveAt(i);
                        updated = true;
                    }
                }

                for(var i = 0;i < NewNTPList.Count;i++)
                {
                    for(var j = 0;j < NewNTPList[i].Item.Count;j++)
                    {
                        for(var k = 0;k < NewNTPList[i].Item[j].Content.Count;k++)
                        {
                            if(nullset.Contains(NewNTPList[i].Item[j].Content[k]))
                            {
                                NewNTPList[i].Item[j].Content[k] = "epsilon";
                                updated = true;
                            }
                        }
                    }
                }
            }
        }

        private void NTPListClone()
        {
            var ntplist = ProductionInfo.Production_ParserResult.ntplist;
            NewNTPList = new List<NonTerminalProductionInfo>();

            foreach(var pitem in ntplist)
            {
                var newinfo = new NonTerminalProductionInfo{Name = pitem.Name,Item = new List<NonTerminalProductionItemInfo>()};

                foreach(var item in pitem.Item)
                {
                    newinfo.Item.Add(new NonTerminalProductionItemInfo{Content = item.Content.ToList()});
                }

                NewNTPList.Add(newinfo);
            }
        }

        /// <summary>
        /// 生成First集
        /// </summary>
        private void GenerateFirstSet()
        {
            FirstSet.Clear();
            var tplist = ProductionInfo.Production_ParserResult.tplist;

            foreach(var item in tplist)
            {
                if(!FirstSet.ContainsKey(item.Name))
                {
                    FirstSet[item.Name] = new HashSet<string>();
                }

                FirstSet[item.Name].Add(item.Name);
            }

            foreach(var item in NewNTPList)
            {
                if(!FirstSet.ContainsKey(item.Name))
                {
                    FirstSet[item.Name] = new HashSet<string>();
                }
            }

            var updated = true;

            while(updated)
            {
                updated = false;

                foreach(var item in NewNTPList)
                {
                    foreach(var pitem in item.Item)
                    {
                        var onlyepsilon = true;

                        foreach(var citem in pitem.Content)
                        {
                            if(citem != "epsilon")
                            {
                                foreach(var yitem in FirstSet[citem])
                                {
                                    if(!FirstSet[item.Name].Contains(yitem))
                                    {
                                        updated = true;
                                        FirstSet[item.Name].Add(yitem);
                                    }
                                }

                                if(!FirstSet[citem].Contains("epsilon"))
                                {
                                    onlyepsilon = false;  
                                    break;
                                }
                            }
                        }

                        if(onlyepsilon && !FirstSet[item.Name].Contains("epsilon"))
                        {
                            updated = true;
                            FirstSet[item.Name].Add("epsilon");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 生成Follow集
        /// </summary>
        private void GenerateFollowSet()
        {
            var ntpmap = new Dictionary<string,int>();
            FollowSet.Clear();
            var id = 0;

            foreach(var item in NewNTPList)
            {
                FollowSet[item.Name] = new HashSet<string>();
                ntpmap[item.Name] = id++;
            }

            //开始符号的FOLLOW集中包含结束符号
            FollowSet[StartSymbol].Add("#");

            //开始符号若以非终结符结尾，则该非终结符的FOLLOW集中包含结束符号
            foreach(var pitem in NewNTPList[ntpmap[StartSymbol]].Item)
            {
                if(ntpmap.ContainsKey(pitem.Content[pitem.Content.Count - 1]))
                {
                    FollowSet[pitem.Content[pitem.Content.Count - 1]].Add("#");
                }
            }

            var updated = true;

            while(updated)
            {
                updated = false;

                foreach(var item in NewNTPList)
                {
                    foreach(var pitem in item.Item)
                    {
                        for(var i = 0;i < (pitem.Content.Count - 1);i++)
                        {
                            if(FollowSet.ContainsKey(pitem.Content[i]))
                            {
                                foreach(var yitem in FirstSet[pitem.Content[i + 1]])
                                {
                                    if(yitem != "epsilon" && !FollowSet[pitem.Content[i]].Contains(yitem))
                                    {
                                        FollowSet[pitem.Content[i]].Add(yitem);
                                        updated = true;
                                    }
                                }
                            }
                        }

                        for(var i = pitem.Content.Count - 1;i >= 0;i--)
                        {
                            if(i == pitem.Content.Count - 1 || FirstSet[pitem.Content[i + 1]].Contains("epsilon"))
                            {
                                if(FollowSet.ContainsKey(pitem.Content[i]))
                                {
                                    foreach(var yitem in FollowSet[item.Name])
                                    {
                                        if(!FollowSet[pitem.Content[i]].Contains(yitem))
                                        {
                                            FollowSet[pitem.Content[i]].Add(yitem);
                                            updated = true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            
        }

        private HashSet<string> GetProductionFirstSet(NonTerminalProductionItemInfo pitem)
        {
            var r = new HashSet<string>();
            var onlyepsilon = true;

            foreach(var citem in pitem.Content)
            {
                if(citem != "epsilon")
                {
                    foreach(var yitem in FirstSet[citem])
                    {
                        r.Add(yitem);
                    }

                    if(!FirstSet[citem].Contains("epsilon"))
                    {
                        onlyepsilon = false;  
                        break;
                    }
                }
            }

            if(onlyepsilon)
            {
                r.Add("epsilon");
            }

            return r;
        }

        /// <summary>
        /// 生成预测分析表
        /// </summary>
        private bool GeneratePredictAnalysisTable(out string ErrorText)
        {
            ErrorText = "";
            //首先建立终结符/非终结符->数值的映射，为建立预测分析表做准备
            TPMap.Clear();
            NTPMap.Clear();
            var tplist = ProductionInfo.Production_ParserResult.tplist;

            var id = 0;
            CompileResult = "";

            foreach(var item in ProductionInfo.Production_ParserResult.tplist)
            {
                TPMap[item.Name] = id++;
            }

            TPMap["#"] = id++;
            id = 0;

            foreach(var item in NewNTPList)
            {
                NTPMap[item.Name] = id++;
            }

            //初始化预测分析表
            PredictAnalysisTable = new ProductionItem[NewNTPList.Count,tplist.Length + 1];

            for(var i = 0;i < NewNTPList.Count;i++)
            {
                //包括一个终止符
                for(var j = 0;j <= tplist.Length;j++)
                {
                    PredictAnalysisTable[i,j] = new ProductionItem{Name = null,Content = null};
                }
            }

            //开始建立预测分析表
            foreach(var item in NewNTPList)
            {
                var ntpid = NTPMap[item.Name];
                var has_epsilon = false;

                foreach(var pitem in item.Item)
                {
                    foreach(var sitem in GetProductionFirstSet(pitem))
                    {
                        if(sitem != "epsilon")
                        {
                            if(PredictAnalysisTable[ntpid,TPMap[sitem]].Name == null)
                            {
                                PredictAnalysisTable[ntpid,TPMap[sitem]].Name = item.Name;
                                PredictAnalysisTable[ntpid,TPMap[sitem]].Content = pitem.Content.ToArray();
                            }
                            else
                            {
                                ErrorText = "[GrammarCompiler_LL_1]:该文法不是LL(1)文法！在预测分析表中当非终结符为\"" + item.Name + "\"且终结符为\"" + sitem + "\"时发生冲突！\n";
                                return false;
                            }
                        }
                        else
                        {
                            has_epsilon = true;
                        }
                    }
                }

                if(has_epsilon)
                {
                    foreach(var sitem in FollowSet[item.Name])
                    {
                        if(PredictAnalysisTable[ntpid,TPMap[sitem]].Name == null)
                        {
                            PredictAnalysisTable[ntpid,TPMap[sitem]].Name = item.Name;
                            PredictAnalysisTable[ntpid,TPMap[sitem]].Content = new string[]{"epsilon"};
                        }
                        else
                        {
                            ErrorText = "[GrammarCompiler_LL_1]:该文法不是LL(1)文法！在预测分析表中当非终结符为\"" + item.Name + "\"且终结符为\"" + sitem + "\"时发生冲突！\n";
                            return false;
                        }
                    }
                }
            }

            //生成结果HTML代码
            var stb = new StringBuilder();
            stb.Append("<!DOCTYPE html><html><head><meta http-equiv=\"Content-Type\" content=\"text/html;charset=utf-8\" /><style>*{font-family:Consolas;color:#c8c8c8;background-color:#1e1e1e;}table,th,tr,td{border-collapse:collapse;border-color:#c8c8c8;}html{overflow:scroll;}td{white-space:nowrap;}</style></head><body>预测分析表：<br />");
            stb.Append("<table border=\"1\">");
            stb.Append("<tr><td></td>");

            foreach(var item in TPMap)
            {
                stb.Append("<td>" + item.Key + "</td>");
            }

            stb.Append("</tr>");

            foreach(var ntp in NTPMap)
            {
                stb.Append("<tr><td>" + ntp.Key + "</td>");

                foreach(var tp in TPMap)
                {
                    var content = "";

                    if(PredictAnalysisTable[ntp.Value,tp.Value].Name != null)
                    {
                        content = PredictAnalysisTable[ntp.Value,tp.Value].Name + " ->";

                        foreach(var item in PredictAnalysisTable[ntp.Value,tp.Value].Content)
                        {
                            content += " &lt;" + item + "&gt;";
                        }
                    }

                    stb.Append("<td>" + content + "</td>");
                }

                stb.Append("</tr>");
            }

            stb.Append("</table></body></html>");
            CompileResult = stb.ToString();
            ErrorText = "[GrammarCompiler_LL_1]:Execute OK!\n";
            return true;
        }

        public bool Compile(DFAPriorityGenerator.ResultInfo ProductionInfo,string StartSymbol,out string ErrorText)
        {
            this.ProductionInfo = ProductionInfo;
            this.StartSymbol = StartSymbol;
            NTPListClone();
            //EliminateEpsilon();
            /*if(!EliminateLeftRecursion(out ErrorText))
            {
                return false;
            }*/

            //EliminateCommonFactor();
            EliminateEpsilon();
            GenerateFirstSet();
            GenerateFollowSet();
            //Clipboard.SetText(GenerateProductionCode());
            var r = GeneratePredictAnalysisTable(out ErrorText);
            return r;
        }

        public bool Test(List<LexerWordInfo> WordList,out string ErrorText)
        {
            var stb = new StringBuilder();
            stb.Append("<!DOCTYPE html><html><head><meta http-equiv=\"Content-Type\" content=\"text/html;charset=utf-8\" /><style>*{font-family:Consolas;color:#c8c8c8;background-color:#1e1e1e;}table,th,tr,td{border-collapse:collapse;border-color:#c8c8c8;}html{overflow:scroll;}td{white-space:nowrap;}</style></head><body>分析过程：<br />");
            stb.Append("<table border=\"1\">");
            stb.Append("<tr><td>分析栈</td><td>剩余字符</td><td>所用产生式</td></tr>");
            ErrorText = "";
            var stack = new Stack<string>();
            var tplist = ProductionInfo.Production_ParserResult.tplist;

            stack.Push("#");
            stack.Push(StartSymbol);
            var index = 0;

            try
            {
                while(true)
                {
                    var tpname = (index < WordList.Count) ? tplist[WordList[index].DFA.TerminalID].Name : "#";
                    var tpid = TPMap[tpname];
                    stb.Append("<tr><td>");

                    foreach(var item in stack)
                    {
                        stb.Append((item == "#") ? "#" : ("&lt;" + item + "&gt"));
                    }

                    stb.Append("</td><td>");

                    for(var i = index;i < WordList.Count;i++)
                    {
                        stb.Append("&lt;" + tplist[WordList[i].DFA.TerminalID].Name + "&gt;");
                    }

                    stb.Append("<td>");

                    var curtop = stack.Pop();

                    if(TPMap.ContainsKey(curtop))
                    {
                        if(curtop == "#")
                        {
                            if(tpname != "#")
                            {
                                ErrorText = "期望终止标记，遇到\"" + tpname + "\"";
                                throw new Exception();
                            }
                            else
                            {
                                ErrorText = "检测通过，输入串完全匹配!";
                                throw new Exception("true");
                            }
                        }
                        else
                        {
                            if(tpname == curtop)
                            {
                                index++;
                            }
                            else
                            {
                                ErrorText = "期望\"" + curtop + "\"，遇到\"" + tpname + "\"";
                                throw new Exception();
                            }
                        }
                    }
                    else if(curtop == "epsilon")
                    {
                    
                    }
                    else
                    {
                        if(PredictAnalysisTable[NTPMap[curtop],tpid].Name == null)
                        {
                            ErrorText = "不期望的词：" + (tpname == "#" ? "终止标记" : "\"" + tpname + "\"");
                            throw new Exception();
                        }
                        else
                        {
                            var content = PredictAnalysisTable[NTPMap[curtop],tpid].Content;
                            stb.Append(PredictAnalysisTable[NTPMap[curtop],tpid].Name + " ->");
                            
                            foreach(var item in content)
                            {
                                stb.Append(" &lt;" + item + "&gt;");
                            }

                            for(var i = content.Length - 1;i >= 0;i--)
                            {
                                stack.Push(content[i]);
                            }
                        }
                    }

                    stb.Append("</td></tr>");
                }
            }
            catch(Exception ex)
            {
                return ex.Message == "true";
            }
            finally
            {
                stb.Append("</table></body></html>");
                TestResult = stb.ToString();
            }
        }

        public string GetCompileResult()
        {
            return CompileResult;
        }

        public string GetTestResult()
        {
            return TestResult;
        }
    }
}
