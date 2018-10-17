using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace zArm.Api.Motion
{
	[Obsolete]
	public class AnimationBlender
    {
        List<TransitionedAnimation> _animations = new List<TransitionedAnimation>();
        MotionPose _startingPose;
        PoseSpeed _defaultAcceleration;
        PoseSpeed _defaultTopSpeed;
        ReaderWriterLockSlim _sync = new ReaderWriterLockSlim();

        public AnimationBlender(MotionPose startingPose, PoseSpeed defaultAcceleration, PoseSpeed defaultTopSpeed)
        {
            //set fields
            _startingPose = startingPose;
            _defaultAcceleration = defaultAcceleration;
            _defaultTopSpeed = defaultTopSpeed;
        }

        public void Add(Animation animation, double startTime = 0, double? animationOffset = null)
        {
            _sync.EnterWriteLock();
            try
            {

            }
            finally
            {
                _sync.ExitWriteLock();
            }
        }

        public void GetPosition()
        {
            _sync.EnterReadLock();
            try
            {

            }
            finally
            {
                _sync.EnterReadLock();
            }
        }

        class TransitionedAnimation
        {
            public Animation Animation;
            public Animation Transition;
            double AnimationOffset;
            double StartTime;
        }

    }

}
