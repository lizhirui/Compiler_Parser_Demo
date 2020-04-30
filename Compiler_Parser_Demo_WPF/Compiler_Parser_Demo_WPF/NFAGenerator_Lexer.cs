using System;
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
            Char
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
            var curlist = new List<WordInfo>();
            var i = 0;
                
            while(i < len)
            {
                switch(curexpression[i])
                {
                    case '(':
                        curlist.Add(new WordInfo{Type = WordType.LeftBrace,StartPos = i,Length = 1,AdditionInfo = '('});
                        i++;
                        break;

                    case ')':
                        curlist.Add(new WordInfo{Type = WordType.RightBrace,StartPos = i,Length = 1,AdditionInfo = ')'});
                        i++;
                        break;

                    case '|':
                        curlist.Add(new WordInfo{Type = WordType.Or,StartPos = i,Length = 1,AdditionInfo = '|'});
                        i++;
                        break;

                    case '*':
                        curlist.Add(new WordInfo{Type = WordType.Star,StartPos = i,Length = 1,AdditionInfo = '*'});
                        i++;
                        break;

                    case '\\':
                        if(i == len - 1)
                        {
                            GenerateError("转义提示符后无字符",curexpression);
                        }
                        else
                        {
                            switch(curexpression[i + 1])
                            {
                                case 'B':
                                    //大写字母
                                    curlist.Add(new WordInfo{Type = WordType.LeftBrace,StartPos = i,Length = 2,AdditionInfo = '('});

                                    for(var ch = 'A';ch < 'Z';ch++)
                                    {
                                        curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = ch});
                                        curlist.Add(new WordInfo{Type = WordType.Or,StartPos = i,Length = 2,AdditionInfo = '|'});
                                    }

                                    curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = 'Z'});
                                    curlist.Add(new WordInfo{Type = WordType.RightBrace,StartPos = i,Length = 2,AdditionInfo = ')'});
                                    break;

                                case 'b':
                                    //小写字母
                                    curlist.Add(new WordInfo{Type = WordType.LeftBrace,StartPos = i,Length = 2,AdditionInfo = '('});

                                    for(var ch = 'a';ch < 'z';ch++)
                                    {
                                        curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = ch});
                                        curlist.Add(new WordInfo{Type = WordType.Or,StartPos = i,Length = 2,AdditionInfo = '|'});
                                    }

                                    curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = 'z'});
                                    curlist.Add(new WordInfo{Type = WordType.RightBrace,StartPos = i,Length = 2,AdditionInfo = ')'});
                                    break;

                                case 'a':
                                    //字母
                                    curlist.Add(new WordInfo{Type = WordType.LeftBrace,StartPos = i,Length = 2,AdditionInfo = '('});

                                    for(var ch = 'A';ch <= 'Z';ch++)
                                    {
                                        curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = ch});
                                        curlist.Add(new WordInfo{Type = WordType.Or,StartPos = i,Length = 2,AdditionInfo = '|'});
                                    }
                                    
                                    for(var ch = 'a';ch < 'z';ch++)
                                    {
                                        curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = ch});
                                        curlist.Add(new WordInfo{Type = WordType.Or,StartPos = i,Length = 2,AdditionInfo = '|'});
                                    }

                                    curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = 'z'});
                                    curlist.Add(new WordInfo{Type = WordType.RightBrace,StartPos = i,Length = 2,AdditionInfo = ')'});
                                    break;

                                case '1':
                                    //数字
                                    curlist.Add(new WordInfo{Type = WordType.LeftBrace,StartPos = i,Length = 2,AdditionInfo = '('});

                                    for(var ch = '0';ch < '9';ch++)
                                    {
                                        curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = ch});
                                        curlist.Add(new WordInfo{Type = WordType.Or,StartPos = i,Length = 2,AdditionInfo = '|'});
                                    }

                                    curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = '9'});
                                    curlist.Add(new WordInfo{Type = WordType.RightBrace,StartPos = i,Length = 2,AdditionInfo = ')'});
                                    break;

                                case 'i':
                                    //字母/数字
                                    curlist.Add(new WordInfo{Type = WordType.LeftBrace,StartPos = i,Length = 2,AdditionInfo = '('});

                                    for(var ch = 'A';ch <= 'Z';ch++)
                                    {
                                        curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = ch});
                                        curlist.Add(new WordInfo{Type = WordType.Or,StartPos = i,Length = 2,AdditionInfo = '|'});
                                    }
                                    
                                    for(var ch = 'a';ch <= 'z';ch++)
                                    {
                                        curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = ch});
                                        curlist.Add(new WordInfo{Type = WordType.Or,StartPos = i,Length = 2,AdditionInfo = '|'});
                                    }

                                    for(var ch = '0';ch < '9';ch++)
                                    {
                                        curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = ch});
                                        curlist.Add(new WordInfo{Type = WordType.Or,StartPos = i,Length = 2,AdditionInfo = '|'});
                                    }

                                    curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = '9'});
                                    curlist.Add(new WordInfo{Type = WordType.RightBrace,StartPos = i,Length = 2,AdditionInfo = ')'});
                                    break;

                                case 'n':
                                    //换行符
                                    curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = '\n'});
                                    break;

                                case 'r':
                                    //回车符
                                    curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = '\r'});
                                    break;

                                case 't':
                                    //制表符
                                    curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = '\t'});
                                    break;

                                default:
                                    curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 2,AdditionInfo = curexpression[i + 1]});
                                    break;
                            }

                            i += 2;
                        }

                        break;

                    default:
                        curlist.Add(new WordInfo{Type = WordType.Char,StartPos = i,Length = 1,AdditionInfo = curexpression[i]});
                        i++;
                        break;
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
                ErrorMsg = "[NFAGenerator_Lexer]:" + ex.Message + "\n" + ex.StackTrace;
                return false;
            }

            Result = new ResultInfo{ParserResult = ParserResult,WordList = rlist.ToArray()};
            ErrorMsg = "";
            return true;
        }

        public bool MoveFrom(object obj)
        {
            Changed = true;
            return Analysis((Production_Parser.ResultInfo)obj);
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
