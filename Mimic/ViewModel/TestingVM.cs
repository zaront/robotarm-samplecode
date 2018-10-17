using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using zArm.Api;
using zArm.Api.Commands;
using zArm.Api.Specialized;

namespace Mimic.ViewModel
{
    class TestingVM : BaseVM, IModule
    {
        Arm _arm;
        public ConsoleVM Console { get; }

        //led
        double _led_FadeBrightness = 5;
        double _led_BlinkSpeed = 5;
        double _led_BlinkCount = 3;
        double _led_PulseSpeed = 5;
        double _led_PulseCount = 3;
        string _led_Status;
        bool _led_SyncBtn;
        bool _led_UseColor;
        double _led_SyncBtnBrightness = 5;
        Color? _led_SelectedColor;
        public ICommand Led_OnCommand { get; }
        public ICommand Led_OffCommand { get; }
        public ICommand Led_FadeCommand { get; }
        public ICommand Led_BlinkCommand { get; }
        public ICommand Led_PulseCommand { get; }
        public ICommand Led_GetStatusCommand { get; }

        //button
        string _button_info;
        string _button_infoState;
        SolidColorBrush _button_infoColor;
        string _button_status;
        public ICommand Button_GetStatusCommand { get; }
        public ICommand Button_ResetCountCommand { get; }

        //knob
        string _knob_info;
        string _knob_status;
        int _knob_position = 5;
        int _knob_rangeMin = 1;
        int _knob_rangeMax = 10;
        public ICommand Knob_GetStatusCommand { get; }
        public ICommand Knob_SetRangeCommand { get; }

        //sound
        bool _sound_SyncBtn;
        bool _sound_SyncLed;
        double _sound_FreqValue = 500;
        string _sound_PlayNodes = "C-8,C#-8,E5,R-8,A5-2";
        string _sound_Status;
        string _sound_SyncBtnNotes = "A5";
        string _sound_SyncLedNotes = "F-16,A-16";
        public ICommand Sound_StopCommand { get; }
        public ICommand Sound_PlayCommand { get; }
        public ICommand Sound_PlayFreqCommand { get; }
        public ICommand Sound_GetStatusCommand { get; }

        //servo
        bool _servo_IsCalibrated;
        bool _servo_Listen;
        bool _servo_SyncPos;
        bool _servo_IgnoreJitter;
        int _servo_EaseIn;
        int _servo_EaseOut;
        int _servo_Speed = 50;
        bool _servo_MoveSync;
        public ICommand Servo_OnCommand { get; }
        public ICommand Servo_OffCommand { get; }
        public TestingServoVM[] Servos { get; private set; }
        public ICommand Servo_MoveCommand { get; }
        public ICommand Servo_MoveStopCommand { get; }

        public TestingVM()
        {
            //set fields
            Console = new ConsoleVM();

            //Led
            Led_OnCommand = new RelayCommand(Led_On);
            Led_OffCommand = new RelayCommand(Led_Off);
            Led_FadeCommand = new RelayCommand(Led_Fade);
            Led_BlinkCommand = new RelayCommand(Led_Blink);
            Led_PulseCommand = new RelayCommand(Led_Pulse);
            Led_GetStatusCommand = new RelayCommandAsync(Led_GetStatus);

            //Button
            Button_GetStatusCommand = new RelayCommandAsync(Button_GetStatus);
            Button_ResetCountCommand = new RelayCommand(Button_ResetCount);

            //Knob
            Knob_GetStatusCommand = new RelayCommandAsync(Knob_GetStatus);
            Knob_SetRangeCommand = new RelayCommand(Knob_SetRange);

            //Sound
            Sound_StopCommand = new RelayCommand(Sound_Stop);
            Sound_PlayCommand = new RelayCommand(Sound_Play);
            Sound_PlayFreqCommand = new RelayCommand(Sound_PlayFreq);
            Sound_GetStatusCommand = new RelayCommandAsync(Sound_GetStatus);

            //Servo
            Servo_OnCommand = new RelayCommand(Servo_On);
            Servo_OffCommand = new RelayCommand(Servo_Off);
            Servo_MoveCommand = new RelayCommand(Servo_Move);
            Servo_MoveStopCommand = new RelayCommand(Servo_MoveStop);
        }

		public MainWindowVM Main { get => App.Instance; }

