using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zArm.Api;
using zArm.Api.Specialized;
using zArm.Behaviors;
using zArm.Simulation;
using zArm.Simulation.Emulator;
using zArm.Simulation.Enviorment;

namespace Mimic.ViewModel
{
	class RecordAndPlaybackVM : BaseVM, IModule
	{
		int _playbackSpeed;
		bool _isPlaying;
		bool _isRecording;
		Sim _sim;
		MotionStudioSim _motionSim;
		IKSim _ikSim;
		SimpleRecordAndPlayback _recordAndPlayback;

		public RelayCommand PlayToggleCommand { get; }
		public RelayCommand RecordToggleCommand { get; }

		public RecordAndPlaybackVM()
		{
			//set fields
			_motionSim = new MotionStudioSim();
			_motionSim.HasStarted += Sim_HasStarted;
			_motionSim.HasStopped += Sim_HasStopped;
			_recordAndPlayback = new SimpleRecordAndPlayback();
			_recordAndPlayback.StateChanged += RecordAndPlayback_StateChanged;
			_recordAndPlayback.PlaybackSpeedChanged += RecordAndPlayback_PlaybackSpeedChanged;
			PlaybackSpeed = _recordAndPlayback.PlaybackSpeed;

			//setup commands
			PlayToggleCommand = new RelayCommand(PlayToggle) { Enabled = false };
			RecordToggleCommand = new RelayCommand(RecordToggle);

		}

		void RecordAndPlayback_PlaybackSpeedChanged(object sender, int e)
		{
			//insure main thread
			if (SwitchToMainThread(sender, e))
				return;

			//update UI
			if (e != _playbackSpeed)
			{
				_playbackSpeed = e;
				FirePropertyChanged("PlaybackSpeed");
			}
		}

		void RecordAndPlayback_StateChanged(object sender, RecordAndPlaybackState e)
		{
			//insure main thread
			if (SwitchToMainThread(sender, e))
				return;

			//enabled commands
			RecordToggleCommand.Enabled = e != RecordAndPlaybackState.Playing;
			PlayToggleCommand.Enabled = e != RecordAndPlaybackState.Recording;
			IsRecording = e == RecordAndPlaybackState.Recording;
			IsPlaying = e == RecordAndPlaybackState.Playing;
		}

		private void RecordToggle()
		{
			//stop recording
			if (IsRecording)
				_recordAndPlayback.Stop();

			//start recording
			else
				_recordAndPlayback.Record();
		}

		private void PlayToggle()
		{
			//stop playing
			if (IsPlaying)
				_recordAndPlayback.Stop();

			//start playing
			else
				_recordAndPlayback.Play();
		}

		void IModule.ShowingModule()
		{
			Sim = _motionSim; //load the sim

			//add behaviors
			App.Instance.Arm?.Behaviors.Add(_recordAndPlayback);
		}

		void IModule.HidingModule()
		{
			Sim = null; //unload the sim

			//stop recording and playback
			_recordAndPlayback.Stop();

			//remove behavior
			App.Instance.Arm?.Behaviors.Remove(_recordAndPlayback);
		}

		private async void Sim_HasStarted(object sender, EventArgs e)
		{
			//create simulated arm;
			var arm = App.Instance.Arm;
			var comm = new EmulatorCommunication(_motionSim.SimArm, null, arm.Settings); //use real robots current settings
			var simArm = new Arm(comm, comm.Arm.Settings.Values.ActiveServos.GetValueOrDefault(5));
			await simArm.LoadSettingsAsync(); //load setting

			//sync arm with simulator
			arm.Behaviors.Add(new SyncronizeWithSimulator(simArm));

			//setup IK in simulator
			_ikSim = new IKSim(_motionSim.TransformGimbal, _motionSim.SimArm, new KinematicsUI(arm));
		}

		void Sim_HasStopped(object sender, EventArgs e)
		{
			//stop sync arm with simulator
			App.Instance.Arm?.Behaviors.Remove<SyncronizeWithSimulator>();

			//shutdown IK behavior in simulator
			_ikSim.Dispose();
			_ikSim = null;
		}

		public int PlaybackSpeed
		{
			get { return _playbackSpeed; }
			set
			{
				_playbackSpeed = value;
				FirePropertyChanged();

				//change from databinding
				if (_recordAndPlayback.PlaybackSpeed != _playbackSpeed)
					_recordAndPlayback.PlaybackSpeed = _playbackSpeed;

			}
		}

		public bool IsPlaying
		{
			get { return _isPlaying; }
			set { _isPlaying = value; FirePropertyChanged(); }
		}

		public bool IsRecording
		{
			get { return _isRecording; }
			set { _isRecording = value; FirePropertyChanged(); }
		}

		public Sim Sim
		{
			get { return _sim; }
			set { _sim = value; FirePropertyChanged(); }
		}
	}
}
