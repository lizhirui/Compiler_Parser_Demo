using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Media.Imaging;

namespace Compiler_Parser_Demo_WPF
{
    class NFAGenerator_Parser : ITask
    {
        private bool Changed = false;

        private class NFAParserAnalysisException : Exception
        {
            
        }

        public class NFANode
        {
            public NFAEdge[] Edge;
        }

        public struct NFAEdge
        {
            public char Condition;
            public bool Epsilon;
            public NFANode NextNode;
        }

        public struct ResultInfo
        {
            public ResultItem[] Item;
            public Production_Parser.ResultInfo Production_ParserResult;
        }

        public struct ResultItem
        {
            public NFANode StartNode;
            public NFANode EndNode; 
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

        public string ErrorMsg
        {
            get;
            private set;
        }

        private int CurExpressionIndex = 0;
        private int CurIndex = 0;
        private NFAGenerator_Lexer.ResultInfo NFAWordInfo;

        public NFAGenerator_Parser()
        {
            ErrorMsg = "";
        }

        private void GenerateError(string ErrorTitle)
        {
            GenerateError(ErrorTitle,"");
        }

        private void GenerateError(string ErrorTitle,string ErrorString)
        {
            ErrorMsg = "[NFAGenerator_Parser]:" + ErrorTitle + "（字符" + (CurIndex < NFAWordInfo.WordList[CurExpressionIndex].Length ? NFAWordInfo.WordList[CurExpressionIndex][CurIndex].StartPos : -1) + "）";

            if(ErrorString != "")
            {
                ErrorMsg += ":\"" + ErrorString + "\"";
            }

            ErrorMsg += "在终结符" + NFAWordInfo.ParserResult.tplist[CurExpressionIndex].Name + "中";

            ErrorMsg += '\n';
            throw new NFAParserAnalysisException();
        }

        /// <summary>
        /// ExpressionChar -> "(" <Expression> ")" | <Char>
        /// </summary>
        /// <param name="WordInfo"></param>
        /// <returns></returns>
        private ResultItem Analysis_ExpressionChar(NFAGenerator_Lexer.WordInfo[] WordInfo)
        {
            if(CurIndex >= WordInfo.Length)
            {
                GenerateError("期望字符或表达式但遇到了文本结束标记");
            }

            switch(WordInfo[CurIndex].Type)
            {
                case NFAGenerator_Lexer.WordType.LeftBrace:
                    var beginindex = CurIndex;
                    CurIndex++;
                    var r = Analysis_Expression(WordInfo);

                    if(CurIndex >= WordInfo.Length || WordInfo[CurIndex].Type != NFAGenerator_Lexer.WordType.RightBrace)
                    {
                        CurIndex = beginindex;
                        GenerateError("该左括号无对应右括号","(");
                    }

                    CurIndex++;
                    return r;

                case NFAGenerator_Lexer.WordType.Char:
                    var node = new NFANode{Edge = new NFAEdge[1]{new NFAEdge{Condition = (char)WordInfo[CurIndex].AdditionInfo,Epsilon = false,NextNode = new NFANode{Edge = new NFAEdge[0]}}}};
                    CurIndex++;
                    return new ResultItem{StartNode = node,EndNode = node.Edge[0].NextNode};

                default:
                    GenerateError("无效的词，期望\"(\"或字符",((char)WordInfo[CurIndex].AdditionInfo) + "");
                    break;
            }
            
            //Here is unreachable
            return new ResultItem();
        }

        /// <summary>
        /// ExpressionStar -> <ExpressionChar> <Star> | <ExpressionChar>
        /// </summary>
        /// <param name="WordInfo"></param>
        /// <returns></returns>
        private ResultItem Analysis_ExpressionStar(NFAGenerator_Lexer.WordInfo[] WordInfo)
        {
            var r = Analysis_ExpressionChar(WordInfo);
            
            if(CurIndex < WordInfo.Length && WordInfo[CurIndex].Type == NFAGenerator_Lexer.WordType.Star)
            {
                var endnode = new NFANode{Edge = new NFAEdge[0]};
                var startnode = new NFANode{Edge = new NFAEdge[2]{new NFAEdge{Epsilon = true,NextNode = r.StartNode},new NFAEdge{Epsilon = true,NextNode = endnode}}};
                r.EndNode.Edge = new NFAEdge[1]{new NFAEdge{Epsilon = true,NextNode = startnode}};
                CurIndex++;
                return new ResultItem{StartNode = startnode,EndNode = endnode};
            }

            return r;
        }

        /// <summary>
        /// ExpressionPart -> <ExpressionStar> <ExpressionPart> | <ExpressionStar>
        /// </summary>
        /// <param name="WordInfo"></param>
        /// <returns></returns>
        private ResultItem Analysis_ExpressionPart(NFAGenerator_Lexer.WordInfo[] WordInfo)
        {
            var startnode = new NFANode{Edge = new NFAEdge[0]};
            var curnode = startnode;
            var empty = true;

            while(true)
            {
                var beginindex = CurIndex;

                try
                {
                    var r = Analysis_ExpressionStar(WordInfo);    
                    curnode.Edge = new NFAEdge[1]{new NFAEdge{Epsilon = true,NextNode = r.StartNode}};
                    curnode = r.EndNode;
                    empty = false;
                }
                catch(NFAParserAnalysisException ex)
                {
                    CurIndex = beginindex;
                    break;
                }

                if(CurIndex < WordInfo.Length && WordInfo[CurIndex].Type == NFAGenerator_Lexer.WordType.RightBrace)
                {
                    break;
                }
            }

            if(empty)
            {
                if(CurIndex >= WordInfo.Length)
                {
                    GenerateError("期望表达式但遇到了文本结束标记");
                }
                else
                {
                    GenerateError("期望表达式",((char)WordInfo[CurIndex].AdditionInfo) + "");
                }
            }

            return new ResultItem{StartNode = startnode,EndNode = curnode};
        }