		async void IModule.ShowingModule()
        {
            _arm = App.Instance.Arm;

            //Servos
            if (Servos == null)
            {
                Servos = new TestingServoVM[_arm.Servos.Count];
                for (int i = 0; i < _arm.Servos.Count; i++)
                    Servos[i] = new TestingServoVM(_arm.Servos[i], this);
            }

            (Console as IModule)?.ShowingModule();

            //button
            _arm.Button.Up += Button_Up;
            _arm.Button.Down += Button_Down;

            //knob
            _arm.Knob.PositionChanged += Knob_PositionChanged;

            //servo
            foreach (var servo in Servos)
                servo.ShowingModule();
            Servo_IsCalibrated = await _arm.Servos.IsCalibrationEnabledAsync();

        }

        void IModule.HidingModule()
        {
            (Console as IModule)?.HidingModule();

            //button
            _arm.Button.Up -= Button_Up;
            _arm.Button.Down -= Button_Down;

            //knob
            _arm.Knob.PositionChanged -= Knob_PositionChanged;

            //servo
            foreach (var servo in Servos)
                servo.HidingModule();
            
        }

        #region Led

        void Led_On()
        {
            App.Instance.Arm.Led.On(GetLedColor());
        }

        void Led_Off()
        {
            App.Instance.Arm.Led.Off();
        }

        void Led_Fade()
        {
            _arm.Led.Fade((int)Led_FadeBrightness, GetLedColor());
        }

        public double Led_FadeBrightness
        {
            get { return _led_FadeBrightness; }
            set { _led_FadeBrightness = value; FirePropertyChanged(); }
        }

        void Led_Blink()
        {
            App.Instance.Arm.Led.Blink((int)Led_BlinkSpeed, (int)Led_BlinkCount, GetLedColor());
        }

        public double Led_BlinkSpeed
        {
            get { return _led_BlinkSpeed; }
            set { _led_BlinkSpeed = value; FirePropertyChanged(); }
        }

        public double Led_BlinkCount
        {
            get { return _led_BlinkCount; }
            set { _led_BlinkCount = value; FirePropertyChanged(); }
        }

        void Led_Pulse()
        {
            _arm.Led.Pulse((int)Led_PulseSpeed, (int)Led_PulseCount, GetLedColor());
        }

        public double Led_PulseSpeed
        {
            get { return _led_PulseSpeed; }
            set { _led_PulseSpeed = value; FirePropertyChanged(); }
        }

        public double Led_PulseCount
        {
            get { return _led_PulseCount; }
            set { _led_PulseCount = value; FirePropertyChanged(); }
        }

        async Task Led_GetStatus()
        {
            var result = await _arm.Led.GetStatusAsync();
            Led_Status = (result != null) ? $"{result.State} value={result.Value} count={result.Count} current count={result.CurrentCount}" : null;
        }

        public string Led_Status
        {
            get { return _led_Status; }
            set { _led_Status = value; FirePropertyChanged(); }
        }

        public bool? Led_SyncBtn
        {
            get { return _led_SyncBtn; }
            set
            {
                //validate
                if (_led_SyncBtn == value)
                    return;

                _led_SyncBtn = value.GetValueOrDefault();
                if (_led_SyncBtn)
                    _arm.Led.SyncWithButton((int)Led_SyncBtnBrightness, GetLedColor());
                else
                    _arm.Led.SyncWithButtonOff();

                FirePropertyChanged();
            }
        }

        public double Led_SyncBtnBrightness
        {
            get { return _led_SyncBtnBrightness; }
            set { _led_SyncBtnBrightness = value; FirePropertyChanged(); }
        }

        public bool? Led_UseColor
        {
            get { return _led_UseColor; }
            set { _led_UseColor = value.GetValueOrDefault(); FirePropertyChanged(); }
        }

        public Color? Led_SelectedColor
        {
            get { return _led_SelectedColor; }
            set { _led_SelectedColor = value; FirePropertyChanged(); }
        }

        LedColor? GetLedColor()
        {
            if (!_led_UseColor)
                return null;
            if (Led_SelectedColor == null)
                return null;
            var color = Led_SelectedColor.Value;
            return new LedColor(color.R, color.G, color.B);
        }

        #endregion

        #region Button

        private void Button_Up(object sender, DataEventArg<ButtonUpResponse> e)
        {
            if (SwitchToMainThread(sender, e)) return;

            Button_InfoState = "Button Up";
            Button_InfoColor = Brushes.Transparent;
            Button_Info = $"pressed time={TimeSpan.FromMilliseconds(e.Data.PressedTime)} long={e.Data.WasLong} count={e.Data.Count} combo={e.Data.ComboCount}";
        }

        private void Button_Down(object sender, DataEventArg<ButtonDownResponse> e)
        {
            if (SwitchToMainThread(sender, e)) return;

            Button_InfoState = "Button Down";
            Button_InfoColor = Brushes.DarkGreen;
            Button_Info = $"gap time={TimeSpan.FromMilliseconds(e.Data.GapTime)}";
        }

