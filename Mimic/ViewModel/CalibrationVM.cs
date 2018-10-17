using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using zArm.Api;
using zArm.Api.Behaviors;
using zArm.Api.Windows;
using zArm.Behaviors;
using zArm.Simulation;
using zArm.Simulation.Emulator;
using zArm.Simulation.Enviorment;

namespace Mimic.ViewModel
{
    class CalibrationVM : BaseVM, IModule
    {
        public const int CalibrationCompleteStage = 1;

        int _calibrationState;
        TaskVM _activeTask;
        Step _activeStep;
        StepVM _selectedStep;
        Sim _sim;
        CalibrationSim _calSim;
		bool _armResetNeeded;

		public ICommand VerifyHardwareCommand { get; }
        public ICommand CalibrateServosCommand { get; }

        public CalibrationVM()
        {
            //set fields
            VerifyHardwareCommand = new RelayCommand(VerifyHardware);
            CalibrateServosCommand = new RelayCommand(CalibrateServos);
            _calSim = new CalibrationSim();
            _calSim.HasStarted += Sim_HasStarted;
            _calSim.HasStopped += Sim_HasStopped;
        }

        private async void Sim_HasStarted(object sender, EventArgs e)
        {
            //sync hardware arm with simulator
            var comm = new EmulatorCommunication(_calSim.SimArm, null);
            var simArm = new Arm(comm, comm.Arm.Settings.Values.ActiveServos.GetValueOrDefault(5));
            await simArm.LoadSettingsAsync(); //load setting
            App.Instance.Arm.Behaviors.Add(new SyncronizeCommands(simArm) { SuppressSync = true });
        }

        private void Sim_HasStopped(object sender, EventArgs e)
        {
            //stop sync hardware arm with simulator
            App.Instance.Arm?.Behaviors.Remove<SyncronizeCommands>();
        }

        public int CalibrationState
        {
            get { return _calibrationState; }
            set { _calibrationState = value; FirePropertyChanged(); }
        }

        public TaskVM ActiveTask
        {
            get { return _activeTask; }
            set { _activeTask = value; FirePropertyChanged(); }
        }

        public Step ActiveStep
        {
            get { return _activeStep; }
            set { _activeStep = value; FirePropertyChanged(); }
        }

        public StepVM SelectedStep
        {
            get { return _selectedStep; }
            set { _selectedStep = value; FirePropertyChanged(); }
        }

        public Sim Sim
        {
            get { return _sim; }
            set { _sim = value; FirePropertyChanged(); }
        }

        void IModule.ShowingModule()
        {
            App.Instance.Arm.Behaviors.SuspendAll = true; //suspend behaviors
            CalibrationState = App.Instance.Arm.Settings.Calibrated.GetValueOrDefault();
            Sim = _calSim; //load the sim
            ActiveTask = null; //reset
        }

        void IModule.HidingModule()
        {
            App.Instance.Arm.SoftReset(); //reset the arm after calibration changes
            App.Instance.Arm.Behaviors.SuspendAll = false; //unsuspend behaviors
            Sim = null; //unload the sim

			//reset arm settings
			if (_armResetNeeded)
			{
				_armResetNeeded = false;
				App.Instance.Connection.ArmListener.ResetCurrentArm();
			}
		}

		string SerializeState<T>(T obj)
		{
			if (obj == null)
				return null;
			return JsonConvert.SerializeObject(obj, new JsonSerializerSettings() { Error = (e, i) => i.ErrorContext.Handled = true });
		}

		T DeserializeState<T>(string obj)
		{
			if (string.IsNullOrWhiteSpace(obj))
				return default(T);
			return JsonConvert.DeserializeObject<T>(obj, new JsonSerializerSettings() { Error = (e, i) => i.ErrorContext.Handled = true });
		}

        void VerifyHardware()
        {
			//load previous state
			VerifyHardwareState state = null;
			try { state = DeserializeState<VerifyHardwareState>(Properties.Settings.Default.VerifyHardwareState); }
			catch { }
            var workflow = new VerifyHardware(state);

			//start workflow
            StartWorkflow(workflow, "Test Hardware", i=>
            {
				//save current state for next time
				Properties.Settings.Default.VerifyHardwareState = SerializeState(workflow.GetCurrentState());
				Properties.Settings.Default.Save();

				//reset arm
				if (workflow.Result == StepResult.Passed)
					_armResetNeeded = true;
			});
        }

