using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Compiler_Parser_Demo_WPF
{
    class Production_Lexer : ITask
    {
        public enum WordType
        {
            Product,
            Or,
            Semicolon,//used to indicate the end of production
            RegularExpression,
            ProductionDefininition,
            ProductionReference
        }

        public struct WordInfo
        {
            public WordType Type;
            public object AdditionInfo;
            public int StartPos;
            public int Line;
            public int Column;
            public int Length;
        }

        private class LexerAnalysisException : Exception
        {
            
        }

        public string Text
        {
            get;
            private set;
        }

        public WordInfo[] Result
        {
            get;
            private set;
        }

        public string ErrorMsg
        {
            get;
            private set;
        }

        private int CurLine;
        private int CurColumn;
        private int CurPos;

        private bool Changed = false;

        public Production_Lexer()
        {
            Result = null;
            ErrorMsg = "";
            Text = "";
        }

        private void GenerateError(string ErrorTitle,string ErrorString)
        {
            ErrorMsg = "[Lexer]:" + ErrorTitle + "（行" + CurLine + "字符" + CurColumn + "）:\"" + ErrorString + "\"\n";
            throw new LexerAnalysisException();
        }

        private WordInfo GetNewWord(WordType type)
        {
            return GetNewWord(type,1);
        }

        private WordInfo GetNewWord(WordType type,int Length)
        {
            return GetNewWord(type,Length,null);
        }

        private WordInfo GetNewWord(WordType type,int Length,string AdditionInfo)
        {
            var r = new WordInfo{Type = type,AdditionInfo = AdditionInfo,Line = CurLine,Column = CurColumn,StartPos = CurPos,Length = Length};
            NextChar(Length);
            return r;
        }

        private void NextChar(int Increase)
        {
            while(Increase > 0)
            {
                if(Text[CurPos] == '\n')
                {
                    CurLine++;
                    CurColumn = 1;
                }
                else
                {
                    CurColumn++;
                }

                CurPos++;
                Increase--;
            }
        }

        private void Analysis_Product(List<WordInfo> rlist,int len)
        {
            if(CurPos == (len - 1))
            {
                GenerateError("无效的字符序列","" + Text[CurPos]);
            }
            else if(Text[CurPos + 1] != '>')
            {
                GenerateError("无效的字符序列","" + Text[CurPos] + Text[CurPos + 1]);
            }
            else
            {
                rlist.Add(GetNewWord(WordType.Product,2));
            }
        }

        private void Analysis_ProductionReference(List<WordInfo> rlist,int len)
        {
            var flag = false;
            var tstr = "";

            for(var i = CurPos + 1;i < len;i++)
            {
                if(Text[i] == '>')
                {
                    tstr = Text.Substring(CurPos + 1,i - CurPos - 1);
                    var regex = new Regex("^[A-Za-z_][A-Za-z0-9_]*$");
                                
                    if(regex.IsMatch(tstr))
                    {
                        rlist.Add(GetNewWord(WordType.ProductionReference,tstr.Length + 2,tstr));
                        flag = true;
                        break;
                    }
                    else
                    {
                        GenerateError("违反产生式标识符规则，首字符必须为字母或下划线且其余字符必须为字母、数字或下划线","<" + tstr + ">");
                    }
                }
            }

            if(!flag)
            {
                GenerateError("未找到产生式引用终止符","<" + tstr);
            }
        }

        private void Analysis_RegularExpression(List<WordInfo> rlist,int len)
        {
            var flag = false;
            var tstr = "";

            for(int i = CurPos + 1;i < len;i++)
            {
                if(Text[i] == '\\')
                {
                    i++;
                    continue;
                }

                if(Text[i] == '\"')
                {
                    tstr = Text.Substring(CurPos + 1,i - CurPos - 1);
                    rlist.Add(GetNewWord(WordType.RegularExpression,tstr.Length + 2,tstr));
                    flag = true;
                    break;
                }
            }

            if(!flag)
            {
                GenerateError("未找到正则式终止符","\"" + tstr);
            }
        }

        private void Analysis_Comment(List<WordInfo> rlist,int len)
        {
            if(CurPos == len)
            {
                GenerateError("无效的字符序列",Text[CurPos] + "");
            }

            NextChar(1);

            if(Text[CurPos] == '/')
            {
                //Line Comment
                for(int i = CurPos + 1;i < len;i++)
                {
                    NextChar(1);

                    if(Text[CurPos - 1] == '\n')
                    {
                        break; 
                    }
                }
            }
            else if(Text[CurPos] == '*')
            {
                //Block Comment
                for(int i = CurPos + 1;i < len;i++)
                {
                    NextChar(1);

                    if(Text[CurPos - 1] == '\\' && Text[CurPos - 2] == '*')
                    {
                        break; 
                    }
                }
            }
            else
            {
                GenerateError("无效的字符序列","" + Text[CurPos - 1] + Text[CurPos]);
            }
        }

        private void Analysis_ProductionDifinition(List<WordInfo> rlist,int len)      
        {
            var i = 0;

            for(i = CurPos;i < len;i++)
            {
                if(!char.IsLetterOrDigit(Text[i]) && Text[i] != '_')
                {
                    break;
                }
            }

            var tstr = Text.Substring(CurPos,i - CurPos);
            rlist.Add(GetNewWord(WordType.ProductionDefininition,tstr.Length,tstr));
        }

        public bool Analysis(string Text)
        {
            this.Text = Text;
            var len = Text.Length;
            var rlist = new List<WordInfo>();
            CurLine = 1;
            CurColumn = 1;
            CurPos = 0;

            try
            {
                for(CurPos = 0;CurPos < len;)
                {
                    switch(Text[CurPos])
                    {
                        case '-':
                            Analysis_Product(rlist,len);
                            break;

                        case '|':
                            rlist.Add(GetNewWord(WordType.Or));
                            break;

                        case ';':
                            rlist.Add(GetNewWord(WordType.Semicolon));
                            break;

                        case '<':
                            Analysis_ProductionReference(rlist,len);
                            break;

                        case '\"':
                            Analysis_RegularExpression(rlist,len);
                            break;

                        case '/':
                            Analysis_Comment(rlist,len);
                            break;

                        case ' ':
                        case '\t':
                        case '\r':
                        case '\n':
                            NextChar(1);
                            break;

                        default:
                            if(char.IsLetter(Text[CurPos]) || Text[CurPos] == '_')
                            {
                                Analysis_ProductionDifinition(rlist,len);
                            }
                            else
                            {
                                GenerateError("无效的字符序列",Text[CurPos] + "");
                            }

                            break;
                    }
                }
            }
            catch(LexerAnalysisException ex)
            {
                Result = null;
                return false;
            }
            catch(Exception ex)
            {
                Result = null;
                ErrorMsg = ex.Message + "\n" + ex.StackTrace;
                return false;
            }

            Result = rlist.ToArray();
            ErrorMsg = "";
            return true;
        }

        public bool MoveFrom(object obj)
        {
            Changed = true;
            return Analysis(obj as string);
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