        public string Button_Info
        {
            get { return _button_info; }
            set { _button_info = value; FirePropertyChanged(); }
        }

        public string Button_InfoState
        {
            get { return _button_infoState; }
            set { _button_infoState = value; FirePropertyChanged(); }
        }

        public SolidColorBrush Button_InfoColor
        {
            get { return _button_infoColor; }
            set { _button_infoColor = value; FirePropertyChanged(); }
        }

        public string Button_Status
        {
            get { return _button_status; }
            set { _button_status = value; FirePropertyChanged(); }
        }

        async Task Button_GetStatus()
        {
            var result = await _arm.Button.GetStatusAsync();
            Button_Status = $"is Down={result.IsDown} count={result.Count}";
        }

        void Button_ResetCount()
        {
            _arm.Button.ResetPressedCount();
        }

        #endregion

        #region Knob

        private void Knob_PositionChanged(object sender, DataEventArg<KnobPositionChangedResponse> e)
        {
            if (SwitchToMainThread(sender, e)) return;

            Knob_Info = $"pos={e.Data.Position} change={e.Data.Amount}";
        }

        public string Knob_Info
        {
            get { return _knob_info; }
            set { _knob_info = value; FirePropertyChanged(); }
        }

        public string Knob_Status
        {
            get { return _knob_status; }
            set { _knob_status = value; FirePropertyChanged(); }
        }

        public int Knob_Position
        {
            get { return _knob_position; }
            set { _knob_position = value; FirePropertyChanged(); }
        }

        public int Knob_RangeMin
        {
            get { return _knob_rangeMin; }
            set { _knob_rangeMin = value; FirePropertyChanged(); }
        }

        public int Knob_RangeMax
        {
            get { return _knob_rangeMax; }
            set { _knob_rangeMax = value; FirePropertyChanged(); }
        }

        async Task Knob_GetStatus()
        {
            var pos = await _arm.Knob.GetPositionAsync();
            var range = await _arm.Knob.GetRangeAsync();
            Knob_Status = $"pos={pos.Position} range={range.Min}~{range.Max}";
        }

        void Knob_SetRange()
        {
            _arm.Knob.SetPosition(Knob_Position);
            _arm.Knob.SetRange(Knob_RangeMin, Knob_RangeMax);
        }

        #endregion

        #region Sound

        public bool Sound_SyncBtn
        {
            get { return _sound_SyncBtn; }
            set
            {
                //validate
                if (_sound_SyncBtn == value)
                    return;

                _sound_SyncBtn = value;
                if (_sound_SyncBtn)
                    _arm.Sound.SyncWithButton(Sound_SyncBtnNotes);
                else
                    _arm.Sound.SyncWithButtonOff();

                FirePropertyChanged();
            }
        }

        public bool Sound_SyncLed
        {
            get { return _sound_SyncLed; }
            set
            {
                //validate
                if (_sound_SyncLed == value)
                    return;

                _sound_SyncLed = value;
                if (_sound_SyncLed)
                    _arm.Sound.SyncWithLed(Sound_SyncLedNotes);
                else
                    _arm.Sound.SyncWithLedOff();

                FirePropertyChanged();
            }
        }

        public double Sound_FreqValue
        {
            get { return _sound_FreqValue; }
            set { _sound_FreqValue = value; FirePropertyChanged(); }
        }

        public string Sound_PlayNodes
        {
            get { return _sound_PlayNodes; }
            set { _sound_PlayNodes = value; FirePropertyChanged(); }
        }

        public string Sound_Status
        {
            get { return _sound_Status; }
            set { _sound_Status = value; FirePropertyChanged(); }
        }

        public string Sound_SyncBtnNotes
        {
            get { return _sound_SyncBtnNotes; }
            set { _sound_SyncBtnNotes = value; FirePropertyChanged(); }
        }

        public string Sound_SyncLedNotes
        {
            get { return _sound_SyncLedNotes; }
            set { _sound_SyncLedNotes = value; FirePropertyChanged(); }
        }

        void Sound_Stop()
        {
            _arm.Sound.Stop();
        }

        void Sound_Play()
        {
            _arm.Sound.PlayNotes(Sound_PlayNodes);
        }

        void Sound_PlayFreq()
        {
            _arm.Sound.PlayFrequency((int)Math.Round(Sound_FreqValue));
        }

        async Task Sound_GetStatus()
        {
            var result = await _arm.Sound.GetStatusAsync();
            Sound_Status = $"is playing={result.IsPlaying}";
        }

