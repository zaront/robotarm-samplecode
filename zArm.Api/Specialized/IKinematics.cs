using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api.Specialized
{
    public interface IKinematics
    {
        KinematicResult<Pose> GetPose(KinematicTarget target, Pose initialPose = null);
        KinematicResult<KinematicTarget> GetTarget(Pose pose);
        KinematicTarget TargetOffset { get; set; }
		KinematicTarget BaseLocation { get; }
    }

    public class KinematicResult<T>
    {
        public T Result { get; set; }
        public bool BackwardReach { get; set; }
        public bool OnTarget { get; set; }
        public float OnTargetDistance { get; set; }
    }

    public class KinematicTarget
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}