        void CalibrateServos()
        {
			//load previous state
			ServoCalibratorState state = null;
			try { state = DeserializeState<ServoCalibratorState>(Properties.Settings.Default.ServoCalibratorState); }
			catch { }
			var workflow = new ServoCalibrator(state);

			//start workflow
			StartWorkflow(workflow, "Servo Calibration", i =>
			{
				//save current state for next time
				Properties.Settings.Default.ServoCalibratorState = SerializeState(workflow.GetCurrentState());
				Properties.Settings.Default.Save();

				//reset arm
				if (workflow.Result == StepResult.Passed)
					_armResetNeeded = true;
			});
		}

        void StartWorkflow(StepWorkflow workflow, string name, Action<StepWorkflow> shutdown = null)
        {
            //setup task
            workflow.StepChanged += Workflow_StepChanged;
            ActiveStep = null;
            ActiveTask = new TaskVM(workflow, name, shutdown);

            //start workflow
            App.Instance.Arm.Behaviors.Add(workflow);
        }

        private void Workflow_StepChanged(object sender, StepChangeEventArgs e)
        {
            if (SwitchToMainThread(sender, e))
                return;

            //end task
            if (e.Step == null)
            {
                //update bound CalibrationState
                CalibrationState = App.Instance.Arm.Settings.Calibrated.GetValueOrDefault();

                //reset special animations
                _calSim.FocusOnArm();
                _calSim.VerticalPostion();

                //shutdown activity
                ActiveTask.Shutdown?.Invoke(ActiveTask.Workflow);

                //back to task selection
                ActiveTask = null;
                return;
            }

            //refresh results
            if (e.UpdateResult)
            {
                //stop special animations
                switch (e.Step.Key)
                {
                    case "Led":
                        var sc = App.Instance.Arm.Behaviors.Get<SyncronizeCommands>();
                        if (sc != null)
                        {
                            sc.SuppressSync = true; //disable syncronize with sim
                            sc.SyncArm.Led.Off();
                        }
                        break;
                    case "Knob":
                        _calSim.FlashKnob(false);
                        break;
                    case "Button":
                        _calSim.FlashButton(false);
                        break;
                    case "ShoulderFeedback":
                    case "ShoulderMotor":
                        _calSim.StopSwingServo(_calSim.SimArm.Shoulder);
                        break;
                    case "UpperArmFeedback":
                    case "UpperArmMotor":
                        _calSim.StopSwingServo(_calSim.SimArm.UpperArm);
                        break;
                    case "ForeArmFeedback":
                    case "ForeArmMotor":
                        _calSim.StopSwingServo(_calSim.SimArm.ForeArm);
                        break;
                    case "HandFeedback":
                    case "HandMotor":
                        _calSim.StopSwingServo(_calSim.SimArm.Hand);
                        break;
                    case "FingerFeedback":
                    case "FingerMotor":
                        _calSim.StopSwingServo(_calSim.SimArm.Finger);
                        break;
                    case "ShoulderRange":
                        _calSim.StopRangeServo(_calSim.SimArm.Shoulder);
                        break;
                    case "UpperArmRange":
                        _calSim.StopRangeServo(_calSim.SimArm.UpperArm);
                        break;
                    case "ForeArmRange":
                        _calSim.StopRangeServo(_calSim.SimArm.ForeArm);
                        break;
                    case "HandRange":
                        _calSim.StopRangeServo(_calSim.SimArm.Hand);
                        break;
                    case "FingerRange":
                        _calSim.StopRangeServo(_calSim.SimArm.Finger, true);
                        _calSim.FocusOnArm();
                        break;
                    case "FingerRangeLight":
                        _calSim.StopRangeServo(_calSim.SimArm.Finger, true, true);
                        _calSim.FocusOnArm();
                        break;
                    case "VerticalPositionPower":
                        _calSim.FlashPower(false);
                        break;
                    case "BaseHold":
                        _calSim.FlashBase(false);
                        break;
                    case "UpperArmHold":
                        _calSim.FlashUpperArm(false);
                        break;
                    case "ForeArmHold":
                        _calSim.FlashForeArm(false);
                        break;
                    case "ShoulderProfile":
                    case "UpperArmProfile":
                    case "ForeArmProfile":
                    case "HandProfile":
                    case "FingerProfile":
                    case "ShoulderSpeed":
                    case "UpperArmSpeed":
                    case "ForeArmSpeed":
                    case "HandSpeed":
                    case "FingerSpeed":
                        _calSim.FreezeCameraCentering(false);
                        var sc2 = App.Instance.Arm.Behaviors.Get<SyncronizeCommands>();
                        if (sc2 != null)
                            sc2.SuppressSync = true; //disable syncronize with sim
                        _calSim.VerticalPostion(false);
                        break;
                    case "Yoga1":
                    case "Yoga2":
                    case "Yoga3":
                    case "Yoga4":
                    case "Yoga5":
                    case "Yoga6":
                    case "Yoga7":
                    case "Yoga8":
                    case "Yoga9":
                    case "Yoga10":
                    case "Yoga11":
                    case "Yoga12":
                    case "Yoga13":
                    case "Yoga14":
                    case "Yoga15":
                    case "Yoga16":
                    case "Yoga17":
                    case "Yoga18":
                    case "Yoga19":
                    case "Yoga20":
                        _calSim.StopPose();
                        _calSim.FlashKnob(false);
                        _calSim.FocusOnArm();
                        break;
                }

                ActiveTask.Steps.FirstOrDefault(i => i.Key == e.Step.Key)?.UpdateResult();
                return;
            }

            //start special animations
            switch (e.Step.Key)
            {
                case "Led":
                    _calSim.FocusOnKnob();
                    var sc = App.Instance.Arm.Behaviors.Get<SyncronizeCommands>();
                    if (sc != null)
                        sc.SuppressSync = false; //syncronize with sim
                    break;
                case "Knob":
                    _calSim.FlashKnob(true);
                    break;
                case "Button":
                    _calSim.FlashButton(true);
                    break;
                case "VerticalPosition":
                    _calSim.FocusOnArm();
                    _calSim.VerticalPostion(true);
                    break;
                case "ShoulderFeedback":
                case "ShoulderMotor":
                    _calSim.SwingServo(_calSim.SimArm.Shoulder);
                    break;
                case "UpperArmFeedback":
                case "UpperArmMotor":
                    _calSim.SwingServo(_calSim.SimArm.UpperArm);
                    break;
                case "ForeArmFeedback":
                case "ForeArmMotor":
                    _calSim.SwingServo(_calSim.SimArm.ForeArm);
                    break;
                case "HandFeedback":
                case "HandMotor":
                    _calSim.SwingServo(_calSim.SimArm.Hand);
                    break;
                case "FingerFeedback":
                case "FingerMotor":
                    _calSim.SwingServo(_calSim.SimArm.Finger);
                    break;
                case "ShoulderRange":
                    _calSim.RangeServo(_calSim.SimArm.Shoulder);
                    break;
                case "UpperArmRange":
                    _calSim.RangeServo(_calSim.SimArm.UpperArm);
                    break;
                case "UpperArmRangeHold":
                    _calSim.RangeServo(_calSim.SimArm.UpperArm);
                    _calSim.RangeServo(_calSim.SimArm.UpperArm);
                    break;
                case "ForeArmRange":
                    _calSim.RangeServo(_calSim.SimArm.ForeArm);
                    break;
                case "HandRange":
                    _calSim.RangeServo(_calSim.SimArm.Hand);
                    break;
                case "FingerRange":
                    _calSim.RangeServo(_calSim.SimArm.Finger, true);
                    _calSim.FocusOnHand();
                    break;
                case "FingerRangeLight":
                    _calSim.RangeServo(_calSim.SimArm.Finger, true, true);
                    _calSim.FocusOnHand();
                    break;
                case "VerticalPositionPower":
                    _calSim.VerticalPostion(true);
                    _calSim.FlashPower(true);
                    break;
                case "BaseHold":
                    _calSim.FlashBase(true);
                    break;
                case "UpperArmHold":
                    _calSim.FlashUpperArm(true);
                    break;
                case "ForeArmHold":
                    _calSim.FlashForeArm(true);
                    break;
                case "ShoulderProfile":
                case "UpperArmProfile":
                case "ForeArmProfile":
                case "HandProfile":
                case "FingerProfile":
                case "ShoulderSpeed":
                case "UpperArmSpeed":
                case "ForeArmSpeed":
                case "HandSpeed":
                case "FingerSpeed":
                    _calSim.FreezeCameraCentering(true);
                    var sc2 = App.Instance.Arm.Behaviors.Get<SyncronizeCommands>();
                    if (sc2 != null)
                        sc2.SuppressSync = false; //syncronize with sim
                    break;
                case "VerticalPositionClosed":
                    _calSim.FocusOnArm();
                    _calSim.VerticalPostion(false);
                    break;
                case "Yoga1": ShowYogaPose(0); break;
                case "Yoga2": ShowYogaPose(1); break;
                case "Yoga3": ShowYogaPose(2); break;
                case "Yoga4": ShowYogaPose(3); break;
                case "Yoga5": ShowYogaPose(4); break;
                case "Yoga6": ShowYogaPose(5); break;
                case "Yoga7": ShowYogaPose(6); break;
                case "Yoga8": ShowYogaPose(7); break;
                case "Yoga9": ShowYogaPose(8); break;
                case "Yoga10": ShowYogaPose(9); break;
                case "Yoga11": ShowYogaPose(10); break;
                case "Yoga12": ShowYogaPose(11); break;
                case "Yoga13": ShowYogaPose(12); break;
                case "Yoga14": ShowYogaPose(13); break;
                case "Yoga15": ShowYogaPose(14); break;
                case "Yoga16": ShowYogaPose(15); break;
                case "Yoga17": ShowYogaPose(16); break;
                case "Yoga18": ShowYogaPose(17); break;
                case "Yoga19": ShowYogaPose(18); break;
                case "Yoga20": ShowYogaPose(19); break;
            }

            //display message from step
            ActiveStep = e.Step;

            //hilight step
            if (ActiveTask != null)
                foreach (var step in ActiveTask.Steps)
                {
                    if (step.Hilight(e.Step))
                        SelectedStep = step;
                }
        }

