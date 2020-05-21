using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler_Parser_Demo_WPF
{
    interface IGrammarCompiler
    {
        /// <summary>
        /// 获取主类型
        /// </summary>
        /// <returns></returns>
        string GetMajorType();

        /// <summary>
        /// 获取从类型
        /// </summary>
        /// <returns></returns>
        string GetMinorType();

        /// <summary>
        /// 文法编译
        /// </summary>
        /// <param name="GrammarInfo"></param>
        bool Compile(DFAPriorityGenerator.ResultInfo ProductionInfo,string StartSymbol,out string ErrorText);

        /// <summary>
        /// 文法测试
        /// </summary>
        /// <param name="WordList"></param>
        /// <param name="ErrorText"></param>
        /// <returns></returns>
        bool Test(List<LexerWordInfo> WordList,out string ErrorText);

        /// <summary>
        /// 获取编译结果
        /// </summary>
        /// <returns></returns>
        string GetCompileResult();

        /// <summary>
        /// 获取测试结果
        /// </summary>
        /// <returns></returns>
        string GetTestResult();
    }
}