        /// <summary>
        /// ExpressionOr -> <ExpressionPart> <Or> <ExpressionOr> | <ExpressionPart>;
        /// </summary>
        /// <param name="WordInfo"></param>
        /// <returns></returns>
        private ResultItem Analysis_ExpressionOr(NFAGenerator_Lexer.WordInfo[] WordInfo)
        {
            var rlist = new List<ResultItem>();

            while(true)
            {
                var r = Analysis_ExpressionPart(WordInfo);
                rlist.Add(r);

                if(CurIndex >= WordInfo.Length || WordInfo[CurIndex].Type != NFAGenerator_Lexer.WordType.Or)
                {
                    break;
                }

                if(CurIndex < WordInfo.Length && WordInfo[CurIndex].Type == NFAGenerator_Lexer.WordType.RightBrace)
                {
                    break;
                }

                CurIndex++;
            }

            if(rlist.Count == 0)
            {
                GenerateError("表达式选择列表为空");
            }

            var beginnode = new NFANode();
            var endnode = new NFANode{Edge = new NFAEdge[0]};
            var beginlist = new List<NFAEdge>();

            foreach(var item in rlist)
            {
                beginlist.Add(new NFAEdge{Epsilon = true,NextNode = item.StartNode});
                item.EndNode.Edge = new NFAEdge[1]{new NFAEdge{Epsilon = true,NextNode = endnode}};
            }

            beginnode.Edge = beginlist.ToArray();
            return new ResultItem{StartNode = beginnode,EndNode = endnode};
        }

        /// <summary>
        /// Expression -> <ExpressionOr> <Expression> | <ExpressionOr> | <epsilon>;
        /// </summary>
        /// <param name="WordInfo"></param>
        /// <returns></returns>
        private ResultItem Analysis_Expression(NFAGenerator_Lexer.WordInfo[] WordInfo)
        {
            var startnode = new NFANode{Edge = new NFAEdge[0]};
            var curnode = startnode;

            while(true)
            {
                if(CurIndex >= WordInfo.Length)
                {
                    break;
                }

                var r = Analysis_ExpressionOr(WordInfo);
                curnode.Edge = new NFAEdge[1]{new NFAEdge{Epsilon = true,NextNode = r.StartNode}};
                curnode = r.EndNode;

                if(CurIndex < WordInfo.Length && WordInfo[CurIndex].Type == NFAGenerator_Lexer.WordType.RightBrace)
                {
                    break;
                }
            }

            return new ResultItem{StartNode = startnode,EndNode = curnode};
        }

        /// <summary>
        /// Analysis Regular Expression
        /// Production:
        /// Expression -> <ExpressionOr> <Expression> | <ExpressionOr> | <epsilon>;
        /// ExpressionOr -> <ExpressionPart> <Or> <ExpressionOr> | <ExpressionPart>;
        /// ExpressionPart -> <ExpressionStar> <ExpressionPart> | <ExpressionStar>
        /// ExpressionStar -> <ExpressionChar> <Star> | <ExpressionChar>
        /// ExpressionChar -> "(" <Expression> ")" | <Char>
        /// </summary>
        /// <param name="NFAWordInfo"></param>
        /// <returns></returns>
        public bool Analysis(NFAGenerator_Lexer.ResultInfo NFAWordInfo)
        {
            this.NFAWordInfo = NFAWordInfo;
            CurExpressionIndex = 0;
            var rlist = new List<ResultItem>();

            try
            {
                foreach(var wordlist in NFAWordInfo.WordList)
                {
                    CurIndex = 0;
                    var r = Analysis_Expression(wordlist);

                    if(CurIndex < wordlist.Length)
                    {
                        GenerateError("期望文本结尾标记");
                    }

                    //Connect child graph to main graph by connection start and end node
                    rlist.Add(new ResultItem{StartNode = r.StartNode,EndNode = r.EndNode});
                    CurExpressionIndex++;
                }
            }
            catch(NFAParserAnalysisException ex)
            {
                Result = new ResultInfo();
                ResultImage = null;
                return false;
            }
            catch(Exception ex)
            {
                Result = new ResultInfo();
                ResultImage = null;
                ErrorMsg = "[NFAGenerator_Parser]" + ex.Message + "\n" + ex.StackTrace;
                return false;
            }
            
            ErrorMsg = "";
            Result = new ResultInfo{Item = rlist.ToArray(),Production_ParserResult = NFAWordInfo.ParserResult};
            var rimage = new List<BitmapImage>();

            foreach(var item in rlist)
            {
                rimage.Add(NFAGenerator_DiagramGenerator.ToImage(item));
            }

            ResultImage = rimage.ToArray();
            return true;
        }

        public bool MoveFrom(object obj)
        {
            Changed = true;
            return Analysis((NFAGenerator_Lexer.ResultInfo)obj);
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
    }
}
