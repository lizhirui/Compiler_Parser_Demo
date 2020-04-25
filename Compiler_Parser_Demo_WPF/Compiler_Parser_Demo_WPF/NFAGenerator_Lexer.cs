﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler_Parser_Demo_WPF
{
    class NFAGenerator_Lexer : ITask
    {
        private bool Changed = false;
        private Production_Parser.ResultInfo ParserResult;

        private class NFALexerAnalysisException : Exception
        {
            
        }

        public enum WordType
        {
            LeftBrace,
            RightBrace,
            Or,
            Star,
            String
        }

        public struct WordInfo
        {
            public WordType Type;
            public object AdditionInfo;
            public int StartPos;
            public int Length;
        }

        public string ErrorMsg
        {
            get;
            private set;
        }

        public struct ResultInfo
        {
            public Production_Parser.ResultInfo ParserResult;
            public WordInfo[][] WordList;
        }

        public ResultInfo Result
        {
            get;
            private set;
        }

        public NFAGenerator_Lexer()
        {
            ErrorMsg = "";
        }
        private void GenerateError(string ErrorTitle,string ErrorString)
        {
            ErrorMsg = "[NFALexer]:" + ErrorTitle + ":\"" + ErrorString + "\"\n";
            throw new NFALexerAnalysisException();
        }

        private void AnalysisItem(List<WordInfo[]> rlist,string curexpression)
        {
            var len = curexpression.Length;
            var curstr = new StringBuilder();
            var curlist = new List<WordInfo>();
            var i = 0;
                
            while(i < len)
            {
                var defaultdealed = false;

                switch(curexpression[i])
                {
                    case '(':
                        curlist.Add(new WordInfo{Type = WordType.LeftBrace,StartPos = i,Length = 1,AdditionInfo = null});
                        i++;
                        break;

                    case ')':
                        curlist.Add(new WordInfo{Type = WordType.RightBrace,StartPos = i,Length = 1,AdditionInfo = null});
                        i++;
                        break;

                    case '|':
                        curlist.Add(new WordInfo{Type = WordType.Or,StartPos = i,Length = 1,AdditionInfo = null});
                        i++;
                        break;

                    case '*':
                        curlist.Add(new WordInfo{Type = WordType.Star,StartPos = i,Length = 1,AdditionInfo = null});
                        i++;
                        break;

                    case '\\':
                        if(i == len - 1)
                        {
                            GenerateError("转义提示符后无字符",curexpression);
                        }
                        else
                        {
                            curstr.Append(curexpression[i + 1]);
                            i += 2;
                        }

                        break;

                    default:
                        defaultdealed = true;
                        curstr.Append(curexpression[i]);
                        i++;
                        break;
                }

                if(!defaultdealed)
                {
                    var tstr = curstr.ToString();
                    curlist.Add(new WordInfo{Type = WordType.String,StartPos = i - tstr.Length,Length = tstr.Length,AdditionInfo = tstr});
                }
            }

            rlist.Add(curlist.ToArray());
        }


        public bool Analysis(Production_Parser.ResultInfo ParserResult)
        {
            this.ParserResult = ParserResult;
            var rlist = new List<WordInfo[]>();
            
            try
            {
                foreach(var curexpressionitem in ParserResult.tplist)
                {
                    AnalysisItem(rlist,curexpressionitem.RegularExpression); 
                }
            }
            catch(NFALexerAnalysisException ex)
            {
                Result = new ResultInfo{ParserResult = new Production_Parser.ResultInfo{ntplist = null,tplist = null},WordList = null};
                return false;
            }
            catch(Exception ex)
            {
                Result = new ResultInfo{ParserResult = new Production_Parser.ResultInfo{ntplist = null,tplist = null},WordList = null};
                ErrorMsg = ex.Message + "\n" + ex.StackTrace;
                return false;
            }

            Result = new ResultInfo{ParserResult = ParserResult,WordList = rlist.ToArray()};
            ErrorMsg = "";
            return true;
        }

        public bool MoveFrom(object obj)
        {
            Changed = true;
            return true;
        }

        public object MoveTo()
        {
            Changed = false;
            return null;
        }

        public bool ResultChanged()
        {
            return Changed;
        }
    }
}
