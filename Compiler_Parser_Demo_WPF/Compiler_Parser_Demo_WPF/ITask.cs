using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler_Parser_Demo_WPF
{
    interface ITask
    {
        /// <summary>
        /// Receive input from the previous task and run the current task
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>true if success</returns>
        bool MoveFrom(object obj);

        /// <summary>
        /// Send the result to the next task
        /// </summary>
        /// <returns></returns>
        object MoveTo();

        /// <summary>
        /// Judge whether the result of current task is changed
        /// </summary>
        /// <returns></returns>
        bool ResultChanged();

        /// <summary>
        /// Set changed flag is true
        /// </summary>
        void SetChanged();
    }
}