        void ShowYogaPose(int yogaPoseIndex)
        {
            var caliberator = App.Instance.Arm.Behaviors.Get<ServoCalibrator>();
            if (caliberator != null)
            {
                var yogaPose = caliberator.YogaPoses[yogaPoseIndex];
                _calSim.SetPose(yogaPose.Pose.Servos, yogaPose.ServoIndexHilight);

                if (yogaPose.ServoIndex == 0)
                    _calSim.FocusOnArmTop();
                else if (yogaPose.ServoIndex == 4)
                    _calSim.FocusOnHand();
                else
                    _calSim.FocusOnArmSide();
            }
            _calSim.FlashKnob(true);
        }

        


        internal class TaskVM
        {
            public StepWorkflow Workflow { get; }
            public string Name { get; }
            public ObservableCollection<StepVM> Steps { get; }
            public Action<StepWorkflow> Shutdown;

            public TaskVM(StepWorkflow workflow, string name, Action<StepWorkflow> shutdown)
            {
                //set fields
                Workflow = workflow;
                Steps = new ObservableCollection<StepVM>(workflow.Steps.Where(i=>i.ShowStep).Select(i => new StepVM(i)));
                Name = name;
                Shutdown = shutdown;
            }
        }

        internal class StepVM : BaseVM
        {
            Step _step;
            string _resultMessage;
            StepResult _result;
            Brush _color;
            Brush _backColor;

