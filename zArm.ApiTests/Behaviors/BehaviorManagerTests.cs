using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Sequences;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.Api;
using zArm.Api.Behaviors;

namespace zArm.ApiTests.Behaviors
{
    [TestClass()]
    public class BehaviorManagerTests
    {

        [TestMethod()]
        public void BehaviorManager_ReplaceBehavior()
        {
            using (Sequence.Create())
            {
                //setup
                var b1 = new Mock<IBehavior>();
                var b1_same = new Mock<IBehavior>();
                b1.Setup(i => i.Enable(It.IsAny<Arm>())).InSequence();
                b1.Setup(i => i.Disable()).InSequence();
                b1_same.Setup(i => i.Enable(It.IsAny<Arm>())).InSequence();
                b1_same.Setup(i => i.Disable()).InSequence();

                //test
                var behaviors = new BehaviorManager(null);
                behaviors.Add(b1.Object);
                behaviors.Add(b1_same.Object);
                behaviors.Remove(b1.Object);
                behaviors.Remove(b1_same.Object);
            }
        }

        [TestMethod()]
        public void BehaviorManager_MultipleBehavior()
        {
            using (Sequence.Create())
            {
                //setup
                var b1 = new Mock<IBehavior>();
                var b2 = new Mock<IBehavior2>();
                b1.Setup(i => i.Enable(It.IsAny<Arm>())).InSequence();
                b2.Setup(i => i.Enable(It.IsAny<Arm>())).InSequence();
                b1.Setup(i => i.Disable()).InSequence();
                b2.Setup(i => i.Disable()).InSequence();

                //test
                var behaviors = new BehaviorManager(null);
                behaviors.Add(b1.Object);
                behaviors.Add(b2.Object);
                behaviors.Remove(b1.Object);
                behaviors.Remove(b2.Object);
            }
        }

        [TestMethod()]
        public void BehaviorManager_MultipleWorflows()
        {
            using (Sequence.Create())
            {
                //setup
                var b1 = new Mock<IBehavior>();
                var w1 = new Mock<IWorkflowB1>();
                var w1_same = new Mock<IWorkflowB1>();
                var w2 = new Mock<IWorkflowB2>();
                var w3 = new Mock<IWorkflowB3>();
                WorkflowManager w1_end = null;
                WorkflowManager w1_same_end = null;
                WorkflowManager w3_end = null;
                b1.Setup(i => i.Enable(It.IsAny<Arm>())).InSequence();
                w1.Setup(i => i.Enable(It.IsAny<Arm>())).InSequence();
                w1.Setup(i => i.StartWorkflow(It.IsAny<WorkflowManager>())).Callback((WorkflowManager i) => w1_end = i);
                w1.Setup(i => i.Disable()).InSequence();
                w1.Setup(i => i.StopWorkflow()).InSequence();
                w1_same.Setup(i => i.Enable(It.IsAny<Arm>())).InSequence();
                w1_same.Setup(i => i.StartWorkflow(It.IsAny<WorkflowManager>())).Callback((WorkflowManager i) => w1_same_end = i);
                w2.Setup(i => i.Enable(It.IsAny<Arm>())).InSequence();
                w3.Setup(i => i.Enable(It.IsAny<Arm>())).InSequence();
                w2.Setup(i => i.Disable()).InSequence();
                w1_same.Setup(i => i.Disable()).InSequence();
                w1_same.Setup(i => i.StopWorkflow()).InSequence();
                w3.Setup(i => i.StartWorkflow(It.IsAny<WorkflowManager>())).Callback((WorkflowManager i) => w3_end = i);
                b1.Setup(i => i.Disable()).InSequence();
                w3.Setup(i => i.Disable()).InSequence();
                w3.Setup(i => i.StopWorkflow()).InSequence();
                //never called
                w2.Setup(i => i.StartWorkflow(It.IsAny<WorkflowManager>())).InSequence(Times.Never());
                w2.Setup(i => i.StopWorkflow()).InSequence(Times.Never());

                //test
                var behaviors = new BehaviorManager(null);
                behaviors.Add(b1.Object);
                behaviors.Add(w1.Object);
                behaviors.Add(w1_same.Object);
                behaviors.Add(w2.Object);
                behaviors.Add(w3.Object);
                behaviors.Remove(w2.Object);
                w1_same_end.EndWorkflow();
                behaviors.Remove(b1.Object);
                behaviors.Remove(w3.Object);
            }
        }

        public interface IBehavior2 : IBehavior { }

        public interface IWorkflowB1 : IBehavior, IWorkflow { }

        public interface IWorkflowB2 : IBehavior, IWorkflow { }

        public interface IWorkflowB3 : IBehavior, IWorkflow { }

    }
}