        #endregion

        #region Servo

        public bool Servo_Listen
        {
            get { return _servo_Listen; }
            set
            {
                //validate
                if (_servo_Listen == value)
                    return;

                _servo_Listen = value;
                if (_servo_Listen)
                    _arm.Servos.EnablePositionChanged();
                else
                    _arm.Servos.DisablePositionChanged();

                FirePropertyChanged();
            }
        }

        public bool Servo_SyncPos
        {
            get { return _servo_SyncPos; }
            set { _servo_SyncPos = value; FirePropertyChanged(); }
        }

        public bool Servo_IsCalibrated
        {
            get { return _servo_IsCalibrated; }
            set { _servo_IsCalibrated = value; FirePropertyChanged(); }
        }

        public bool Servo_IgnoreJitter
        {
            get { return _servo_IgnoreJitter; }
            set { _servo_IgnoreJitter = value; FirePropertyChanged(); }
        }

        public int Servo_EaseIn
        {
            get { return _servo_EaseIn; }
            set { _servo_EaseIn = value; FirePropertyChanged(); }
        }

        public int Servo_EaseOut
        {
            get { return _servo_EaseOut; }
            set { _servo_EaseOut = value; FirePropertyChanged(); }
        }

        public int Servo_Speed
        {
            get { return _servo_Speed; }
            set { _servo_Speed = value; FirePropertyChanged(); }
        }

        public bool Servo_MoveSync
        {
            get { return _servo_MoveSync; }
            set { _servo_MoveSync = value; FirePropertyChanged(); }
        }

        void Servo_On()
        {
            _arm.Servos.On();
        }

        void Servo_Off()
        {
            _arm.Servos.Off();
        }

        void Servo_Move()
        {
            var pose = new Pose(Servos.Select(i => i.Value));
            _arm.Servos.Move(pose, Servo_Speed, Servo_EaseIn, Servo_EaseOut, Servo_MoveSync);
        }

        void Servo_MoveStop()
        {
            _arm.Servos.StopMove();
        }

        public class TestingServoVM : BaseVM, IModule
        {
            Servo _servo;
            TestingVM _parent;
            bool _on;
            float _position;
            float _value;
            public ICommand SetPosCommand { get; }
            public ICommand MoveCommand { get; }

            public TestingServoVM(Servo servo, TestingVM parent)
            {
                //set fields
                _servo = servo;
                _parent = parent;
                SetPosCommand = new RelayCommand(SetPos);
                MoveCommand = new RelayCommand(Move);
            }

            void SetPos()
            {
                if (_parent.Servo_IsCalibrated)
                    _servo.SetPosition(Value);
                else
                    _servo.SetPosition(Value + 90);
            }

            void Move()
            {
                _servo.Move(Value, _parent.Servo_Speed, _parent.Servo_EaseIn, _parent.Servo_EaseOut);
            }

            public void ShowingModule()
            {
                UpdateOn(_servo.IsOn);
                _servo.PositionChanged += PositionChanged;
                _servo.OnChanged += OnChanged;
            }

            public void HidingModule()
            {
                _servo.PositionChanged -= PositionChanged;
                _servo.OnChanged -= OnChanged;
            }

            private void PositionChanged(object sender, DataEventArg<ServoPositionChangedResponse> e)
            {
                //validate
                if (_parent.Servo_IgnoreJitter && e.Data.IsVibrating)
                    return;

                if (SwitchToMainThread(sender, e)) return;

                if (_parent.Servo_IsCalibrated)
                    Position = e.Data.Position;
                else
                    Position = e.Data.Position.Map(150, 550, -90, 90);
            }

            private void OnChanged(object sender, DataEventArg<ServoOnChanged> e)
            {
                if (SwitchToMainThread(sender, e)) return;

                UpdateOn(e.Data.IsOn);
            }

            public float Position
            {
                get { return _position; }
                set { _position = value; FirePropertyChanged(); }
            }

            public float Value
            {
                get { return _value; }
                set
                {
                    //validate
                    if (_value == value)
                        return;

                    _value = value;
                    FirePropertyChanged();

                    if (_parent.Servo_SyncPos)
                        SetPos();
                }
            }

            public bool On
            {
                get { return _on; }
                set
                {
                    //validate
                    if (_on == value)
                        return;

                    _on = value;
                    if (_on)
                        _servo.On();
                    else
                        _servo.Off();

                    FirePropertyChanged();
                }
            }

            void UpdateOn(bool on)
            {
                _on = on;
                FirePropertyChanged("On");
            }

        }

        #endregion
    }
}
