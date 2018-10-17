using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api.Behaviors
{
    public interface IWorkflow
    {
        void StartWorkflow(WorkflowManager workflowManager);
        void StopWorkflow();
    }


    public class WorkflowManager
    {
        Action _endWorkflow;

        internal WorkflowManager(Action endWorkflow)
        {
            //set fields
            _endWorkflow = endWorkflow;
        }

        public void EndWorkflow()
        {
            _endWorkflow?.Invoke();
        }
    }
}
