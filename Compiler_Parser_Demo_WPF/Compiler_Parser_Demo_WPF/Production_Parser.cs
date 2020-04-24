using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler_Parser_Demo_WPF
{
    class Production_Parser
    {
        private class ParserAnalysisException : Exception
        {
            
        }

        public struct NonTerminalProductionInfo
        {
            public string Name;
            public NonTerminalProductionItemInfo[] Item;
        }

        public struct NonTerminalProductionItemInfo
        {
            public string[] Content;
        }

        public struct TerminalProductionInfo
        {
            public string Name;
            public string RegularExpression;
        }

        public NonTerminalProductionInfo[] NonTerminalProductionResult
        {
            get;
            private set;
        }

        public TerminalProductionInfo[] TerminalProductionResult
        {
            get;
            private set;
        }

        public string ErrorMsg
        {
            get;
            private set;
        }

        public Production_Lexer.WordInfo[] WordList
        {
            get;
            private set;
        }

        private int CurIndex = 0;
        private Dictionary<string,int> ProductionNameMap;
        private Dictionary<string,int> TerminalProductionNameMap;
        private HashSet<string> WaitCheckReference;
        private List<KeyValuePair<string,int>> WaitCheckReferenceList;
        private Dictionary<string,string> TerminalNameMap;
        private const string SysNamePrefix = "__ParserSystem_";
        private int CurSysNameID = 0;

        public Production_Parser()
        {
            ErrorMsg = "";
        }

        private void GenerateError(string ErrorTitle)
        {
            GenerateError(ErrorTitle,"");
        }

        private void GenerateError(string ErrorTitle,string ErrorString)
        {
            ErrorMsg = "[Parser]:" + ErrorTitle + "（行" + WordList[CurIndex].Line + "字符" + WordList[CurIndex].Column + "）";

            if(ErrorString != "")
            {
                ErrorMsg += ":\"" + ErrorString + "\"";
            }

            ErrorMsg += '\n';
            throw new ParserAnalysisException();
        }

        private void AnalysisLine(List<NonTerminalProductionInfo> ntplist)
        {
            if(WordList[CurIndex].Type != Production_Lexer.WordType.ProductionDefininition)
            {
                GenerateError("产生式不以标识符开始");
            }

            var name = (string)WordList[CurIndex].AdditionInfo;

            if(ProductionNameMap.ContainsKey(name) || TerminalNameMap.ContainsKey(name))
            {
                GenerateError("产生式名称已存在",name);
            }

            if(name == "epsilon" || name.StartsWith(SysNamePrefix))
            {
                GenerateError("产生式名称不能为epsilon或以" + SysNamePrefix + "开头");
            }

            var startindex = CurIndex;

            CurIndex++;

            if(WordList[CurIndex].Type != Production_Lexer.WordType.Product)
            {
                GenerateError("期望符号","->");
            }

            CurIndex++;
            
            var itemlist = new List<NonTerminalProductionItemInfo>();
            var curcontent = new List<string>();
            var terminallist = new List<string>();
            var oldsysnameid = CurSysNameID;

            while(CurIndex < WordList.Length && WordList[CurIndex].Type != Production_Lexer.WordType.Semicolon)
            {
                switch(WordList[CurIndex].Type)
                {
                    case Production_Lexer.WordType.ProductionReference:
                        var curname = (string)WordList[CurIndex].AdditionInfo;

                        if(name.StartsWith(SysNamePrefix))
                        {
                            GenerateError("产生式引用名称不能以" + SysNamePrefix + "开头");
                        }

                        if(!ProductionNameMap.ContainsKey(curname) && !TerminalProductionNameMap.ContainsKey(curname) && curname != "epsilon" && !WaitCheckReference.Contains(curname))
                        {
                            WaitCheckReference.Add(curname);
                            WaitCheckReferenceList.Add(new KeyValuePair<string,int>(curname,CurIndex));
                        }

                        curcontent.Add(curname);
                        break;

                    case Production_Lexer.WordType.RegularExpression:
                        var restr = (string)WordList[CurIndex].AdditionInfo;

                        if(restr != "")
                        {
                            terminallist.Add(restr);
                            var newname = "";

                            if(TerminalNameMap.ContainsKey(restr))
                            {
                                newname = TerminalNameMap[restr];
                            }
                            else
                            {
                                newname = SysNamePrefix + CurSysNameID;
                                CurSysNameID++;
                                TerminalNameMap[restr] = newname;
                            }

                            curcontent.Add(newname);
                        }

                        break;

                    case Production_Lexer.WordType.Or:
                        itemlist.Add(new NonTerminalProductionItemInfo{Content = curcontent.ToArray()});
                        curcontent.Clear();
                        break;

                    default:
                        GenerateError("无效的字符序列",(string)WordList[CurIndex].AdditionInfo);
                        break;
                }

                CurIndex++;
            }

            CurIndex++;

            var isterminal = false;

            if(itemlist.Count == 0)
            {
                isterminal = true;
                        
                foreach(var item in curcontent)
                {
                    if(!item.StartsWith(SysNamePrefix))
                    {
                        isterminal = false;
                        break;
                    }
                }
            }

            if(isterminal)
            {
                CurSysNameID = oldsysnameid;
                var stb = new StringBuilder();
                    
                foreach(var item in terminallist)
                {
                    if(int.Parse(TerminalNameMap[item].Substring(SysNamePrefix.Length)) >= CurSysNameID)
                    {
                        TerminalNameMap.Remove(item);
                    }

                    stb.Append(item);
                }

                TerminalNameMap[stb.ToString()] = name;
                TerminalProductionNameMap[name] = startindex;

            }
            else
            {
                itemlist.Add(new NonTerminalProductionItemInfo{Content = curcontent.ToArray()});
                ntplist.Add(new NonTerminalProductionInfo{Name = name,Item = itemlist.ToArray()});
                ProductionNameMap[name] = startindex;
            }
        }

        public bool Analysis(Production_Lexer.WordInfo[] WordList)
        {
            List<NonTerminalProductionInfo> ntplist = new List<NonTerminalProductionInfo>();
            List<TerminalProductionInfo> tplist = new List<TerminalProductionInfo>();

            this.WordList = WordList;
            CurIndex = 0;
            CurSysNameID = 0;
            ProductionNameMap = new Dictionary<string,int>();
            TerminalProductionNameMap = new Dictionary<string,int>();
            TerminalNameMap = new Dictionary<string,string>();
            WaitCheckReference = new HashSet<string>();
            WaitCheckReferenceList = new List<KeyValuePair<string,int>>();

            try
            {
                while(CurIndex < WordList.Length)
                {
                    AnalysisLine(ntplist);
                }

                foreach(var item in WaitCheckReferenceList)
                {
                    if(!ProductionNameMap.ContainsKey(item.Key) && !TerminalProductionNameMap.ContainsKey(item.Key))
                    {
                        CurIndex = item.Value;
                        GenerateError("找不到该终结符或非终结符的定义",(string)WordList[item.Value].AdditionInfo);
                    }
                }

                foreach(var item in TerminalNameMap)
                {
                    tplist.Add(new TerminalProductionInfo{Name = item.Value,RegularExpression = item.Key});
                }
            }
            catch(ParserAnalysisException ex)
            {
                NonTerminalProductionResult = null;
                TerminalProductionResult = null;
                return false;
            }
            catch(Exception ex)
            {
                NonTerminalProductionResult = null;
                TerminalProductionResult = null;
                ErrorMsg = ex.Message + "\n" + ex.StackTrace;
                return false;
            }
            
            NonTerminalProductionResult = ntplist.ToArray();
            TerminalProductionResult = tplist.ToArray();
            ErrorMsg = "";
            return true;
        }
    }
}