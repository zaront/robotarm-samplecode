using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api.Motion
{
    public class ServoMotion
    {
        int _maxSpeed;
        ulong _micros;
        float _totalTimeSteps;
        float _percentageComplete;
        float _inStartPos;
        float _inEndPose;
        int _inSpd;
        int _inEaseIn;
        int _inEaseOut;
        float _inMaxSpeed;
        float _inPercentageComplete;

        //these match variables in FeedbackServo.h of the firmware
        float _moveStartPos;
        float _moveEndPos;
        float _moveSpeed;
        float _easeInDuration;
        float _easeInAccel;
        float _dwellDuration;
        float _easeOutAccel;
        float _moveStartTime = 0;
        float _totalDuration;

        public bool IsMoving { get; set; }

        public ServoMotion(int maxSpeed)
        {
            //set fields
            _maxSpeed = maxSpeed;
        }

        public int MaxSpeed
        {
            get { return _maxSpeed; }
            set { _maxSpeed = value; }
        }

        public float PercentageComplete
        {
            get { return _percentageComplete; }
        }

        public float TotalDuration
        {
            get { return _totalDuration; }
        }

        public float SetMotionSpeed(int spd)
        {
            return SetMotion(_inStartPos, _inEndPose, spd, _inEaseIn, _inEaseOut, _inMaxSpeed, _inPercentageComplete);
        }

        public int Speed
        {
            get { return _inSpd; }
        }

        public float SetMotion(float startPos, float endPos, int spd, int easeIn, int easeOut, float maxSpeed = 0, float percentageComplete = 0)
        {
            //reset timeSteps
            _totalTimeSteps = 0;

            //get start pos
            _moveStartPos = startPos;
            var pos = endPos;

            //set fields
            _inStartPos = startPos;
            _inEndPose = endPos;
            _inEaseIn = easeIn;
            _inEaseOut = easeOut;
            _inSpd = spd;
            _inMaxSpeed = maxSpeed;
            _inPercentageComplete = percentageComplete;


            //This exactly matches code in firmeware at FeedbackServo.cpp (FeedbackServo::Move)

            //set speed
            if (maxSpeed == 0)
                maxSpeed = _maxSpeed;
            _moveSpeed = spd * (1.0f / maxSpeed) * 10.0f;
            if (pos < _moveStartPos)
                _moveSpeed = -_moveSpeed;

            //set end Pos
            _moveEndPos = pos;

            //set easeIn
            _easeInDuration = 0;
            if (easeIn == 0)
            {
                _easeInAccel = 0;
            }
            else
            {
                _easeInAccel = (1.0f / easeIn) * .002f;
                if (_moveSpeed < 0)
                    _easeInAccel = -_easeInAccel; //same as movespeed direction
                _easeInDuration = abs(_moveSpeed / _easeInAccel); //milliseconds till moveSpeed
            }

            //set easeOut
            float easeOutDuration = 0;
            if (easeOut == 0)
            {
                _easeOutAccel = 0;
            }
            else
            {
                _easeOutAccel = (1.0f / easeOut) * .002f;
                if (_moveSpeed > 0)
                    _easeOutAccel = -_easeOutAccel; //opposite of movespeed direction
                easeOutDuration = abs(_moveSpeed / _easeOutAccel); //milliseconds till 0
            }

            //if easeOut & easeIn prevent reaching moveSpeed then lower moveSpeed to intercept speed
            float easeInDist = abs(.5f * _easeInAccel * (_easeInDuration * _easeInDuration));
            float easeOutDist = abs(.5f * _easeOutAccel * (easeOutDuration * easeOutDuration));
            if (abs(easeInDist + easeOutDist) > abs(_moveEndPos - _moveStartPos))
            {
                //calculate intercept speed
                float d = abs(_moveEndPos - _moveStartPos);
                float ai = abs(_easeInAccel);
                float ao = abs(_easeOutAccel);
                float newMoveSpeed = 0;
                if (easeInDist != 0 && easeOutDist != 0)
                {
                    //both easeOut & easeIn
                    if (easeIn > easeOut)
                    {
                        d = (easeInDist / (easeInDist + easeOutDist)) * d;
                        newMoveSpeed = sqrt(2.0f * ai * d);
                    }
                    else
                    {
                        d = (easeOutDist / (easeInDist + easeOutDist)) * d;
                        newMoveSpeed = sqrt(2.0f * ao * d);
                    }
                }
                else if (easeInDist != 0)
                    newMoveSpeed = sqrt(2.0f * ai * d); // easeIn only
                else
                    newMoveSpeed = sqrt(2.0f * ao * d); // easeOut only

                //assign new speed
                if (_moveSpeed < 0)
                    _moveSpeed = -newMoveSpeed;
                else
                    _moveSpeed = newMoveSpeed;

                //update distances and time
                if (_easeInDuration != 0)
                {
                    _easeInDuration = abs(_moveSpeed / _easeInAccel); //milliseconds till moveSpeed
                    easeInDist = abs(.5f * _easeInAccel * (_easeInDuration * _easeInDuration));
                }
                if (easeOutDuration != 0)
                {
                    easeOutDuration = abs(_moveSpeed / _easeOutAccel); //milliseconds till 0
                    easeOutDist = abs(.5f * _easeOutAccel * (easeOutDuration * easeOutDuration));
                }
            }

            //calculate time
            if (_moveSpeed == 0)
            {
                _dwellDuration = 0;
                _totalDuration = 0;
            }
            else
            {
                _dwellDuration = 0;
                float remainingDist = abs(_moveEndPos - _moveStartPos) - abs(easeInDist + easeOutDist);
                if (remainingDist > 0)
                    _dwellDuration = abs(remainingDist / _moveSpeed);
                _totalDuration = _dwellDuration + _easeInDuration + easeOutDuration;
            }



            //mark as moving
            IsMoving = true;
            _percentageComplete = 0;

            //set start time
            if (percentageComplete == 0)
                _moveStartTime = 0;
            else
                _moveStartTime = _totalDuration * percentageComplete;

            return _totalDuration;
        }

        public float GetPositionIncriment(float timeStep)
        {
            //incriment timesteps
            _totalTimeSteps += timeStep * 1000f;

            return GetPosition(_totalTimeSteps);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalDuration">milliseconds</param>
        /// <returns></returns>
        public float GetPosition(float totalDuration)
        {
            //set duration in microseconds
            _micros = (ulong)Math.Round(totalDuration * 1000);

            var position = GetPosition();

            //update percentage complete
            _percentageComplete = _micros / 1000 / _totalDuration;

            if (position == _moveEndPos)
            {
                IsMoving = false;
                _percentageComplete = 1;
            }

            return position;
        }

        float GetPosition()
        {
            //This exactly matches code in firmeware at FeedbackServo.cpp (FeedbackServo::Update)

            //calculate new position using easing
            float totalDuration = (micros() - _moveStartTime) / 1000.0f; //convert to floating milliseconds
            float remainingDuration = totalDuration;
            float duration;
            float newPosition = _moveStartPos;

            //ease in time
            if (_easeInDuration > 0)
            {
                duration = min(remainingDuration, _easeInDuration);
                remainingDuration -= duration;
                if (duration > 0)
                    newPosition += (.5f * _easeInAccel * (duration * duration)); //get distance from time - accelerating from 0
            }
            //dwell time
            if (_dwellDuration > 0)
            {
                duration = min(remainingDuration, _dwellDuration);
                remainingDuration -= duration;
                if (duration > 0)
                    newPosition += duration * _moveSpeed; //get distance from time - constant velocity
            }
            //ease out time
            if (remainingDuration > 0)
            {
                newPosition += (_moveSpeed * remainingDuration) + (.5f * _easeOutAccel * (remainingDuration * remainingDuration)); //get distance from time - decelerating from velocity
            }

            //set the position
            if (totalDuration >= _totalDuration || (newPosition >= _moveEndPos && _moveSpeed > 0) || (newPosition <= _moveEndPos && _moveSpeed < 0))
            {
                newPosition = _moveEndPos;
                //successfuly reached position
            }
            return newPosition;
        }

        float abs(float value)
        {
            return Math.Abs(value);
        }

        float micros()
        {
            return _micros;
        }

        float min(float v1, float v2)
        {
            return Math.Min(v1, v2);
        }

        float sqrt(float value)
        {
            return (float)Math.Sqrt(value);
        }




        //public static ServoMotionParams[] PlanGroupMotion(ServoMotionParams parameters, params ServoMotion[] servoMotions)
        //{
        //    //use slowest speed
        //    var maxSpeed = servoMotions.Min(i => i.MaxSpeed);
        //}
    }


    public class ServoMotionParams
    {
        public int Speed { get; set; }
        public int EaseIn { get; set; }
        public int EaseOut { get; set; }
        public float MaxSpeed { get; set; }
    }
}
