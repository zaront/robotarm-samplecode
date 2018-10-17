using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api.Motion
{
    public class SpeedControl
    {
        double _start;
        double _end;
        double _acceleration; //degrees increase per millisecond
        double _topSpeed; //degrees per millisecond
        double _peakVelocity;
        double _accelerationTime;
        double _dwellTime;
        double _totalTime;

        public SpeedControl(double start, double end, double acceleration = 150, double topSpeed = 150)
        {
            //set fields
            _start = start;
            _end = end;
            _acceleration = acceleration;
            _topSpeed = topSpeed;

            //set acceleration and dwell time
            _accelerationTime = Kinamatic.GetTimeUsingVelocity(0, _topSpeed, _acceleration);
            var accDist = Kinamatic.GetDistanceUsingAcceleration(0, _acceleration, _accelerationTime);
            var halfDist = (double)Math.Abs(_end - _start) / 2d;
            if (accDist <= halfDist)
            {
                var dwellDist = (halfDist * 2) - (accDist * 2);
                _dwellTime = Kinamatic.GetTimeUsingDistance(_topSpeed, 0, dwellDist);
                _peakVelocity = _topSpeed;
            }
            else
            {
                _accelerationTime = Kinamatic.GetTimeUsingDistance(0, _acceleration, halfDist);
                _dwellTime = 0;
                _peakVelocity = Kinamatic.GetVelocityUsingDistance(0, _acceleration, halfDist);
            }
            _totalTime = (_accelerationTime * 2) + _dwellTime;
        }

        public double GetPosition(double time)
        {
            //validate
            if (time <= 0)
                return _start;
            if (time >= _totalTime)
                return _end;

            //acceleration
            var duration = Math.Min(time, _accelerationTime);
            var position = Kinamatic.GetDistanceUsingAcceleration(0, _acceleration, duration);

            //dwell
            if (time > _accelerationTime)
            {
                duration = Math.Min(time - _accelerationTime, _dwellTime);
                position += Kinamatic.GetDistanceUsingAcceleration(_peakVelocity, 0, duration);
            }

            //deceleration
            if (time > _accelerationTime + _dwellTime)
            {
                duration = Math.Min(time - (_accelerationTime + _dwellTime), _accelerationTime);
                position += Kinamatic.GetDistanceUsingAcceleration(_peakVelocity, -_acceleration, duration);
            }

            if (_start < _end)
                position = _start + position;
            else
                position = _start - position;

            return position;
        }

        public double GetCompleted(double time)
        {
            if (time <= 0)
                return 0;
            if (time >= _totalTime)
                return 1;
            return time / _totalTime;
        }

        public double Start
        {
            get { return _start; }
        }

        public double End
        {
            get { return _end; }
        }

        public double TotalTime
        {
            get { return _totalTime; }
        }
    }
}
