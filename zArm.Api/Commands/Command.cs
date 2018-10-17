using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace zArm.Api.Commands
{

    public class CommandException : Exception
    {
        public CommandException(string message) : base(message) { }
    }


    public abstract class Message
    {
        [Order]
        public abstract int ID { get; }
    }

    public abstract class Command : Message
    {
        protected int? Constrain(int? value, int low = 1, int high = 10)
        {
            if (value == null)
                return value;
            return Constrain(value.Value, low, high);
        }

        protected int Constrain(int value, int low = 1, int high = 10)
        {
            if (value < low)
                return low;
            if (value > high)
                return high;
            return value;
        }

        protected float? Constrain(float? value, float low = 1, float high = 10)
        {
            if (value == null)
                return value;
            return Constrain(value.Value, low, high);
        }

        protected float Constrain(float value, float low = 1, float high = 10)
        {
            if (value < low)
                return low;
            if (value > high)
                return high;
            return value;
        }
    }

    public abstract class Response : Message
    {
    }

    public abstract class MessageResponse : Response
    {
        [Order]
        public string Message { get; set; }
    }


    //Messages

    public class ErrorResponse : MessageResponse { override public int ID { get; } = -1; }

    public class AcknowledgementResponse : MessageResponse { override public int ID { get; } = 0; }

    public class InfoResponse : MessageResponse { override public int ID { get; } = 1; }


    //Global

    public class PingCommand : Command { override public int ID { get; } = 1; }

    public class HelpCommand : Command { override public int ID { get; } = 2; }

    public class SoftResetCommand : Command { override public int ID { get; } = 3; }

    public class SoftResetResponse : Response { override public int ID { get; } = 3; }



    //Settings

    public class SettingReadCommand : Command
    {
        override public int ID { get; } = 4;
        [Order]
        public int SettingID { get; set; }
    }

    public class SettingReadResponse : Response
    {
        override public int ID { get; } = 4;
        [Order]
        public int SettingID { get; set; }
        [Order]
        public string Value { get; set; }
    }

    public class SettingWriteCommand : Command
    {
        override public int ID { get; } = 5;
        [Order]
        public int SettingID { get; set; }
        [Order]
        public string Value { get; set; }
    }

    //Servo

    public abstract class ServoResponse : Response //used in AsyncCommandFirstServoResponse
    {
        [Order]
        public int ServoID { get; set; }
    }

    [FastCommand]
    public class ServoPositionCommand : Command
    {
        override public int ID { get; } = 10;

        int _servoID;
        [Order]
        public int ServoID
        {
            get { return _servoID; }
            set { _servoID = Constrain(value, 1, 7); }
        }

        float _position;
        [Order]
        public float Position
        {
            get { return _position; }
            set { _position = Constrain(value, CommandBuilder.MinServoPosition, CommandBuilder.MaxServoPosition); }
        }
    }

    public class ServoPositionChangedCommand : Command
    {
        override public int ID { get; } = 11;
        [Order]
        public int ServoID { get; set; }
        [Order]
        public bool Enabled { get; set; }
    }

    public class ServoPositionChangedResponse : ServoResponse
    {
        override public int ID { get; } = 11;
        [Order]
        public float Position { get; set; }
        [Order]
        public bool IsOn { get; set; }
        [Order]
        public bool IsVibrating { get; set; }
    }

    public class ServoStatusCommand : Command
    {
        override public int ID { get; } = 12;

        int? _servoID;
        [Order]
        public int? ServoID
        {
            get { return _servoID; }
            set { _servoID = Constrain(value, 0, 7); }
        }
    }

    public class ServoStatusResponse : ServoPositionChangedResponse
    {
        override public int ID { get; } = 12;
        [Order]
        public float LastSetPosition { get; set; }
        [Order]
        public bool IsMoving { get; set; }
    }

    public interface IsServoOnCommandType { }

    public class ServoOnCommand : ServoStatusCommand, IsServoOnCommandType { override public int ID { get; } = 13; }

    public class ServoOnResponse : ServoResponse { override public int ID { get; } = 13; }

    public class ServoOffCommand : ServoStatusCommand { override public int ID { get; } = 14; }

    public class ServoOffResponse : ServoResponse { override public int ID { get; } = 14; }

    public class ServoSetCalibrationCommand : Command
    {
        override public int ID { get; } = 15;
        [Order]
        public bool Enable { get; set; }
    }

    public class ServoGetCalibrationCommand : Command { override public int ID { get; } = 16; }

    public class ServoGetCalibrationResponse : Response
    {
        override public int ID { get; } = 16;
        [Order]
        public bool Enabled { get; set; }
    }

    public class ServoCalibrationMoveCommand : ServoPositionCommand { override public int ID { get; } = 17; }

    public class ServoCalibrationMoveResponse : ServoResponse
    {
        override public int ID { get; } = 17;
        [Order]
        public int Duration { get; set; }
    }




    //Servo Movement

    public class MoveCommand : ServoPositionCommand, IsServoOnCommandType
    {
        override public int ID { get; } = 20;

        int? _speed;
        [Order]
        public int? Speed
        {
            get { return _speed; }
            set { _speed = Constrain(value, 0, 100); }
        }

        int? _easeIn;
        [Order]
        public int? EaseIn
        {
            get { return _easeIn; }
            set { _easeIn = Constrain(value, 0, 100); }
        }

        int? _easeOut;
        [Order]
        public int? EaseOut
        {
            get { return _easeOut; }
            set { _easeOut = Constrain(value, 0, 100); }
        }

        float? _percentageComplete;
        [Order]
        public float? PercentageComplete
        {
            get { return _percentageComplete; }
            set { _percentageComplete = Constrain(value, 0, 1); }
        }
    }

    public class MoveResponse : ServoResponse
    {
        override public int ID { get; } = 20;
        [Order]
        public float PercentageComplete { get; set; }
    }

    public class MoveStopCommand : ServoStatusCommand { override public int ID { get; } = 21; }

    public class MoveAllCommand : Command, IsServoOnCommandType
    {
        public readonly int ServoCount;

        public MoveAllCommand() : this(5) { }
        public MoveAllCommand(int servoCount)
        {
            ServoCount = servoCount;
        }

        override public int ID { get; } = 22;

        float? _servo1_Position;
        [Order]
        [Required(1)]
        public float? Servo1_Position
        {
            get { return _servo1_Position; }
            set { _servo1_Position = Constrain(value, CommandBuilder.MinServoPosition, CommandBuilder.MaxServoPosition); }
        }

        float? _servo2_Position;
        [Order]
        [Required(2)]
        public float? Servo2_Position
        {
            get { return _servo2_Position; }
            set { _servo2_Position = Constrain(value, CommandBuilder.MinServoPosition, CommandBuilder.MaxServoPosition); }
        }

        float? _servo3_Position;
        [Order]
        [Required(3)]
        public float? Servo3_Position
        {
            get { return _servo3_Position; }
            set { _servo3_Position = Constrain(value, CommandBuilder.MinServoPosition, CommandBuilder.MaxServoPosition); }
        }

        float? _servo4_Position;
        [Order]
        [Required(4)]
        public float? Servo4_Position
        {
            get { return _servo4_Position; }
            set { _servo4_Position = Constrain(value, CommandBuilder.MinServoPosition, CommandBuilder.MaxServoPosition); }
        }

        float? _servo5_Position;
        [Order]
        [Required(5)]
        public float? Servo5_Position
        {
            get { return _servo5_Position; }
            set { _servo5_Position = Constrain(value, CommandBuilder.MinServoPosition, CommandBuilder.MaxServoPosition); }
        }

        float? _servo6_Position;
		//TEMP: disabled for now
        //[Order]
        //[Required(6)]
        public float? Servo6_Position
        {
            get { return _servo6_Position; }
            set { _servo6_Position = Constrain(value, CommandBuilder.MinServoPosition, CommandBuilder.MaxServoPosition); }
        }

        float? _servo7_Position;
		//TEMP: disabled for now
		//[Order]
		//[Required(7)]
		public float? Servo7_Position
        {
            get { return _servo7_Position; }
            set { _servo7_Position = Constrain(value, CommandBuilder.MinServoPosition, CommandBuilder.MaxServoPosition); }
        }

        int? _speed;
        [Order]
        public int? Speed
        {
            get { return _speed; }
            set { _speed = Constrain(value, 0, 100); }
        }

        int? _easeIn;
        [Order]
        public int? EaseIn
        {
            get { return _easeIn; }
            set { _easeIn = Constrain(value, 0, 100); }
        }

        int? _easeOut;
        [Order]
        public int? EaseOut
        {
            get { return _easeOut; }
            set { _easeOut = Constrain(value, 0, 100); }
        }

        float? _percentageComplete;
        [Order]
        public float? PercentageComplete
        {
            get { return _percentageComplete; }
            set { _percentageComplete = Constrain(value, 0, 1); }
        }
    }

    public class MoveAllSynchronizedCommand : MoveAllCommand
    {
        public MoveAllSynchronizedCommand() : this(5) { }
        public MoveAllSynchronizedCommand(int servoCount) : base(servoCount) { }

        override public int ID { get; } = 23;
    }



    //LED

    public class LedOffCommand : Command { override public int ID { get; } = 40; }

    public class LedOnCommand : Command
    {
        override public int ID { get; } = 41;
        [Order]
        public LedColor? Color { get; set; }
    }

    public class LedFadeCommand : Command
    {
        override public int ID { get; } = 42;

        int? _brightness;
        [Order]
        public int? Brightness
        {
            get { return _brightness; }
            set { _brightness = Constrain(value, 0); }
        }

        [Order]
        public LedColor? Color { get; set; }
    }

    public class LedBlinkCommand : Command
    {
        override public int ID { get; } = 43;

        int? _speed;
        [Order]
        public int? Speed
        {
            get { return _speed; }
            set { _speed = Constrain(value); }
        }

        int? _count;
        [Order]
        public int? Count
        {
            get { return _count; }
            set { _count = Constrain(value, 0, 100); }
        }

        [Order]
        public LedColor? Color { get; set; }
    }

    public class LedPulseCommand : LedBlinkCommand { override public int ID { get; } = 44; }

    public class LedSyncButtonCommand : LedFadeCommand { override public int ID { get; } = 46; }

    public class LedSyncButtonOffCommand : Command { override public int ID { get; } = 47; }

    public class LedStatusCommand : Command { override public int ID { get; } = 45; }

    public class LedStatusResponse : Response
    {
        override public int ID { get; } = 45;
        [Order]
        public LedState State { get; set; }
        [Order]
        public int Value { get; set; }
        [Order]
        public int Count { get; set; }
        [Order]
        public int CurrentCount { get; set; }
    }

    public enum LedState : int
    {
        Off = 0,
        On = 1,
        Fade = 2,
        Blink = 3,
        Pulse = 4
    }

    public struct LedColor
    {
        public static readonly LedColor Red = (LedColor)"#FF0000";
        public static readonly LedColor Green = (LedColor)"#00FF00";
        public static readonly LedColor Blue = (LedColor)"#0000FF";
        public static readonly LedColor White = (LedColor)"#FFFFFF";
        public static readonly LedColor Black = (LedColor)"#000000";
        public static readonly LedColor Yellow = (LedColor)"#FFFF00";
        public static readonly LedColor Orange = (LedColor)"#FF8000";

        public byte R;
        public byte G;
        public byte B;

        public LedColor(byte red, byte green, byte blue)
        {
            //set fields
            R = red;
            G = green;
            B = blue;
        }

        public LedColor(string hashcode)
        {
            try
            {
                hashcode = hashcode.Replace("#", string.Empty);
                var color = Enumerable.Range(0, hashcode.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hashcode.Substring(x, 2), 16))
                     .ToArray();
                if (color.Length >= 3)
                {
                    R = color[0];
                    G = color[1];
                    B = color[2];
                    return;
                }
            }
            catch { }

            //default white
            R = 255;
            G = 255;
            B = 255;
        }

        public static explicit operator LedColor(string hashcode)  // implicit digit to byte conversion operator
        {
            return new LedColor(hashcode);
        }

        public static bool operator ==(LedColor a, LedColor b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.R == b.R && a.G == b.G && a.B == b.B;
        }

        public static bool operator !=(LedColor a, LedColor b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return $"{R} {G} {B}";
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }



    //Button

    public class ButtonDownResponse : Response
    {
        override public int ID { get; } = 30;
        [Order]
        public int GapTime { get; set; }
    }

    public class ButtonUpResponse : Response
    {
        override public int ID { get; } = 31;
        [Order]
        public int PressedTime { get; set; }
        [Order]
        public bool WasLong { get; set; }
        [Order]
        public int Count { get; set; }
        [Order]
        public int ComboCount { get; set; }
    }

    public class ButtonStatusCommand : Command { override public int ID { get; } = 32; }

    public class ButtonStatusResponse : Response
    {
        override public int ID { get; } = 32;
        [Order]
        public bool IsDown { get; set; }
        [Order]
        public int Count { get; set; }
    }

    public class ButtonCountResetCommand : Command { override public int ID { get; } = 33; }




    //Knob

    public class KnobPositionResponse : Response
    {
        override public int ID { get; } = 60;
        [Order]
        public int Position { get; set; }
    }

    public class KnobPositionChangedResponse : Response
    {
        override public int ID { get; } = 61;
        [Order]
        public int Position { get; set; }
        [Order]
        public int Amount { get; set; }
    }

    public class KnobRangeResponse : Response
    {
        override public int ID { get; } = 64;
        [Order]
        public int Min { get; set; }
        [Order]
        public int Max { get; set; }
    }

    public class KnobPositionCommand : Command { override public int ID { get; } = 60; }

    public class KnobPositionSetCommand : Command
    {
        override public int ID { get; } = 62;
        [Order]
        public int Position { get; set; }
    }

    public class KnobRangeCommand : Command { override public int ID { get; } = 64; }

    public class KnobRangeSetCommand : Command
    {
        override public int ID { get; } = 63;
        [Order]
        public int Min { get; set; }
        [Order]
        public int Max { get; set; }
    }




    //Sound

    public class SoundPlayNotesCommand : Command
    {
        override public int ID { get; } = 50;
        [Order]
        public string Notes { get; set; }
        [Order]
        public bool CallbackWhenDone { get; set; }
    }

    public class SoundPlayNotesResponse : Response
    {
        override public int ID { get; } = 50;
        [Order]
        public bool IsPlayingComplete { get; set; }
    }

    public class SoundStopCommand : Command { override public int ID { get; } = 51; }

    public class SoundStatusCommand : Command { override public int ID { get; } = 52; }

    public class SoundStatusResponse : Response
    {
        override public int ID { get; } = 52;
        [Order]
        public bool IsPlaying { get; set; }
    }

    public class SoundPlayFreqCommand : Command
    {
        override public int ID { get; } = 53;

        int? _frequency;
        [Order]
        public int? Frequency
        {
            get { return _frequency; }
            set { _frequency = Constrain(value, 1, 4000); }
        }
    }

    public class SoundSyncButtonCommand : SoundPlayNotesCommand { override public int ID { get; } = 54; }

    public class SoundSyncButtonOffCommand : Command { override public int ID { get; } = 55; }

    public class SoundSyncLedCommand : SoundPlayNotesCommand { override public int ID { get; } = 56; }

    public class SoundSyncLedOffCommand : Command { override public int ID { get; } = 57; }




    /// <summary>
    /// indicates that the parameter order is important and will be serialized and desrialized in the order it apears in the source code
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class OrderAttribute : Attribute
    {
        private readonly int _order;
        public OrderAttribute([CallerLineNumber]int order = 0)
        {
            _order = order;
        }

        public int Order
        {
            get { return _order; }
        }
    }

    /// <summary>
    /// indicates that a nullable parameter is required, and if serialized when null use a CommandBuilder.DefaultValueWhenMissingParam
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class RequiredAttribute : Attribute
    {
        readonly int? _servoID;

        public RequiredAttribute()
        {
        }

        public RequiredAttribute(int servoID)
        {
            _servoID = servoID;
        }

        public int? ServoID
        {
            get { return _servoID; }
        }
    }

    /// <summary>
    /// indicates that a command can be processed by the robot fast, and doesn't require a max send speed limit  CommandBuilder.MaxSendSpeed
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class FastCommandAttribute : Attribute
    {
    }
}
