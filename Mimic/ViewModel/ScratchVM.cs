using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using zArm.Behaviors;

namespace Mimic.ViewModel
{
    class ScratchVM : BaseVM, IModule
    {
        string _instructions;
        bool _inError;
        ScratchCommunicator _comm;

        public ICommand OpenNewCommand { get; }
		public MainWindowVM Main { get => App.Instance; }

		public ScratchVM()
        {
            //set fields
            OpenNewCommand = new RelayCommand(OpenNew);
        }

        void IModule.ShowingModule()
        {
			//is scratch comm already running?
			var comm = App.Instance.Arm.Behaviors.Get<ScratchCommunicator>();
			if (comm != null)
			{
				_comm = comm;
			}

			//enable scratch behavior
			else
			{
				var ik = new KinematicsUI(App.Instance.Arm);
				ScratchSpeech speech = null;
				try
				{
					speech = new ScratchSpeech(); //can throw exceptions
				}
				catch { }
				_comm = App.Instance.Arm.Behaviors.GetOrAdd(() => new ScratchCommunicator(Properties.Settings.Default.ScratchVersion, App.Instance.Storage, ik, speech));
				if (_comm.IsListening)
				{
					Instructions = "Scratch integration enabled.  open scratch and start coding";
					InError = false;
				}
				else
				{
					Instructions = "Failed to enable Scratch integration";
					InError = true;
				}
			}
        }

        void IModule.HidingModule()
        {
			//disable scratch behavior
			if (!Main.Settings.RunScratchInBackground)
			{
				App.Instance.Arm.Behaviors.Remove<ScratchCommunicator>();
				_comm = null;
			}
        }

        void OpenNew()
        {
            Process.Start(_comm.GetUrl());
        }

        public string Instructions
        {
            get { return _instructions; }
            set { _instructions = value; FirePropertyChanged(); }
        }

        public bool InError
        {
            get { return _inError; }
            set { _inError = value; FirePropertyChanged(); }
        }
    }



	class ScratchSpeech : IScratchSpeech
	{
		SpeechRecognitionEngine _recognizer;
		SpeechSynthesizer _synth;

		public event EventHandler SpeakingCompleted;
		public event EventHandler<string> Recognized;

		public ScratchSpeech()
		{
			//set fields
			_synth = new SpeechSynthesizer();
			_synth.SetOutputToDefaultAudioDevice();
			_synth.SpeakCompleted += Synth_SpeakCompleted;
			_recognizer = new SpeechRecognitionEngine();
			_recognizer.SetInputToDefaultAudioDevice();
			_recognizer.SpeechRecognized += Recognizer_SpeechRecognized;

		}

		private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
		{
			Recognized?.Invoke(this, e.Result.Text);
		}

		private void Synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
		{
			SpeakingCompleted?.Invoke(this, EventArgs.Empty);
		}

		public void Speak(string text)
		{
			_synth.SpeakAsync(text);
		}

		public void StopSpeaking()
		{
			_synth.SpeakAsyncCancelAll();
		}

		public void SetVoice(bool male)
		{
			_synth.SelectVoiceByHints(male ? VoiceGender.Male : VoiceGender.Female);
		}

		public void ListenFor(bool oneTime, params string[] choices)
		{
			_recognizer.LoadGrammar(new Grammar(new GrammarBuilder(new Choices(choices))));
			_recognizer.RecognizeAsync(oneTime ? RecognizeMode.Single : RecognizeMode.Multiple);
		}

		public void StopListening()
		{
			_recognizer.RecognizeAsyncCancel();
		}
	}
}
