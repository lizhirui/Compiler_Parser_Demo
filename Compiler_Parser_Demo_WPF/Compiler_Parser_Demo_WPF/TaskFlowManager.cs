using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler_Parser_Demo_WPF
{
    class TaskFlowManager
    {
        private List<Type> TaskTypeList = new List<Type>();
        private List<ITask> TaskInstance = new List<ITask>();
        private Dictionary<Type,int> TaskTypeMap = new Dictionary<Type,int>();
        public delegate void TaskResultUpdatedHandler(TaskFlowManager Sender,Type TaskType);
        public delegate void TaskResultClearedHandler(TaskFlowManager Sender,Type TaskType);
        public event TaskResultUpdatedHandler TaskResultUpdated;
        public event TaskResultClearedHandler TaskResultCleared;


        public void AddTask<T>() where T : class,ITask
        {
            var TaskType = typeof(T);

            if(TaskTypeMap.ContainsKey(typeof(T)))
            {
                throw new ArgumentException("Task already exists in TaskTypeList");
            }

            TaskTypeMap[TaskType] = TaskTypeList.Count;
            TaskTypeList.Add(TaskType);
            TaskInstance.Add(Activator.CreateInstance(TaskType,true) as ITask);
        }

        public void ClearTask()
        {
            TaskTypeList.Clear();
            TaskInstance.Clear();
            TaskTypeMap.Clear();
        }

        public object GetTask(Type TaskType)
        {
            if(!TaskTypeMap.ContainsKey(TaskType))
            {
                throw new ArgumentException("TaskTypeList doesn't contain this task");
            }

            return TaskInstance[TaskTypeMap[TaskType]];
        }

        public T GetTask<T>() where T : class,ITask
        {
            return GetTask(typeof(T)) as T;
        }

        public bool RunTask<T>()
        {
            var TaskType = typeof(T);

            if(!TaskTypeMap.ContainsKey(TaskType))
            {
                throw new ArgumentException("TaskTypeList doesn't contain this task");
            }

            var endtaskid = TaskTypeMap[TaskType];
            var starttaskid = endtaskid + 1;
            var i = 0;

            for(i = 0;i <= endtaskid;i++)
            {
                if(TaskInstance[i].ResultChanged())
                {
                    starttaskid = i + 1;
                    break;
                }
            }
            
            var succ = true;

            for(i = starttaskid;i <= endtaskid;i++)
            {
                object lastresult = null;

                if(i > 0)
                {
                    lastresult = TaskInstance[i - 1].MoveTo();
                }

                if(!TaskInstance[i].MoveFrom(lastresult))
                {
                    if(i > 0)
                    {
                        TaskInstance[i - 1].SetChanged();
                    }

                    succ = false;
                    break;
                }

                TaskResultUpdated(this,TaskTypeList[i]);
            }

            if(!succ)
            {
                for(;i <= endtaskid;i++)
                {
                    TaskResultCleared(this,TaskTypeList[i]);
                }
            }

            return succ;
        }
    }
}
