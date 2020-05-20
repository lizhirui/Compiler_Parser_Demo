using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Compiler_Parser_Demo_WPF
{
    class GrammarCompilerCollection : List<IGrammarCompiler>
    {
        public GrammarCompilerCollection() : base()
        {
            
        }

        public GrammarCompilerCollection(int capacity) : base(capacity)
        {
            
        }

        public GrammarCompilerCollection(IEnumerable<IGrammarCompiler> collection) : base(collection)
        {
            
        }
    }
}