            public string Key { get; }
            public string Name { get; }

            public StepVM(Step step)
            {
                //set fields
                _step = step;
                Key = _step.Key;
                Name = _step.Name;

                UpdateResult();
            }

            public string ResultMessage
            {
                get { return _resultMessage; }
                set { _resultMessage = value; FirePropertyChanged(); }
            }

            public void UpdateResult()
            {
                Result = _step.Result;
                ResultMessage = _step.ResultMessage;
            }

            public StepResult Result
            {
                get { return _result; }
                set
                {
                    _result = value;
                    FirePropertyChanged();
                    switch (_result)
                    {
                        case StepResult.Passed: Color = ModuleColors.Green; break;
                        case StepResult.Failed: Color = ModuleColors.Red; break;
                        case StepResult.Skipped: Color = Brushes.DarkKhaki; break;
                        default: Color = Brushes.DarkGray; break;
                    }
                    BackColor = Brushes.Transparent;
                }
            }

            public bool Hilight(Step step)
            {
                if (step == _step)
                {
                    BackColor = Brushes.Transparent;
                    Color = Brushes.DarkOrange;
                    return true;
                }
                else
                    Result = Result; //set to default colors
                return false;
            }

            public Brush Color
            {
                get { return _color; }
                set { _color = value; FirePropertyChanged(); }
            }

            public Brush BackColor
            {
                get { return _backColor; }
                set { _backColor = value; FirePropertyChanged(); }
            }
        }

    }
}
