using GongSolutions.Wpf.DragDrop;
using MahApps.Metro.Controls.Dialogs;
using Mimic.Behaviors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using zArm.Api;
using zArm.Api.Specialized;
using zArm.Api.Windows;
using zArm.Behaviors;
using zArm.Simulation;
using zArm.Simulation.Emulator;
using zArm.Simulation.Enviorment;
using System.Windows.Documents;
using Mimic.Adorners;
using System.Reactive.Subjects;
using zArm.Simulation.Components;
using zArm.Simulation.Entities;

namespace Mimic.ViewModel
{
    class MotionStudioVM : BaseVM, IModule, IDropTarget
    {
        Sim _sim;
        MotionStudioSim _motionSim;
		IKSim _ikSim;
		RecordAndPlayback _recordAndPlayback;
        RecordingType? _selectedRecordingType;
        string _instruction;
        RecordingFolderVM _recordings;
        RecordingFolderVM _selectedRecordingFolder;
        RecordingNodeVM _selectedRecording;
        List<RecordingNodeVM> _selectedRecordings;
        RecordAndPlaybackState _state;
        PlaybackControl _playbackControl;
        IDisposable _playbackPositionEvent;
        bool _isPlaying;
        bool _canPlayback;
        float _playbackPosition;
        StorageUI _storage;
        event Action<float> _scrubbing;
        IDisposable _scrubbingThrottled;
        RecordingEditVM _recordingEdit;
        Trail _trailSelected;
        Trail _trailUnselected;
        bool _keepServosOn;
		PositionVM _replaceRecording;
		bool _replaceRecordingStarted;

		public ICommand BeginRecordingCommand { get; }
        public ICommand RecordingDoubleClickCommand { get; }
        public ICommand StopRecordingCommand { get; }
        public ICommand CancelRecordingCommand { get; }
        public ICommand PlayToggleCommand { get; }
        public ICommand SelectionChangedCommand { get; }
        public RelayCommand BackFolderCommand { get; }
        public RelayCommand RemoveCommand { get; }
        public RelayCommand NewFolderCommand { get; }
        public RelayCommand RenameCommand { get; }
		public ICommand RenameAttemptCommand { get; }
		public RelayCommand DeleteCommand { get; }
        public RelayCommand CopyCommand { get; }

		public MotionStudioVM()
        {
			//set fields
			_storage = App.Instance.Storage;
            _motionSim = new MotionStudioSim();
            _motionSim.HasStarted += Sim_HasStarted;
            _motionSim.HasStopped += Sim_HasStopped;
            BeginRecordingCommand = new RelayCommand<RecordingType>(BeginRecording);
            StopRecordingCommand = new RelayCommand(StopRecording);
            CancelRecordingCommand = new RelayCommand(CancelRecording);
            PlayToggleCommand = new RelayCommandAsync(PlayToggle);
            _recordings = new RecordingFolderVM() { Name = string.Empty };
            _selectedRecordingFolder = _recordings;
            RecordingDoubleClickCommand = new RelayCommand<MouseButtonEventArgs>(RecordingDoubleClick);
            SelectionChangedCommand = new RelayCommand<SelectionChangedEventArgs>(SelectionChanged);
            _selectedRecordings = new List<RecordingNodeVM>();
            BackFolderCommand = new RelayCommand(BackFolder) { Enabled = false };
            RemoveCommand = new RelayCommand(Remove) { Enabled = false };
            NewFolderCommand = new RelayCommand(NewFolder);
            RenameCommand = new RelayCommand(Rename) { Enabled = false };
			RenameAttemptCommand = new RelayCommand<EditableTextBlock.TextChangingEventArgs>(RenameAttempt);
			DeleteCommand = new RelayCommand(Delete) { Enabled = false };
            CopyCommand = new RelayCommand(Copy) { Enabled = false };
			_trailSelected = new Trail() { R = ModuleColors.Orange.Color.R, G = ModuleColors.Orange.Color.G, B = ModuleColors.Orange.Color.B, A = 220, Width = .2f,
				Decoration = new TrailDecoration() { R = ModuleColors.Orange.Color.R, G = ModuleColors.Orange.Color.G, B = ModuleColors.Orange.Color.B, A = 220, Width = .2f } };
            _trailUnselected = new Trail() { R = 200, G = 200, B = 200, A = 70, Width = .1f,
				Decoration = new TrailDecoration() { R = ModuleColors.Blue.Color.R, G = ModuleColors.Blue.Color.G, B = ModuleColors.Blue.Color.B, A = 125, Width = .1f }
			};
			_scrubbingThrottled = Observable.FromEvent((Action<float> i) => _scrubbing += i, (Action<float> i) => _scrubbing -= i)
                    .Sample(TimeSpan.FromSeconds(.2))
                    .Subscribe(i => ScrubToPosition(i));

            RecordAndPlayback_StateChanged(null, new RecordAndPlayback.StateChangedEventArgs() { State = RecordAndPlaybackState.Idle }); //set initial state

            Task.Run(new Action(LoadAllRecordings));
        }

        void LoadAllRecordings()
        {
            var recordingInfo = _storage.Recordings.GetAll();

			SetRecordings(recordingInfo.Select(i => new Tuple<Recording, EntityInfo>(_storage.Recordings.Get(i), i)));
        }

        void SetRecordings(IEnumerable<Tuple<Recording,EntityInfo>> recordings)
        {
            if (SwitchToMainThread(recordings))
                return;

            //add to recording collection
            var folders = new Dictionary<string, RecordingFolderVM>();
            foreach (var recording in recordings)
            {
                //get folder
                var folder = _recordings; //root folder
                var folderName = recording.Item2.Path;
                if (!string.IsNullOrWhiteSpace(folderName))
                {
                    if (folders.ContainsKey(folderName))
                        folder = folders[folderName];
                    else
                    {
                        //get folder names
                        var dirNames = folderName.Split(Path.DirectorySeparatorChar);
                        var dirs = dirNames.Clone() as string[];
                        for (int i = 0; i < dirs.Length; i++)
                            if (i > 0)
                                dirs[i] = Path.Combine(dirs[i - 1], dirs[i]);

                        //add the folders
                        for (int i = 0; i < dirs.Length; i++)
                        {
                            var dir = dirs[i];
                            if (!folders.ContainsKey(dir))
                            {
                                var newFolder = new RecordingFolderVM() { Name = dirNames[i] };
								folder.Items.Add(newFolder);
								folder = newFolder;
                                folders.Add(dir, newFolder);
                            }
                            else
                                folder = folders[dir];
                        }
                    }
                }

                //add to folder
                folder.Items.Add(new RecordingVM(recording.Item1) { Name = recording.Item2.Name, ID = recording.Item2.ID, SortOrder = recording.Item2.SortOrder });
            }
		}

        void Remove()
        {
            //validate
            if (SelectedRecording == null || SelectedRecordingFolder == null || SelectedRecordingFolder.Parent == null)
                return;

            //move selected items to parent directory
            var items = _selectedRecordings.ToArray();
            foreach (var item in items)
            {
                var oldInfo = GetEntityInfo(item);

                //move entity
                SelectedRecordingFolder.Items.Remove(item);
                SelectedRecordingFolder.Parent.Items.Add(item);

                //move in storage
                var newInfo = GetEntityInfo(item);
                _storage.Recordings.Move(oldInfo, newInfo);

				//update sortOrder on files
				UpdateFolderSortOrder(SelectedRecordingFolder.Parent);

			}
                
        }

        void Copy()
        {
            //validate
            if (SelectedRecording == null)
                return;

            //make a copy of the selected items
            var items = _selectedRecordings.ToArray();
            foreach (var item in items)
            {
                var clone = item.Clone() as RecordingNodeVM;
                //create copy name
                if (!clone.Name.EndsWith(" - Copy"))
                    clone.Name += " - Copy";
                SelectedRecordingFolder.Items.Add(clone);

                //copy in storage
                StorageCopy(item, clone);
            }

        }

        void StorageCopy(RecordingNodeVM source, RecordingNodeVM clone)
        {
            _storage.Recordings.Copy(GetEntityInfo(source), GetEntityInfo(clone));
            var sourceFolder = source as RecordingFolderVM;
            var cloneFolder = clone as RecordingFolderVM;
            if (sourceFolder != null && cloneFolder != null)
                for (int i = 0; i < sourceFolder.Count; i++)
                    StorageCopy(sourceFolder.Items[i], cloneFolder.Items[i]);
        }

        void NewFolder()
        {
            //create a new folder in the current directory
            _selectedRecordingFolder.Items.Add(new RecordingFolderVM());
        }

        void Rename()
        {
            //validate
            if (SelectedRecording == null)
                return;

			//edit name
			SelectedRecording.IsEditingName = true;
        }

		void RenameAttempt(EditableTextBlock.TextChangingEventArgs e)
		{
			//validate
			if (string.IsNullOrWhiteSpace(e.Text))
			{
				e.Cancel = true;
				return;
			}
			var cleanName = Storage.CleanFileName(e.Text); //clean up the name
			if (string.IsNullOrWhiteSpace(cleanName))
			{
				e.Cancel = true;
				return;
			}
			var recording = e.DataContext as RecordingNodeVM;
			if (recording == null)
			{
				e.Cancel = true;
				return;
			}

			//update the name
			var oldInfo = GetEntityInfo(recording);
			e.Text = cleanName; //rename entity
			recording.Name = cleanName; //rename entity
			var newInfo = GetEntityInfo(recording);
			_storage.Recordings.Move(oldInfo, newInfo); //rename in storage
		}

        void Delete()
        {
            //validate
            if (SelectedRecording == null)
                return;

			var items = _selectedRecordings.ToArray();

			/* TEMP skip confirmation
            //confirm
            if (items.Length == 1)
            {
                if (await App.Instance.ShowMessageAsync("Delete", $"Are you sure you want to delete \"{SelectedRecording.Name}\"?", MessageDialogStyle.AffirmativeAndNegative) != MessageDialogResult.Affirmative)
                    return;
            }
            else
            {
                if (await App.Instance.ShowMessageAsync("Delete", $"Are you sure you want to delete {items.Length} selected items?", MessageDialogStyle.AffirmativeAndNegative) != MessageDialogResult.Affirmative)
                    return;
            }
			*/

			//delete
			foreach (var item in items)
            {
                _storage.Recordings.Delete(GetEntityInfo(item)); //delete in storage
                SelectedRecordingFolder.Items.Remove(item);
            }
        }

        void BackFolder()
        {
            if (SelectedRecordingFolder != null && SelectedRecordingFolder.Parent != null)
                SelectedRecordingFolder = SelectedRecordingFolder.Parent;
        }

        EntityInfo GetEntityInfo(RecordingNodeVM node)
        {
            var path = GetParentFilePath(node.Parent, string.Empty);

            if (node is RecordingFolderVM)
                return new EntityInfo() { Path = Path.Combine(path, node.Name)};
            return new EntityInfo() { Name = node.Name, Path = path, ID = node.ID, SortOrder = node.SortOrder };
        }

        string GetParentFilePath(RecordingFolderVM folder, string path)
        {
            if (folder != null)
            {
                path = Path.Combine(folder.Name, path);
                return GetParentFilePath(folder.Parent, path);
            }
            return path;
        }

        void SelectionChanged(SelectionChangedEventArgs e)
        {
            //sync the list of selected items
            if (e.RemovedItems != null)
                foreach (RecordingNodeVM item in e.RemovedItems)
                    _selectedRecordings.Remove(item);
            if (e.AddedItems != null)
                foreach (RecordingNodeVM item in e.AddedItems)
                    _selectedRecordings.Add(item);
        }

        void RecordingDoubleClick(MouseButtonEventArgs e)
        {
            //validate
            var folder = SelectedRecording as RecordingFolderVM;
            if (folder == null)
                return;

            e.Handled = true;

            //open the folder
            SelectedRecordingFolder = folder;
        }

        void BeginRecording(RecordingType type)
        {
            _recordAndPlayback?.StartRecording(type);
        }

        void StopRecording()
        {
            _recordAndPlayback?.StopRecording(false);
        }

        void CancelRecording()
        {
            _recordAndPlayback?.StopRecording(true);
        }

        async Task PlayToggle()
        {
            //validate
            if (_playbackControl == null)
                return;

            //toggle playing
            if (IsPlaying)
                _playbackControl.Stop();
            else
                await _playbackControl.Play();
        }

        void StartPlayback(RecordingVM recording)
        {
			var recordingData = _storage.Recordings.Get(GetEntityInfo(recording));
			if (recordingData != null)
				_recordAndPlayback?.StartPlayback(recordingData);
        }

        private async void Sim_HasStarted(object sender, EventArgs e)
        {
            //create simulated arm;
            var arm = App.Instance.Arm;
            var comm = new EmulatorCommunication(_motionSim.SimArm, null, arm.Settings); //use real robots current settings
            var simArm = new Arm(comm, comm.Arm.Settings.Values.ActiveServos.GetValueOrDefault(5));
            await simArm.LoadSettingsAsync(); //load setting

			//sync arm with simulator
			var syncSim = new SyncronizeWithSimulator(simArm);
			syncSim.SimulatorPositionSyncronized += SyncSim_SimulatorPositionSyncronized;
			syncSim.ArmPowerChanging += SyncSim_ArmPowerChanging;
			arm.Behaviors.Add(syncSim);


			//setup IK in simulator
			_ikSim = new IKSim(_motionSim.TransformGimbal, _motionSim.SimArm, new KinematicsUI(arm));
        }

		void Sim_HasStopped(object sender, EventArgs e)
        {
            //stop sync arm with simulator
            var syncSim = App.Instance.Arm?.Behaviors.Remove<SyncronizeWithSimulator>();
			syncSim.SimulatorPositionSyncronized -= SyncSim_SimulatorPositionSyncronized;
			syncSim.ArmPowerChanging -= SyncSim_ArmPowerChanging;

			//shutdown IK behavior in simulator
			_ikSim.Dispose();
			_ikSim = null;
		}

        void IModule.ShowingModule()
        {
			Sim = _motionSim; //load the sim

            //enabled record and playback behavior
            _recordAndPlayback = new RecordAndPlayback(new KinematicsUI(App.Instance.Arm));
            _recordAndPlayback.StateChanged += RecordAndPlayback_StateChanged;
			_recordAndPlayback.Scrubbing += RecordAndPlayback_Scrubbing;

			App.Instance.Arm?.Behaviors.Add(_recordAndPlayback);
        }

		void IModule.HidingModule()
        {
            Sim = null; //unload the sim
            App.Instance.Arm?.Behaviors.Remove(_recordAndPlayback);
            _recordAndPlayback.StateChanged -= RecordAndPlayback_StateChanged;
			_recordAndPlayback.Scrubbing -= RecordAndPlayback_Scrubbing;
		}

		void SyncSim_SimulatorPositionSyncronized(object sender, EventArgs e)
		{
			//if the simulator arm actively moving the robot, and the servos are on because of a shuttle,  keep the motors on
			_playbackControl?.DelayTurningShuttleServosOff();
		}

		void SyncSim_ArmPowerChanging(object sender, SyncronizeServoInversePower.PowerChangingEventArgs e)
		{
			//keep the motors on if shuttling is going to turn them off
			e.Handled = _playbackControl?.IsShuttling ?? false;
		}


		void RecordAndPlayback_Scrubbing(object sender, EventArgs e)
		{
			UpdatePlaybackPosition(0);
		}

		void RecordAndPlayback_Scrubbing(object sender, RecordAndPlayback.ScrubbingEventArgs e)
		{
			e.Handled = true;

			if (SwitchToMainThread(sender, e))
				return;

			KnobScrubbing(e);
		}

		async void KnobScrubbing(RecordAndPlayback.ScrubbingEventArgs e)
		{
			//validate
			if (_playbackControl == null || _recordingEdit == null)
				return;

			//get movement
			var position = _playbackControl.GetPlaybackPosition();
			MovementVM currentMovement = null;
			MovementVM prevMovement = null;
			MovementVM nextMovement = null;
			var index = 0;
			foreach (var movement in _recordingEdit.Movements)
			{
				nextMovement = movement;
				if (index > position.CurrentIndex)
					break;
				index += movement.IndexCount;
				prevMovement = currentMovement;
				currentMovement = movement;
			}
			if (currentMovement == null)
				return;
			index -= currentMovement.IndexCount;
			if (nextMovement == currentMovement)
				nextMovement = null;

			//get current focus
			var targetIndex = currentMovement.TakeWhile(i => i != currentMovement.Target).Count() + index;
			var secondaryIndex = currentMovement.TakeWhile(i => i != currentMovement.SecondaryTarget).Count() + index;
			var lastIndex = _playbackControl.Recording.Movements.Length -1;
			var shuttleTo = ShuttleTo.Target;
			if (position.CurrentIndex == targetIndex)
				shuttleTo = position.IndexComplete < 1 ? ShuttleTo.LessThanTarget : ShuttleTo.Target;
			else if (position.CurrentIndex == secondaryIndex)
				shuttleTo = position.IndexComplete < 1 ? ShuttleTo.LessThanTarget : ShuttleTo.SecondaryTarget;
			else if (position.CurrentIndex > targetIndex)
				shuttleTo = ShuttleTo.GreaterThanTarget;
			else if (position.CurrentIndex < targetIndex)
				shuttleTo = ShuttleTo.LessThanTarget;

			//calculate knob change
			if (e.Knob.Amount > 0) //move forward
				shuttleTo += 1;
			else //move back
			{
				if (shuttleTo == ShuttleTo.LessThanTarget && currentMovement.Target != currentMovement.SecondaryTarget && position.CurrentIndex > secondaryIndex)
					shuttleTo += 1;
				else
					shuttleTo -= 1;
			}
			if (shuttleTo == ShuttleTo.LessThanTarget)
				shuttleTo -= 1;
			if (shuttleTo == ShuttleTo.GreaterThanTarget)
				shuttleTo += 1;
			if (shuttleTo == ShuttleTo.SecondaryTarget && currentMovement.SecondaryTarget == currentMovement.Target)
				shuttleTo = (e.Knob.Amount > 0) ? ShuttleTo.Target : ShuttleTo.PreviousMovement;
			if (shuttleTo == ShuttleTo.PreviousMovement && prevMovement == null)
				shuttleTo = ShuttleTo.Start;
			if (shuttleTo == ShuttleTo.NextMovement && nextMovement == null)
				shuttleTo = ShuttleTo.End;

			//shuttle
			var shuttledMovement = currentMovement;
			switch (shuttleTo)
			{
				case ShuttleTo.Start:
					if (!(position.CurrentIndex == 0 && position.IndexComplete == 0))
					{
						await _playbackControl.Shuttle(0);
						shuttledMovement = _recordingEdit.Movements.First();
						UpdatePlaybackPosition(0);
					}
					_recordingEdit.SetUpdatingTarget(null, false);
					break;
				case ShuttleTo.PreviousMovement:
					await _recordingEdit.ShuttleTo(prevMovement, false);
					shuttledMovement = prevMovement;
					break;
				case ShuttleTo.SecondaryTarget:
					await _recordingEdit.ShuttleTo(currentMovement, true);
					break;
				case ShuttleTo.Target:
					await _recordingEdit.ShuttleTo(currentMovement, false);
					break;
				case ShuttleTo.NextMovement:
					await _recordingEdit.ShuttleTo(nextMovement, nextMovement.Target != nextMovement.SecondaryTarget);
					shuttledMovement = nextMovement;
					break;
				case ShuttleTo.End:
					if (!(position.CurrentIndex == lastIndex && position.IndexComplete == 1))
					{
						await _playbackControl.Shuttle(1);
						shuttledMovement = _recordingEdit.Movements.Last();
						UpdatePlaybackPosition(0);
					}
					_recordingEdit.SetUpdatingTarget(null, false);
					break;
			}

			//update selection
			_recordingEdit.Selected = shuttledMovement;
		}

		enum ShuttleTo
		{
			Start,
			PreviousMovement,
			LessThanTarget,
			SecondaryTarget,
			Target,
			GreaterThanTarget,
			NextMovement,
			End
		}

		void RecordAndPlayback_StateChanged(object sender, RecordAndPlayback.StateChangedEventArgs e)
        {
            if (SwitchToMainThread(() => RecordAndPlayback_StateChanged(sender, e)))
                return;

            if (_playbackPositionEvent != null)
            {
                _playbackPositionEvent.Dispose();
                _playbackPositionEvent = null;
			}

            State = e.State;

			//process replacement recording
			if (_replaceRecording != null)
			{
				if (e.State == RecordAndPlaybackState.Idle && _replaceRecordingStarted)
				{
					Instruction = "Select a type of recording or hold your robots button down for 2 seconds";
					IsPlaying = false;
					if (e.Recording != null)
						_replaceRecording.ReplaceRecording(e.Recording);
					_replaceRecording = null;
					_replaceRecordingStarted = false;
				}
				if (e.State == RecordAndPlaybackState.Recording)
				{
					Instruction = "Recording replacement...  " + _recordAndPlayback.RecordingInstructions;
					IsPlaying = false;
					_replaceRecordingStarted = true;
				}
				return; //return early
			}

			switch (e.State)
            {
                case RecordAndPlaybackState.Idle:
                    Instruction = "Select a type of recording or hold your robots button down for 2 seconds";
					SelectedRecordingType = null;
                    IsPlaying = false;
                    break;
                case RecordAndPlaybackState.SelectingRecordingType:
                    Instruction = "Twist your robots knob to select a recording type.  Then press your robots button.";
                    SelectedRecordingType = e.RecordingType;
                    SelectedRecording = null;
                    IsPlaying = false;
                    break;
                case RecordAndPlaybackState.Recording:
                    Instruction = "Recording...  " + _recordAndPlayback.RecordingInstructions;
                    SelectedRecordingType = null;
					SelectedRecording = null;
					IsPlaying = false;
                    break;
                case RecordAndPlaybackState.Playing:
                    Instruction = "Playing...  Press your robots button to stop or twist knob to scrub";
                    SelectedRecordingType = null;
                    IsPlaying = true;
                    _playbackPositionEvent = Observable.Interval(TimeSpan.FromSeconds(.1)).Subscribe(UpdatePlaybackPosition);
                    break;
                case RecordAndPlaybackState.PlayingStopped:
                    Instruction = "Ready to playback recording.  Press your robots button to play or twist knob to scrub";
                    SelectedRecordingType = null;
                    IsPlaying = false;
					UpdatePlaybackPosition(0); //update the position one last time after stopping
					break;
            }

			
			RecordingVM newRecording = null;
			//add recording to the current recordings folder and save
			if (e.Recording != null)
            {
				newRecording = new RecordingVM(e.Recording);
				_selectedRecordingFolder.Items.Add(newRecording);
				_storage.Recordings.Save(e.Recording, GetEntityInfo(newRecording)); //save in storage
				UpdateFolderSortOrder(_selectedRecordingFolder);
			}

			//set playback control
			if (e.PlaybackControl != _playbackControl)
            {
                if (_playbackControl != null)
                {
                    _playbackControl.UpdatedMovementAnalysis -= PlaybackControl_UpdatedMovementAnalysis;
                    if (RecordingEdit != null)
                        RecordingEdit.SelectionChanged -= RecordingEdit_SelectionChanged;
                    RecordingEdit = null;
                    _motionSim.Trails?.SetTrails(null);
                }
                _playbackControl = e.PlaybackControl;
                if (_playbackControl != null)
                {
                    _playbackControl.UpdatedMovementAnalysis += PlaybackControl_UpdatedMovementAnalysis;
					IsPlaying = false;
					SetPlaybackPosition(_playbackControl.GetPlaybackPosition());
                    var recordingInfo = _selectedRecording as RecordingVM;
                    RecordingEdit = new RecordingEditVM(_playbackControl, (i) => SaveRecording(recordingInfo, i), _motionSim, () => UpdatePlaybackPosition(0), ReplaceRecording);
                    RecordingEdit.SelectionChanged += RecordingEdit_SelectionChanged;
                    Task.Run(_playbackControl.CalculateRecording);
                }
                CanPlayback = _playbackControl != null;
            }

			//select the new recording - if created
			if (newRecording != null)
				SelectedRecording = newRecording; 

		}

		void ReplaceRecording(PositionVM replaceRecording)
		{
			//set the replacement recording
			_replaceRecording = replaceRecording;

			//set selection
			RecordingEdit.Selected = replaceRecording;

			//begin a new recording
			BeginRecording(RecordingType.RealTime);
		}

        void PlaybackControl_UpdatedMovementAnalysis(object sender, MovementAnalysis[] e)
        {
            if (SwitchToMainThread(sender, e))
                return;

            //create trails
            var ik = new KinematicsUI(App.Instance.Arm);
            var indexSkip = 0;
            var movements = RecordingEdit.Movements.Select(i => {
                var r = e.Skip(indexSkip).Take(i.IndexCount);
                indexSkip += i.IndexCount;
                return r;
            });
            var selectedIndex = RecordingEdit.Selection.Select(i => RecordingEdit.Movements.IndexOf(i)).ToArray();
            var trails = movements.Select((i, index) => {
				var selected = selectedIndex.Contains(index);
				var t = selected ? _trailSelected :_trailUnselected;
				var movementVM = RecordingEdit.Movements[index];
				return new Trail() { R = t.R, G = t.G, B = t.B, A = t.A, Width = t.Width, Speed = t.Speed, Path = GetPath(i, ik, 100, movementVM).ToArray(), Decoration = movementVM.GetTrailDecoration(t, i, ik) };
            });
            _motionSim.Trails?.SetTrails(trails.Where(i => i.Path.Length != 0).ToArray());
        }

		IEnumerable<TrailPos> GetPath(IEnumerable<MovementAnalysis> movements, IKinematics ik, float interval, MovementVM movementVM)
        {
			int index = 0;
            foreach (var movement in movements)
            {
                //start
                var target = ik.GetTarget(movement.StartPose).Result;
                yield return new TrailPos() { X = target.X, Y = target.Y, Z = target.Z };

                //middle slices
                var incriment = interval / movement.TotalDuration;
                var percent = incriment;
                while (percent < 1)
                {
                    target = ik.GetTarget(movement.GetPose(percent)).Result;
                    yield return new TrailPos() { X = target.X, Y = target.Y, Z = target.Z };
                    percent += incriment;
                }

                //end
                target = ik.GetTarget(movement.EndPose).Result;
                yield return new TrailPos() { X = target.X, Y = target.Y, Z = target.Z };

				//cut off early
				if (movementVM.TrailCutoff(index))
					yield break;
				index++;
            }
        }

		void RecordingEdit_SelectionChanged(object sender, List<MovementVM> e)
        {
            if (SwitchToMainThread(sender, e))
                return;

            //validate
            if (RecordingEdit == null)
                return;

			//unhilight
            for (int i = 0; i < RecordingEdit.Movements.Count; i++)
                if (!e.Contains(RecordingEdit.Movements[i]))
					_motionSim.Trails?.UpdateTrail(i, _trailUnselected);
            //hilight
			for (int i = 0; i < RecordingEdit.Movements.Count; i++)
				if (e.Contains(RecordingEdit.Movements[i]))
					_motionSim.Trails?.UpdateTrail(i, _trailSelected);
		}

        void SaveRecording(RecordingVM recordingInfo, Recording recording)
        {
            //validate
            if (recordingInfo == null || recording == null)
                return;

            _storage.Recordings.Save(recording, GetEntityInfo(recordingInfo)); //save in storage
        }

        void UpdatePlaybackPosition(long interval)
        {
            //validate
            if (_playbackControl == null)
                return;

            if (SwitchToMainThread(interval))
                return;

            SetPlaybackPosition(_playbackControl.GetPlaybackPosition());
        }

        public Sim Sim
        {
            get { return _sim; }
            set { _sim = value; FirePropertyChanged(); }
        }

        public string Instruction
        {
            get { return _instruction; }
            set { _instruction = value; FirePropertyChanged(); }
        }

        public RecordingType? SelectedRecordingType
        {
            get { return _selectedRecordingType; }
            set { _selectedRecordingType = value; FirePropertyChanged(); }
        }

        public RecordAndPlaybackState State
        {
            get { return _state; }
            set { _state = value; FirePropertyChanged(); }
        }

        public bool IsPlaying
        {
            get { return _isPlaying; }
            set { _isPlaying = value; FirePropertyChanged(); }
        }

        public bool CanPlayback
        {
            get { return _canPlayback; }
            set { _canPlayback = value; FirePropertyChanged(); }
        }

        public bool KeepServosOn
        {
            get { return _keepServosOn; }
            set
            {
                _keepServosOn = value;
                if (_keepServosOn)
                    App.Instance.Arm?.Behaviors.GetOrAdd<KeepServosOn>();
                else
                    App.Instance.Arm?.Behaviors.Remove<KeepServosOn>();

                FirePropertyChanged();
            }
        }

        public float PlaybackPosition
        {
            get { return _playbackPosition; }
            set
            {
                _playbackPosition = value;
                FirePropertyChanged();

                //changed through databinding - scrubbing is occuring
                _scrubbing?.Invoke(value);
            }
        }

        void SetPlaybackPosition(PlaybackPosition position)
        {
            _playbackPosition = position.TotalComplete;
            RecordingEdit?.SetCursorPosition(position, IsPlaying);
            FirePropertyChanged("PlaybackPosition");
        }

        async void ScrubToPosition(float value)
        {
            //validate
            if (_playbackControl == null || IsPlaying)
                return;

            //move to position
            await _playbackControl.Shuttle(value);
            RecordingEdit?.SetCursorPosition(_playbackControl.GetPlaybackPosition(), IsPlaying);
        }


        public RecordingFolderVM SelectedRecordingFolder
        {
            get { return _selectedRecordingFolder; }
            set
            {
                _selectedRecordingFolder = value;

                //enable / disable commands
                BackFolderCommand.Enabled = (_selectedRecordingFolder != null && _selectedRecordingFolder.Parent != null); //enable the back folder command

                FirePropertyChanged();
            }
        }

        public RecordingNodeVM SelectedRecording
        {
            get { return _selectedRecording; }
            set
            {
                _selectedRecording = value;

                //enable / disable commands
                RenameCommand.Enabled = _selectedRecording != null;
                RemoveCommand.Enabled = _selectedRecording != null && _selectedRecordingFolder != null && _selectedRecordingFolder.Parent != null;
                DeleteCommand.Enabled = _selectedRecording != null;
                CopyCommand.Enabled = _selectedRecording != null;

                //start playback
                var recording = _selectedRecording as RecordingVM;
                if (recording != null)
                    StartPlayback(recording);

                FirePropertyChanged();
            }
        }

        public RecordingEditVM RecordingEdit
        {
            get { return _recordingEdit; }
            set { _recordingEdit = value; FirePropertyChanged(); }
        }

		void UpdateFolderSortOrder(RecordingFolderVM folder)
		{
			int sortOrder = 0;
			foreach (var item in folder.Items)
			{
				//skip folder
				if (item is RecordingFolderVM)
					continue;

				sortOrder++;
				if (item.SortOrder != sortOrder)
				{
					var oldInfo = GetEntityInfo(item);
					item.SortOrder = sortOrder;
					var newInfo = GetEntityInfo(item);
					_storage.Recordings.Move(oldInfo, newInfo);
				}
			}
		}

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            //validate
            var source = dropInfo.Data as RecordingNodeVM;
			var source2 = dropInfo.Data as MovementVM;
			var sourceList = dropInfo.Data as IList;
            var target = dropInfo.TargetItem as RecordingNodeVM;
            var targetFolder = dropInfo.TargetItem as RecordingFolderVM;
            if (dropInfo.TargetCollection != _selectedRecordingFolder.Items || !dropInfo.IsSameDragDropContextAsSource || (source == null && source2 == null && sourceList == null) || (target == null && targetFolder == null))
                return;

            var sourceContainsTarget = source == target || (sourceList != null && sourceList.Contains(target));

            //add to folder
            if (dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.TargetItemCenter) && !sourceContainsTarget && targetFolder != null && (source != null || (sourceList != null && sourceList.OfType<RecordingNodeVM>().Count() != 0)))
            {
                //dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.DestinationText = targetFolder.Name;
                dropInfo.Effects = DragDropEffects.Copy;
                return;
            }

			//rearange
			if (source != null || (sourceList != null && sourceList.OfType<RecordingNodeVM>().Count() != 0))
			{
				dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
				dropInfo.Effects = DragDropEffects.Move;
			}

			//add movements to recording
			if (dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.TargetItemCenter) && !sourceContainsTarget && target != null && targetFolder == null && (source2 != null || (sourceList != null && sourceList.OfType<MovementVM>().Count() != 0)))
			{
				dropInfo.DestinationText = target.Name;
				dropInfo.Effects = DragDropEffects.Copy;
				return;
			}
		}

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            //validate
            var source = dropInfo.Data as RecordingNodeVM;
			var source2 = dropInfo.Data as MovementVM;
			var sourceList = dropInfo.Data as IList;
            var target = dropInfo.TargetItem as RecordingNodeVM;
            var targetFolder = dropInfo.TargetItem as RecordingFolderVM;
			if (dropInfo.TargetCollection != _selectedRecordingFolder.Items || !dropInfo.IsSameDragDropContextAsSource || (source == null && source2 == null && sourceList == null) || (target == null && targetFolder == null))
				return;

			//add movements to recording
			if ((source2 != null || (sourceList != null && sourceList.OfType<MovementVM>().Count() != 0)) && target != null && targetFolder == null)
			{
				if (target is RecordingVM recording)
				{
					var recordingData = _storage.Recordings.Get(GetEntityInfo(recording));
					if (recordingData == null)
						return;
					if (sourceList != null)
					{
						recordingData.Movements = recordingData.Movements.Concat(sourceList.OfType<MovementVM>().SelectMany(i => i.Clone() as MovementVM)).ToArray();
						SaveRecording(recording, recordingData);
					}
					else
					{
						recordingData.Movements = recordingData.Movements.Concat(source2.Clone() as MovementVM).ToArray();
						SaveRecording(recording, recordingData);
					}
				}
				return;
			}

            //rearange
            if (dropInfo.Effects.HasFlag(DragDropEffects.Move))
            {
                if (sourceList != null)
                {
                    foreach (RecordingNodeVM s in sourceList)
                    {
                        var sIndex = _selectedRecordingFolder.Items.IndexOf(s);
                        var tIndex = dropInfo.InsertIndex > sIndex ? dropInfo.InsertIndex - 1 : dropInfo.InsertIndex;
                        if (sIndex != tIndex)
                            _selectedRecordingFolder.Items.Move(sIndex, tIndex);
                    }
                }
                else
                {
                    var sIndex = _selectedRecordingFolder.Items.IndexOf(source);
                    var tIndex = dropInfo.InsertIndex > sIndex ? dropInfo.InsertIndex - 1 : dropInfo.InsertIndex;
                    if (sIndex != tIndex)
                        _selectedRecordingFolder.Items.Move(sIndex, tIndex);
                }
				//update sortOrder on files
				UpdateFolderSortOrder(_selectedRecordingFolder);

				return;
            }

            //add to folder
            if (dropInfo.Effects.HasFlag(DragDropEffects.Copy) && targetFolder != null)
            {
                //move group
                if (sourceList != null)
                {
                    foreach (RecordingNodeVM s in sourceList)
                    {
                        //remove from old folder
                        var oldInfo = GetEntityInfo(s);
                        if (s.Parent != null)
                            s.Parent.Items.Remove(s);

                        //add to new folder
                        targetFolder.Items.Add(s);

                        //move in source
                        var newInfo = GetEntityInfo(s);
                        _storage.Recordings.Move(oldInfo, newInfo);
                    }

                }

                //move single item
                else
                {
                    //remove from old folder
                    var oldInfo = GetEntityInfo(source);
                    if (source.Parent != null)
                        source.Parent.Items.Remove(source);

                    //add to new folder
                    targetFolder.Items.Add(source);

                    //move in source
                    var newInfo = GetEntityInfo(source);
                    _storage.Recordings.Move(oldInfo, newInfo);
                }

				//update sortOrder on files
				UpdateFolderSortOrder(targetFolder);

				return;
            }
        }
    }




	/// <summary>
	/// enables IK transform gizmo in the simulator
	/// </summary>
	class IKSim : IDisposable
	{
		ITransformGimbal _transformGimbal;
		SimzArmB1 _simArm;
		IKinematics _ik;
		bool _gizmoMoving;
		Pose _initialPose;

		public IKSim(ITransformGimbal transformGimbal, SimzArmB1 simArm, IKinematics ik)
		{
			//set fields
			_transformGimbal = transformGimbal;
			_simArm = simArm;
			_ik = ik;

			//attach events
			_transformGimbal.Grabbed += TransformGimbal_Grabbed;
			_transformGimbal.ChangedPosition += TransformGimbal_ChangedPosition;
			for (int i = 0; i < 4; i++)
				_simArm.ArmSegments[i].Servo.ServoChanged += Servo_ServoChanged;

			//initial setup and position
			_transformGimbal.IsVisible = true;
			var result = _ik.GetTarget(new Pose(_simArm.ArmSegments.Select(i => i.Servo.Position)));
			_transformGimbal.SetPosition(result.Result.X, result.Result.Y, result.Result.Z);
		}

		public void Dispose()
		{
			//detach events
			_transformGimbal.Grabbed -= TransformGimbal_Grabbed;
			_transformGimbal.ChangedPosition -= TransformGimbal_ChangedPosition;
			for (int i = 0; i < 4; i++)
				_simArm.ArmSegments[i].Servo.ServoChanged -= Servo_ServoChanged;

			//disable
			_transformGimbal.IsVisible = false;
		}

		private void TransformGimbal_Grabbed(object sender, GrabbedEventArgs e)
		{
			for (int i = 0; i < 4; i++)
				_simArm.ArmSegments[i].Servo.FireEvent.SetGrabbed(e.IsGrabbed);
			if (e.IsGrabbed)
				_initialPose = new Pose(_simArm.ArmSegments.Select(i => i.Servo.Position));
		}

		void TransformGimbal_ChangedPosition(object sender, TranformGizmoEventArgs e)
		{
			//update servo position using IK
			_gizmoMoving = true;
			var result = _ik.GetPose(new KinematicTarget() { X = e.X, Y = e.Y, Z = e.Z }, _initialPose);
			for (int i = 0; i < 4; i++)
				_simArm.ArmSegments[i].Servo.Position = result.Result[i].GetValueOrDefault();
			_gizmoMoving = false;
		}

		private void Servo_ServoChanged(object sender, ServoChangedEventArgs e)
		{
			//update gizmo position
			if (!_gizmoMoving)
			{
				var result = _ik.GetTarget(new Pose(_simArm.ArmSegments.Select(i => i.Servo.Position)));
				_transformGimbal.SetPosition(result.Result.X, result.Result.Y, result.Result.Z);
			}
		}

	}

	abstract class RecordingNodeVM : BaseVM, ICloneable
    {
        string _name;
        RecordingFolderVM _parent;
        string _fullPath = string.Empty;
		bool _isEditingName;

		public Guid ID { get; set; } = Guid.NewGuid();

		public int SortOrder { get; set; }

        public RecordingFolderVM Parent
        {
            get { return _parent; }
            set
            {
                //validate
                if (_parent == value)
                    return;

                _parent = value;
                OnParentOrNameChanged();
                FirePropertyChanged();
            }
        }

        public string Name
        {
            get { return _name; }
            set
            {
                //validate
                if (_name == value)
                    return;

                _name = value;
                OnParentOrNameChanged();
                FirePropertyChanged();
            }
        }

        public string FullPath
        {
            get { return _fullPath; }
            private set
            {
                //validate
                if (_fullPath == value)
                    return;

                _fullPath = value;
                FirePropertyChanged();
            }
        }

        void OnParentOrNameChanged()
        {
            //replace name with a unique one
            _name = InsureUniqueName(this);

            UpdateFullPath();
        }

        void UpdateFullPath()
        {
            //update full directory path
            if (Parent == null)
                FullPath = Name;
            else
                FullPath = ((Parent.FullPath == string.Empty) ? string.Empty : Parent.FullPath + " > ") + Name;

            //update all child items too
            var folder = this as RecordingFolderVM;
            if (folder != null)
            {
                foreach (var item in folder.Items)
                    item.UpdateFullPath();
            }
        }

        string InsureUniqueName(RecordingNodeVM item)
        {
            if (Parent != null)
            {
                var names = Parent.Items.Where(i => i != item && i.Name.StartsWith(item.Name)).Select(i => i.Name);
                if (names.Contains(item.Name))
                {
                    //rename
                    for (int i = 2; i < 100; i++)
                    {
                        var newName = $"{item.Name} ({i})";
                        if (!names.Contains(newName))
                            return newName;
                    }
                }
            }
            return item.Name;
        }

		public bool IsEditingName
		{
			get { return _isEditingName; }
			set { _isEditingName = value; FirePropertyChanged(); }
		}

        public virtual object Clone()
        {
            var clone =  MemberwiseClone() as RecordingNodeVM;
            clone.ID = Guid.NewGuid();
            clone._parent = null;
            return clone;
        }
    }

    class RecordingFolderVM : RecordingNodeVM
    {
        int _count;
        public ObservableCollection<RecordingNodeVM> Items { get; private set; }

        public RecordingFolderVM() : this(null)
        {
        }

        public RecordingFolderVM(IEnumerable<RecordingNodeVM> recordings)
        {
			//set fields
			if (recordings == null)
                Items = new RecordingFolderItems();
            else
            {
                Items = new RecordingFolderItems(recordings);
                foreach (var recording in recordings)
                    recording.Parent = this;
            }
            Items.CollectionChanged += Items_CollectionChanged;
            Name = "New Folder";
        }

        void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //update count
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
                Count = Items.Count;

            //update parent
            if (e.Action != NotifyCollectionChangedAction.Move)
            {
                //remove old parents
                if (e.OldItems != null)
                    foreach (RecordingNodeVM oldItem in e.OldItems)
                        oldItem.Parent = null;
                //add new parent
                if (e.NewItems != null)
                    foreach (RecordingNodeVM newItem in e.NewItems)
                        newItem.Parent = this;
            }
        }

        public int? Count
        {
            get { return (_count != 0) ? (int?)_count : null; }
            private set { _count = value.GetValueOrDefault(); FirePropertyChanged(); }
        }

        public override object Clone()
        {
            var clone = base.Clone() as RecordingFolderVM;

            //deep clone items
            clone.Items = new ObservableCollection<RecordingNodeVM>(clone.Items.Select(i => i.Clone() as RecordingNodeVM));
            clone.Items.CollectionChanged += clone.Items_CollectionChanged;
            foreach (var item in clone.Items)
                item.Parent = clone;

            return clone;
        }


		class RecordingFolderItems : ObservableCollection<RecordingNodeVM>
		{
			public RecordingFolderItems() { }
			public RecordingFolderItems(IEnumerable<RecordingNodeVM> items) : base(items) { }

			protected override void InsertItem(int index, RecordingNodeVM item)
			{
				//always insert folder at the top
				if (item is RecordingFolderVM)
				{
					var lastFolder = this.LastOrDefault(e => e is RecordingFolderVM);
					index = 0;
					if (lastFolder != null)
						index = this.IndexOf(lastFolder) + 1;
				}

				base.InsertItem(index, item);
			}
		}

	}

    class RecordingVM : RecordingNodeVM
    {
		public RecordingType RecordingType { get; private set; }

		public RecordingVM(Recording recording)
        {
            //set fields
            Name = "New Recording";
			RecordingType = recording.RecordingType;
        }

        public override object Clone()
        {
            var clone = base.Clone() as RecordingVM;
			clone.RecordingType = RecordingType;
            return clone;
        }
    }

    class RecordingEditVM : BaseTrackingVM, IDropTarget
    {
        MovementVM _selected;
        List<MovementVM> _selection;
        PropertyObserver<ObservableCollectionTracking<MovementVM>> _movementsEvents;
        PropertyObserver<RecordingEditVM> _events;
        PlaybackControl _playback;
        Action<Recording> _save;
        Subject<bool> _hasChangedOccurred;
        IDisposable _hasChangedThrottled;
        bool _dragDropEnabled = true;
		MovementVM _updatingTarget;
		MotionStudioSim _sim;
		Action _updatePlaybackPosition;
		Action<PositionVM> _replaceRecording;
		IKinematics _ik;

		public RelayCommand DeleteCommand { get; }
        public RelayCommand SliceCommand { get; }
        public RelayCommandAsync InsertCommand { get; }
        public RelayCommand SaveCommand { get; }
		public RelayCommandAsync UpdateTargetCommand { get; }
		public RelayCommand CopyCommand { get; }
		public ObservableCollectionTracking<MovementVM> Movements { get; }
        public ICommand SelectionChangedCommand { get; }
		public RelayCommand RenameCommand { get; }
		public RelayCommand ConvertToCommand { get; }
		public RelayCommand ConvertToRealTimeCommand { get; }
		public RelayCommand ConvertToPositionCommand { get; }
		public RelayCommand ConvertToPickCommand { get; }
		public RelayCommand ConvertToPlaceCommand { get; }
		public RelayCommand ConvertToSafetyCommand { get; }
		public RelayCommand ServosRecordedCommand { get; }
		public RelayCommand ServosRecordedClosedCommand { get; }
		public RelayCommand ClearServoRecorded1Command { get; }
		public RelayCommand ClearServoRecorded2Command { get; }
		public RelayCommand ClearServoRecorded3Command { get; }
		public RelayCommand ClearServoRecorded4Command { get; }
		public RelayCommand ClearServoRecorded5Command { get; }

		public event EventHandler<List<MovementVM>> SelectionChanged;

        public RecordingEditVM(PlaybackControl playback, Action<Recording> save, MotionStudioSim sim, Action updatePlaybackPosition, Action<PositionVM> replaceRecording)
        {
            //set fields
            _playback = playback;
            _save = save;
			_sim = sim;
			_ik = new KinematicsUI(App.Instance.Arm);
			_updatePlaybackPosition = updatePlaybackPosition;
			_replaceRecording = replaceRecording;
			_selection = new List<MovementVM>();
            Movements = CreateMovements(_playback.Recording.Movements);
            _movementsEvents = new PropertyObserver<ObservableCollectionTracking<MovementVM>>(Movements)
                .RegisterHandler(i => i.IsChanged, MovementsDirty);
            _events = new PropertyObserver<RecordingEditVM>(this)
                .RegisterHandler(i => i.IsChanged, IsDirty);
            HasChanged += OnHasChanged;
            Movements.HasChanged += OnHasChanged;
			DeleteCommand = new RelayCommand(Delete);
			RenameCommand = new RelayCommand(Rename);
			SliceCommand = new RelayCommandAsync(Slice) { Enabled = false };
            InsertCommand = new RelayCommandAsync(Insert);
			CopyCommand = new RelayCommand(Copy);
			SaveCommand = new RelayCommand(Save);
            SaveCommand.Enabled = false;
			UpdateTargetCommand = new RelayCommandAsync(UpdateTarget) { Enabled = false };
			ConvertToCommand = new RelayCommand(UpdateConvertTo);
			ConvertToRealTimeCommand = new RelayCommand(() => ConvertTo(typeof(PositionVM)));
			ConvertToPositionCommand = new RelayCommand(() => ConvertTo(typeof(MoveVM)));
			ConvertToPickCommand = new RelayCommand(() => ConvertTo(typeof(PickVM)));
			ConvertToPlaceCommand = new RelayCommand(() => ConvertTo(typeof(PlaceVM)));
			ConvertToSafetyCommand = new RelayCommand(() => ConvertTo(typeof(SafetyVM)));
			ServosRecordedCommand = new RelayCommand(UpdateServosRecorded);
			ServosRecordedClosedCommand = new RelayCommand(ServosRecordedClosed);
			ClearServoRecorded1Command = new RelayCommand(() => ClearServoRecorded(0));
			ClearServoRecorded2Command = new RelayCommand(() => ClearServoRecorded(1));
			ClearServoRecorded3Command = new RelayCommand(() => ClearServoRecorded(2));
			ClearServoRecorded4Command = new RelayCommand(() => ClearServoRecorded(3));
			ClearServoRecorded5Command = new RelayCommand(() => ClearServoRecorded(4));
			SelectionChangedCommand = new RelayCommand<SelectionChangedEventArgs>(OnSelectionChanged);
            Selected = null;
            _hasChangedOccurred = new Subject<bool>();
            _hasChangedThrottled = _hasChangedOccurred
                .Throttle(TimeSpan.FromSeconds(2))
                .Subscribe(HasChangedTimout);

			//update right away if Movements changes upon loading
			if (Movements.Any(i => i.IsChanged))
				IsChanged = true;
		}

		ObservableCollectionTracking<MovementVM> CreateMovements(RecordedMovement[] recordedMovements)
		{
			var movements = new ObservableCollectionTracking<MovementVM>();
			var movementCollection = new List<RecordedMovement>();
			Type movementCollectionType = null;
			Type movementType = null;
			bool isNew = false;
			foreach (var movement in recordedMovements)
			{
				//determine movement type
				if (movement.Type == RecordedMovementType.Move)
				{
					if (movement.PickAndPlaceType == null)
						movementType = typeof(MoveVM);
					else if (movement.PickAndPlaceType.Value == PickAndPlaceType.Pick)
						movementType = typeof(PickVM);
					else if (movement.PickAndPlaceType.Value == PickAndPlaceType.Place)
						movementType = typeof(PlaceVM);
					else if (movement.PickAndPlaceType.Value == PickAndPlaceType.Safety)
						movementType = typeof(SafetyVM);
				}
				else if (movement.Type == RecordedMovementType.Position)
					movementType = typeof(PositionVM);
				isNew = movement.Description != null;

				//add the movement
				CreateMovement(movement, isNew, movementType, movementCollectionType, movementCollection, movements);

				movementCollectionType = movementType;
			}

			//add the leftover movement
			CreateMovement(null, true, null, movementCollectionType, movementCollection, movements);

			return movements;
		}

		void CreateMovement(RecordedMovement movement, bool isNew, Type movementType, Type movementCollectionType, List<RecordedMovement> movementCollection, ObservableCollectionTracking<MovementVM> movements)
		{
			//create a movement from collection
			if ((isNew || movementType != movementCollectionType) && movementCollection.Count != 0)
			{
				if (movementCollectionType == typeof(PositionVM))
					movements.Add(new PositionVM(this, movementCollection.ToArray(), _replaceRecording));
				if (movementCollectionType == typeof(PickVM))
					movements.Add(new PickVM(this, movementCollection.ToArray(), _ik));
				if (movementCollectionType == typeof(PlaceVM))
					movements.Add(new PlaceVM(this, movementCollection.ToArray(), _ik));
				movementCollection.Clear();
			}

			//create movement
			if (movementType == typeof(MoveVM))
				movements.Add(new MoveVM(this, movement));
			else if (movementType == typeof(SafetyVM))
				movements.Add(new SafetyVM(this, movement));
			else
				movementCollection.Add(movement);
		}

		void UpdateConvertTo()
		{
			//disable unnessissary convertions
			ConvertToRealTimeCommand.Enabled = !Selection.All(i => i.GetType() == typeof(PositionVM));
			ConvertToPositionCommand.Enabled = !Selection.All(i => i.GetType() == typeof(MoveVM));
			ConvertToPickCommand.Enabled = !Selection.All(i => i.GetType() == typeof(PickVM));
			ConvertToPlaceCommand.Enabled = !Selection.All(i => i.GetType() == typeof(PlaceVM));
			ConvertToSafetyCommand.Enabled = !Selection.All(i => i.GetType() == typeof(SafetyVM));
		}

		void ConvertTo(Type movementType)
		{
			//convert each movement
			var selection = Selection.ToArray();
			foreach (var movement in selection)
			{
				if (movement.GetType() != movementType)
				{
					//create a new movement
					MovementVM newMovement = null;
					if (movementType == typeof(PositionVM))
					{
						var pose = movement.Target.Copy();
						pose.Type = RecordedMovementType.Position;
						pose.PickAndPlaceType = null;
						newMovement = new PositionVM(this, new RecordedMovement[] { pose }, _replaceRecording);
					}
					if (movementType == typeof(MoveVM))
					{
						var pose = movement.Target.Copy();
						pose.Type = RecordedMovementType.Move;
						pose.PickAndPlaceType = null;
						newMovement = new MoveVM(this, pose);
					}
					if (movementType == typeof(PickVM))
					{
						var pose = movement.Target.Copy();
						pose.Type = RecordedMovementType.Move;
						pose.PickAndPlaceType = PickAndPlaceType.Pick;
						if (movement.GetType() == typeof(PlaceVM))
						{
							//special conversion for PlaceVM
							var pickPlace = movement as PickPlaceVM;
							var secondaryPose = movement.SecondaryTarget.Copy();
							var gripper = pose.Pose[4];
							pose.Pose[4] = secondaryPose.Pose[4];
							secondaryPose.Pose[4] = gripper;
							if (pose.Pose[0] != null)
								pose.Pose[0] += pickPlace.ShoulderOffset;
							if (secondaryPose.Pose[0] != null)
								secondaryPose.Pose[0] -= pickPlace.ShoulderOffset;
							newMovement = new PickVM(this, new RecordedMovement[] { pose, secondaryPose }, _ik);
						}
						else
							newMovement = new PickVM(this, new RecordedMovement[] { pose }, _ik);
					}
					if (movementType == typeof(PlaceVM))
					{
						var pose = movement.Target.Copy();
						pose.Type = RecordedMovementType.Move;
						pose.PickAndPlaceType = PickAndPlaceType.Place;
						if (movement.GetType() == typeof(PickVM))
						{
							//special conversion for PickVM
							var pickPlace = movement as PickPlaceVM;
							var secondaryPose = movement.SecondaryTarget.Copy();
							var gripper = pose.Pose[4];
							pose.Pose[4] = secondaryPose.Pose[4];
							secondaryPose.Pose[4] = gripper;
							if (pose.Pose[0] != null)
								pose.Pose[0] -= pickPlace.ShoulderOffset;
							if (secondaryPose.Pose[0] != null)
								secondaryPose.Pose[0] += pickPlace.ShoulderOffset;
							newMovement = new PlaceVM(this, new RecordedMovement[] { pose, secondaryPose }, _ik);
						}
						else
							newMovement = new PlaceVM(this, new RecordedMovement[] { pose }, _ik);
					}
					if (movementType == typeof(SafetyVM))
					{
						var pose = movement.Target.Copy();
						pose.Type = RecordedMovementType.Move;
						pose.PickAndPlaceType = PickAndPlaceType.Safety;
						newMovement = new SafetyVM(this, pose);
						newMovement.Description = "safety";

					}

					//swap the movement
					if (newMovement != null)
					{
						if (newMovement.Description == null)
							newMovement.Description = movement.Description;
						var index = Movements.IndexOf(movement);
						Movements.Remove(movement);
						Movements.Insert(index, newMovement);
					}
				}
			}

			IsChanged = true;
		}

		public IEnumerable<MovementVM> Selection
        {
            get { return _selection; }
        }

        void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            //sync the list of selected items
            bool changed = false;
            if (e.RemovedItems != null)
            {
                foreach (MovementVM item in e.RemovedItems)
                {
                    _selection.Remove(item);
                    changed = true;
                }
            }
            if (e.AddedItems != null)
            {
                foreach (MovementVM item in e.AddedItems)
                {
                    _selection.Add(item);
                    changed = true;
                }
            }

            if (changed)
                SelectionChanged?.Invoke(this, _selection);
        }

        public MovementVM Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;

                //enable / disable commands
                DeleteCommand.Enabled = _selected != null;
				CopyCommand.Enabled = _selected != null;
				RenameCommand.Enabled = _selected != null;
				ServosRecordedCommand.Enabled = _selected != null;
				ConvertToCommand.Enabled = _selected != null;

				FirePropertyChanged();
            }
        }

        void MovementsDirty(ObservableCollectionTracking<MovementVM> movements)
        {
            if (movements.IsChanged)
                IsChanged = true;
        }

        void IsDirty(RecordingEditVM recordingEdit)
        {
            //enable / disabled commands
            SaveCommand.Enabled = IsChanged;
        }

        void OnHasChanged(object sender, EventArgs e)
        {
            //expire recording cache
            _playback.RecordingHasChanged();

			//rebuild the recordedMovements from MovementVMs
			UpdateRecording();

            //slide timout for recache
            _hasChangedOccurred.OnNext(true);
        }

        void HasChangedTimout(bool done)
        {
            Task.Run(_playback.CalculateRecording);
        }

        public override void AcceptChanges()
        {
            base.AcceptChanges();
            Movements.AcceptChanges();
        }

        public void SetCursorPosition(PlaybackPosition position, bool isPlaying)
        {
            //set percent complete on each Movement
            int index = 0;
			MovementVM cusorOnMovement = null;
            for (int i = 0; i < Movements.Count; i++)
            {
				var movement = Movements[i];
				movement.SetPercentageComplete(position, index);
                index += movement.IndexCount;
				if (cusorOnMovement == null && movement.PercentComplete > 0 && movement.PercentComplete < 1)
					cusorOnMovement = movement;
			}

			//enable slice tool
			EnableSliceCommand(!(isPlaying || position.TotalComplete >= 1 || position.TotalComplete <= 0 || cusorOnMovement == null || !cusorOnMovement.CanSplit));
        }

		void EnableSliceCommand(bool enable)
		{
			if (SliceCommand.Enabled == enable)
				return;

			if (SwitchToMainThread(enable))
				return;

			SliceCommand.Enabled = enable;
		}

        async internal Task ShuttleTo(MovementVM movement, bool isSecondary)
        {
			//Updating target
			SetUpdatingTarget(movement, isSecondary);

			//change selection
			Selected = movement;

			//get the index of the movement
			var target = isSecondary ? movement.SecondaryTarget : movement.Target;
			var index = Array.IndexOf(_playback.Recording.Movements, target);

			//shuttle
			await _playback.ShuttleIndex(index, 1);

			//update playback position
			_updatePlaybackPosition();
		}

		internal void SetUpdatingTarget(MovementVM target, bool isSecondary)
		{
			if (_updatingTarget != null)
			{
				_updatingTarget.UpdatingTarget = false;
				_updatingTarget.UpdatingSecondaryTarget = false;
			}
			_updatingTarget = target;
			if (_updatingTarget != null)
			{
				_updatingTarget.UpdatingTarget = !isSecondary;
				_updatingTarget.UpdatingSecondaryTarget = isSecondary;
			}
			UpdateTargetCommand.Enabled = _updatingTarget != null;
		}

		void ClearServoRecorded(int servoIndex)
		{
			//clear servo recordings
			foreach (var movement in Selection)
				foreach (var rec in movement)
					if (rec.Pose != null)
					{
						rec.Pose[servoIndex] = null;

						//update safety
						var safety = movement as SafetyVM;
						if (safety != null)
							safety.SyncPose();
					}
			IsChanged = true;
		}

		void ServosRecordedClosed()
		{
			//stop hilighting servos
			_sim.Hilight();
		}

		void UpdateServosRecorded()
		{
			//create list of servos that can be cleared from the recording
			var servoCount = new int[5];
			var servoFound = new bool[5];
			foreach (var movement in Selection)
			{
				//reset found flags
				for (int i = 0; i < servoFound.Length; i++)
					servoFound[i] = false;

				foreach (var rec in movement)
				{
					//mark found servos in use
					if (rec.Pose != null)
					{
						for (int i = 0; i < servoCount.Length; i++)
						{
							if (!servoFound[i] && rec.Pose[i] != null)
							{
								servoCount[i]++;
								servoFound[i] = true;
							}
						}

					}

					//exit if all found
					if (servoFound.All(i => i))
						break;
				}
			}

			var total = Selection.Count();
			for (int i = servoCount.Length - 1; i >= 0; i--)
			{
				string name = null;
				RelayCommand cmd = null;
				switch (i)
				{
					case 0: name = "Shoulder"; cmd = ClearServoRecorded1Command; break;
					case 1: name = "Upper Arm"; cmd = ClearServoRecorded2Command; break;
					case 2: name = "Forearm"; cmd = ClearServoRecorded3Command; break;
					case 3: name = "Hand"; cmd = ClearServoRecorded4Command; break;
					case 4: name = "Gripper"; cmd = ClearServoRecorded5Command; break;
				}
				var desc = $"remove {name} keyframes";
				if (servoCount[i] < total && servoCount[i] != 0)
					desc += $" (in {servoCount[i]} selected items)";
				if (servoCount[i] == 0)
					desc = $"no {name} keyframes";

				cmd.Enabled = servoCount[i] > 0;
				cmd.DisplayName = desc;
			}

			//hilight servos
			_sim.Hilight(servoCount.Select((i, e) => (i > 0) ? e + 1 : 0).Where(i => i != 0).ToArray());
		}

		void Delete()
        {
            if (_selection.Count != 0)
            {
                foreach (var selected in _selection.ToArray())
                    Movements.Remove(selected);
				IsChanged = true;
            }
        }

		void Copy()
		{
			if (Selected != null)
			{
				//make a copy
				var copies = new List<MovementVM>();
				foreach (var selected in _selection.ToArray())
					copies.Add(selected.Clone() as MovementVM);

				//change copies name
				foreach (var copy in copies)
					if (!copy.RetainSameName)
						copy.Description += "-copy";

				//add at the end of the selection
				var insertIndex = Movements.IndexOf(_selection.Last()) + 1;
				for (int i = 0; i < copies.Count; i++)
					Movements.Insert(insertIndex + i, copies[i]);

				IsChanged = true;
			}
		}

		void Rename()
		{
			if (Selected != null)
			{
				Selected.IsEditingName = true;
			}
		}

		void Save()
        {
            //save recording
            _save(_playback.Recording);

            //mark entities as saved
            AcceptChanges();
        }

        async Task Slice()
        {
			//validate
			var position = _playback.GetPlaybackPosition();
			if (position.TotalComplete <= 0 || position.TotalComplete >= 1)
				return;

			//get movement
			var target = _playback.Recording.Movements[position.CurrentIndex];
			var movement = Movements.FirstOrDefault(i => i.Contains(target));
			if (movement == null || !movement.CanSplit)
				return;

			//split movement
			var pose = await _playback.CalculatePose(target, position.IndexComplete);
			var splitMovement = movement.Split(target, position.IndexComplete, pose);
			if (splitMovement == null)
				return;

			//rename
			splitMovement.Description = movement.Description + "-split";

			//insert after movement
			var insertIndex = Movements.IndexOf(movement) + 1;
			Movements.Insert(insertIndex, splitMovement);

			IsChanged = true;

		}

        async Task Insert()
        {
			//create movement
			var pose = await _playback.Arm.Servos.GetPoseAsync(true);
			var desc = "current pose ";
			var nextCurrentPose = Movements.Where(i => i.Description != null && i.Description.StartsWith(desc)).Select(i => int.TryParse(i.Description.Substring(i.Description.LastIndexOf(' ')), out var result) ? result : 0).DefaultIfEmpty().Max() + 1;
			desc += nextCurrentPose.ToString();
			var newMove = new MoveVM(this, new RecordedMovement() { Pose = pose, Type = RecordedMovementType.Move, Speed = 50, EaseIn = 0, EaseOut = 0, Description = desc });

			//insert at end of the selection
			if (Selected == null)
				Movements.Add(newMove);
			else
			{
				var insertIndex = Movements.IndexOf(_selection.Last()) + 1;
				Movements.Insert(insertIndex, newMove);
			}

			//change selection to inserted item
			Selected = newMove;

			IsChanged = true;
		}

		async Task UpdateTarget()
		{
			var movement = _updatingTarget;

			if (movement != null)
			{
				//update the target position - use last set position for servos that are on
				var pose = await _playback.Arm.Servos.GetPoseAsync(true);
				var position = _playback.GetPlaybackPosition();
				var target = _playback.Recording.Movements[position.CurrentIndex];
				var initialPose = await _playback.CalculatePose(target, position.IndexComplete, true);
				movement.UpdateTarget(pose, target, initialPose, movement.UpdatingSecondaryTarget);
			}

			//mark as changed
			SetUpdatingTarget(null, false);
			IsChanged = true;
		}

		void UpdateRecording()
		{
			//rebuild a new recording based on the movements in the UI
			_playback.Recording.Movements = Movements.SelectMany(i => i).ToArray();
		}

		public bool DragDropEnabled
        {
            get { return _dragDropEnabled; }
            set { _dragDropEnabled = value; FirePropertyChanged(); }
        }

        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            //validate
            var source = dropInfo.Data as MovementVM;
            var sourceList = dropInfo.Data as IList;
            var target = dropInfo.TargetItem as MovementVM;
            if (dropInfo.TargetCollection != Movements || !dropInfo.IsSameDragDropContextAsSource || (source == null && (sourceList == null || sourceList.OfType<MovementVM>().Count() == 0)) || target == null)
                return;

            //rearange
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = DragDropEffects.Move;
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            //validate
            var source = dropInfo.Data as MovementVM;
            var sourceList = dropInfo.Data as IList;
            var target = dropInfo.TargetItem as MovementVM;
            if (dropInfo.TargetCollection != Movements || !dropInfo.IsSameDragDropContextAsSource || (source == null && sourceList == null) || target == null)
                return;

            //rearange
            if (dropInfo.Effects.HasFlag(DragDropEffects.Move))
            {
                if (sourceList != null)
                {
                    foreach (MovementVM s in sourceList)
                    {
                        var sIndex = Movements.IndexOf(s);
                        var tIndex = dropInfo.InsertIndex > sIndex ? dropInfo.InsertIndex - 1 : dropInfo.InsertIndex;
                        if (sIndex != tIndex)
                            Movements.Move(sIndex, tIndex); //change in UI
                    }
                }
                else
                {
                    var sIndex = Movements.IndexOf(source);
                    var tIndex = dropInfo.InsertIndex > sIndex ? dropInfo.InsertIndex - 1 : dropInfo.InsertIndex;
                    if (sIndex != tIndex)
                        Movements.Move(sIndex, tIndex); //change in UI
                }

				IsChanged = true;
                return;
            }
        }




		public class ServoRecorded
		{
			public string Description { get; set; }
			public bool IsEnabled { get; set; }
			public int ServoIndex { get; set; }
		}
	}

    abstract class MovementVM : BaseTrackingVM, IAdornerFactory, ICloneable, IEnumerable<RecordedMovement>
    {
        RecordingEditVM _parent;
        double _percentComplete;
		bool _isEditingName;
		bool _updatingTarget;
		bool _updatingSecondaryTarget;

		public ICommand ShuttleToCommand { get; private set; }
		public ICommand SetTargetCommand { get; private set; }


		public MovementVM(RecordingEditVM parent)
        {
            //set fields
            _parent = parent;
            ShuttleToCommand = new RelayCommandAsync<string>(ShuttleTo);
			SetTargetCommand = new RelayCommand<string>(SetTarget);
		}

		protected RecordingEditVM Parent { get => _parent; }

        async Task ShuttleTo(string parameter)
        {
            await _parent.ShuttleTo(this, parameter == "secondary");
        }

		void SetTarget(string parameter)
		{
			_parent.SetUpdatingTarget(this, parameter == "secondary");
		}

		public bool IsEditingName
		{
			get { return _isEditingName; }
			set { _isEditingName = value; FirePropertyChanged(); }
		}

		public abstract string Description { get; set; }

        protected abstract int Speed { get; set; }

        protected virtual int EaseIn { get; set; }

        protected virtual int EaseOut { get; set; }

        protected virtual bool EaseingEnabled { get; } = false;

        internal protected abstract int IndexCount { get; }

        internal protected abstract void SetPercentageComplete(PlaybackPosition position, int index);

        public double PercentComplete
        {
            get { return _percentComplete; }
            protected set
            {
                //validate
                if (_percentComplete == value)
                    return;

                _percentComplete = value;
                FirePropertyChanged();
            }
        }

        Adorner IAdornerFactory.CreateAdorner(UIElement element, Type adorner, object adornerParameter)
        {
            var a = new SpeedAdorner(element, EaseingEnabled);
            a.Speed = Speed;
            a.EaseIn = EaseIn;
            a.EaseOut = EaseOut;
            a.SpeedChanged += Adorner_SpeedChanged;
            a.EaseInChanged += Adorner_EaseInChanged;
            a.EaseOutChanged += Adorner_EaseOutChanged;
            a.IsDragging += Adorner_IsDragging;
            return a;
        }

        private void Adorner_IsDragging(object sender, bool e)
        {
            _parent.DragDropEnabled = !e;
        }

        private void Adorner_EaseOutChanged(object sender, int e)
        {
            EaseOut = e;
        }

        private void Adorner_EaseInChanged(object sender, int e)
        {
            EaseIn = e;
        }

        private void Adorner_SpeedChanged(object sender, int e)
        {
            Speed = e;
        }

		public abstract bool CanSplit { get; }

		public abstract MovementVM Split(RecordedMovement movement, float percentComplete, Pose splitPose);

		public bool UpdatingTarget
		{
			get { return _updatingTarget; }
			set { _updatingTarget = value; FirePropertyChanged(); }
		}

		public bool UpdatingSecondaryTarget
		{
			get { return _updatingSecondaryTarget; }
			set { _updatingSecondaryTarget = value; FirePropertyChanged(); }
		}

		public abstract RecordedMovement Target { get; }

		public abstract RecordedMovement SecondaryTarget { get; }

		public abstract void UpdateTarget(Pose newPose, RecordedMovement target, Pose initialPose, bool isSecondary);

		public virtual bool RetainSameName => false;

		public virtual object Clone()
		{
			var clone = MemberwiseClone() as MovementVM;
			clone.PercentComplete = 0;
			clone._updatingTarget = false;
			clone._updatingSecondaryTarget = false;
			clone.ShuttleToCommand = new RelayCommandAsync<string>(clone.ShuttleTo);
			clone.SetTargetCommand = new RelayCommand<string>(clone.SetTarget);
			return clone;
		}

		public abstract IEnumerator<RecordedMovement> GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public virtual bool TrailCutoff(int index) => false;

		public virtual TrailDecoration? GetTrailDecoration(Trail trail, IEnumerable<MovementAnalysis> movements, IKinematics ik) => null;

	}

	abstract class MovementSingleVM : MovementVM
	{
		protected RecordedMovement Entity;

		public MovementSingleVM(RecordingEditVM parent, RecordedMovement entity) : base(parent)
        {
			//set fields
			Entity = entity;
		}

		public override string Description
		{
			get { return Entity.Description; }
			set { Entity.Description = value; FirePropertyChanged(true); }
		}

		protected override int Speed
		{
			get { return Entity.Speed.GetValueOrDefault(50); }
			set { Entity.Speed = value; FirePropertyChanged(true); }
		}

		protected override int EaseIn
		{
			get { return Entity.EaseIn.GetValueOrDefault(); }
			set
			{
				Entity.EaseIn = value;
				FirePropertyChanged(true);

				//if ease in set then insure ease out is set too
				if (Entity.EaseOut == null)
					EaseOut = EaseOut;
			}
		}

		protected override int EaseOut
		{
			get { return Entity.EaseOut.GetValueOrDefault(); }
			set
			{
				Entity.EaseOut = value;
				FirePropertyChanged(true);

				//if ease out set then insure ease in is set too
				if (Entity.EaseIn == null)
					EaseIn = EaseIn;
			}
		}

		protected override bool EaseingEnabled
		{
			get { return true; }
		}

		public bool IsSynchronized
		{
			get { return Entity.Synchronized.GetValueOrDefault(); }
			set { Entity.Synchronized = value; FirePropertyChanged(true); }
		}

		public override void UpdateTarget(Pose newPose, RecordedMovement target, Pose initialPose, bool isSecondary)
		{
			Entity.Pose = newPose;
		}

		protected internal override int IndexCount
		{
			get { return 1; }
		}

		protected internal override void SetPercentageComplete(PlaybackPosition position, int index)
		{
			if (position.CurrentIndex > index)
				PercentComplete = 1;
			else if (index == position.CurrentIndex)
				PercentComplete = position.IndexComplete;
			else
				PercentComplete = 0;
		}

		public override bool CanSplit => true;

		public override MovementVM Split(RecordedMovement movement, float percentComplete, Pose splitPose)
		{
			//validate
			if (Entity != movement)
				return null;
			if (percentComplete <= 0 || percentComplete >= 1)
				return null;

			//create split
			var split = Clone() as MoveVM;
			Entity.Pose = splitPose;

			return split;
		}

		public override object Clone()
		{
			var clone = base.Clone() as MovementSingleVM;
			clone.Entity = Entity.Copy();
			return clone;
		}

		public override IEnumerator<RecordedMovement> GetEnumerator()
		{
			yield return Entity;
		}

		public override RecordedMovement Target
		{
			get { return Entity; }
		}

		public override RecordedMovement SecondaryTarget
		{
			get { return Target; }
		}
	}

    class MoveVM : MovementSingleVM
	{
        public MoveVM(RecordingEditVM parent, RecordedMovement entity) : base(parent, entity)
        {
        }
	}

	class SafetyVM : MovementSingleVM
	{
		public SafetyVM(RecordingEditVM parent, RecordedMovement entity) : base(parent, entity)
		{
			//clean up the safty
			Clean(entity.Pose);

			//replace the safety pose with any existing one
			var pose = GetSafety();
			if (pose != null && !pose.Servos.SequenceEqual(entity.Pose.Servos))
			{
				entity.Pose = pose.Copy();
				IsChanged = true;
			}
		}

		public override void UpdateTarget(Pose newPose, RecordedMovement target, Pose initialPose, bool isSecondary)
		{
			//update all safety positions
			SetSafety(Clean(newPose));
		}

		void SetSafety(Pose pose)
		{
			foreach (var movement in Parent.Movements.OfType<SafetyVM>())
				movement.Entity.Pose = pose.Copy();
		}

		Pose GetSafety()
		{
			//validate
			if (Parent.Movements == null)
				return null;

			//get first safety pose in collection - not including itself
			var movement = Parent.Movements.OfType<SafetyVM>().Where(i => i != this).FirstOrDefault();
			if (movement == null)
				return null;
			return movement.Entity.Pose;
		}

		Pose Clean(Pose pose)
		{
			//safety can't have a gripper recording
			pose[4] = null;

			return pose;
		}

		public void SyncPose()
		{
			//update all safety positions
			SetSafety(Clean(Entity.Pose));
		}

		public override TrailDecoration? GetTrailDecoration(Trail trail, IEnumerable<MovementAnalysis> movements, IKinematics ik)
		{
			var target = ik.GetTarget(movements.First().EndPose).Result;
			var start = new TrailPos() { X = target.X, Y = target.Y, Z = target.Z };
			return new TrailDecoration()
			{
				Type = TrailDecorationType.Safety,
				Width = trail.Decoration.Value.Width,
				R = trail.Decoration.Value.R,
				G = trail.Decoration.Value.G,
				B = trail.Decoration.Value.B,
				A = trail.Decoration.Value.A,
				Start = start
			};
		}

		public override bool RetainSameName => true;
	}

	abstract class MovementArrayVM : MovementVM
	{
		protected RecordedMovement[] Entities;

		public MovementArrayVM(RecordingEditVM parent, RecordedMovement[] entities) : base(parent)
        {
			//set fields
			Entities = entities;
		}

		public override string Description
		{
			get { return Entities.First().Description; }
			set { Entities.First().Description = value; FirePropertyChanged(true); }
		}

		protected internal override int IndexCount
		{
			get { return Entities.Length; }
		}

		protected internal override void SetPercentageComplete(PlaybackPosition position, int index)
		{
			var maxIndex = index + IndexCount;
			if (position.CurrentIndex < index)
				PercentComplete = 0;
			else if (position.CurrentIndex > maxIndex)
				PercentComplete = 1;
			else
				PercentComplete = ((double)(position.CurrentIndex - index) / (double)IndexCount).Clamp(0, 1);
		}

		public override object Clone()
		{
			var clone = base.Clone() as MovementArrayVM;
			clone.Entities = Entities.Copy();
			return clone;
		}

		public override IEnumerator<RecordedMovement> GetEnumerator()
		{
			return Entities.AsEnumerable().GetEnumerator();
		}
	}

	class PositionVM : MovementArrayVM
	{
		Action<PositionVM> _replaceRecording;

		public ICommand NormalizeWaitTimeCommand { get; private set; }
		public ICommand NewRecordingCommand { get; private set; }

		public PositionVM(RecordingEditVM parent, RecordedMovement[] entities, Action<PositionVM> replaceRecording) : base(parent, entities)
        {
            //set fields
			_replaceRecording = replaceRecording;
			NormalizeWaitTimeCommand = new RelayCommand(NormalizeWaitTime);
			NewRecordingCommand = new RelayCommand(NewRecording);
		}

		protected override int Speed
		{
			get { return Entities.First().Speed.GetValueOrDefault(50); }
			set
			{
				//set speed on all movements
				foreach (var entity in Entities)
					entity.Speed = value;

				FirePropertyChanged(true);
			}
		}

		public void NormalizeWaitTime()
        {
            //validate
            if (Entities.Length <= 1)
                return;
            var speed = 10.5f;
            float?[] lastPos = new float?[7];
            bool hasChanged = false;
            for (int i = 0; i < Entities.Length; i++)
            {
                var servoIndex = Entities[i].Pose.Servos.Select((e, index) => (e != null) ? index : -1).Where(e => e != -1).FirstOrDefault();
                if (servoIndex != -1)
                {
                    var pos = Entities[i].Pose[servoIndex];
                    var prevPos = lastPos[servoIndex];
                    lastPos[servoIndex] = pos;
                    if (prevPos != null)
                    {                        
                        var dist = Math.Abs(prevPos.Value - pos.Value);
                        var time = dist * speed;
						Entities[i].Delay = TimeSpan.FromMilliseconds(time);
                    }
                    else
						Entities[i].Delay = null;
                    hasChanged = true;
                }
            }
            if (hasChanged)
                IsChanged = true;
        }

		public void NewRecording()
		{
			//begin replacing the recording
			_replaceRecording(this);
		}

		public void ReplaceRecording(Recording recording)
		{
			//validate
			if (recording == null || recording.Movements == null || recording.Movements.Length <= 1)
				return;

			//replace recording
			Entities = recording.Movements.Where(i => i.Type == RecordedMovementType.Position).ToArray();
			Speed = Speed;

			IsChanged = true;
		}

		public override bool CanSplit => true;

		public override MovementVM Split(RecordedMovement movement, float percentComplete, Pose splitPose)
		{
			//validate
			if (!Entities.Contains(movement))
				return null;
			var index = Array.IndexOf(Entities, movement);
			if (index == 0)
				return null;
			if (index == Entities.Length - 1)
				return null;

			//create split
			var split = Clone() as PositionVM;
			Entities = Entities.Take(index + 1).ToArray();
			split.Entities = split.Entities.Skip(index).ToArray();
			split.First().Delay = null;
			split.Description = Description;

			return split;
		}

		public override void UpdateTarget(Pose newPose, RecordedMovement target, Pose initialPose, bool isSecondary)
		{
			//insure target is within the availible movements
			if (!Entities.Contains(target))
				target = Entities.First();

			//get change in pose
			var changePose = new Pose();
			for (int i = 0; i < changePose.Servos.Length; i++)
			{
				if (initialPose[i].HasValue && newPose[i].HasValue)
					changePose[i] = newPose[i].Value - initialPose[i].Value;
			}

			//set this pose
			target.Pose = newPose;

			//offset future positions
			int index = Array.IndexOf(Entities, target);
			for (int i = index + 1; i < Entities.Length; i++)
				for (int e = 0; e < Entities[i].Pose.Servos.Length; e++)
					if (Entities[i].Pose[e].HasValue && changePose[e].HasValue)
						Entities[i].Pose[e] += changePose[e].Value;

			//blend last .5 sec before
			var maxTime = TimeSpan.FromSeconds(.5);
			var totalTime = TimeSpan.Zero;
			for (int i = index - 1; i >= 0; i--)
			{
				totalTime += Entities[i].Delay.GetValueOrDefault();
				if (totalTime > maxTime)
					break;
				for (int e = 0; e < Entities[i].Pose.Servos.Length; e++)
					if (Entities[i].Pose[e].HasValue && changePose[e].HasValue)
						Entities[i].Pose[e] += changePose[e].Value - (((float)totalTime.Ticks / (float)maxTime.Ticks) * changePose[e].Value);
			}
		}

		public override object Clone()
		{
			var clone = base.Clone() as PositionVM;
			clone.NormalizeWaitTimeCommand = new RelayCommand(clone.NormalizeWaitTime);
			clone.NewRecordingCommand = new RelayCommand(clone.NewRecording);
			return clone;
		}

		public override RecordedMovement Target
		{
			get { return Entities.First(); }
		}

		public override RecordedMovement SecondaryTarget
		{
			get { return Target; }
		}
	}

	abstract class PickPlaceVM : MovementArrayVM
	{
		PickPlaceMovements _pickPlaceMovements;

		public ICommand SetSecondaryTargetOverTargetCommand { get; private set; }

		public PickPlaceVM(RecordingEditVM parent, RecordedMovement[] entities, PickPlaceMovements pickPlaceMovements) : base(parent, entities)
		{
			//set fields
			_pickPlaceMovements = pickPlaceMovements;
			Entities = pickPlaceMovements.Movements;
			IsChanged = pickPlaceMovements.WasGenerated;
			SetSecondaryTargetOverTargetCommand = new RelayCommand<string>(SetSecondaryTargetOverTarget);
		}

		abstract protected TrailDecorationType DecorationType { get; }

		public float ShoulderOffset { get => _pickPlaceMovements.ShoulderOffset; }

		public bool IsSynchronized
		{
			get { return Entities[0].Synchronized.GetValueOrDefault(); }
			set { Entities[0].Synchronized = value; FirePropertyChanged(true); }
		}

		public override bool CanSplit => false;

		public override MovementVM Split(RecordedMovement movement, float percentComplete, Pose splitPose)
		{
			return null;
		}

		public override void UpdateTarget(Pose newPose, RecordedMovement target, Pose initialPose, bool isSecondary)
		{
			if (isSecondary)
				_pickPlaceMovements.SetEntryPose(newPose);
			else
				_pickPlaceMovements.SetTargetPose(newPose);
		}

		protected override int Speed
		{
			get { return Entities[0].Speed.GetValueOrDefault(50); }
			set
			{
				_pickPlaceMovements.SetSpeed(value);

				FirePropertyChanged(true);
			}
		}

		public override RecordedMovement Target
		{
			get { return _pickPlaceMovements.Target; }
		}

		public override RecordedMovement SecondaryTarget
		{
			get { return _pickPlaceMovements.Entry; }
		}

		public override bool TrailCutoff(int index) => index >= 0;

		public override TrailDecoration? GetTrailDecoration(Trail trail, IEnumerable<MovementAnalysis> movements, IKinematics ik)
		{
			var pose = movements.Skip(_pickPlaceMovements.EntryIndex).First().EndPose.Copy(); // <--skip to secondary target
			if (_pickPlaceMovements.EntryIndex == _pickPlaceMovements.OpenGripperIndex)
				pose[0] += _pickPlaceMovements.ShoulderOffset;
			var target = ik.GetTarget(pose).Result; 
			var start = new TrailPos() { X = target.X, Y = target.Y, Z = target.Z };
			pose = movements.Skip(_pickPlaceMovements.TargetIndex).First().EndPose.Copy(); // <--skip to target
			if (_pickPlaceMovements.EntryIndex != _pickPlaceMovements.OpenGripperIndex)
				pose[0] += _pickPlaceMovements.ShoulderOffset;
			target = ik.GetTarget(pose).Result; // <--skip to target
			var end = new TrailPos() { X = target.X, Y = target.Y, Z = target.Z };
			return new TrailDecoration()
			{
				Type = DecorationType,
				Width = trail.Decoration.Value.Width,
				R = trail.Decoration.Value.R,
				G = trail.Decoration.Value.G,
				B = trail.Decoration.Value.B,
				A = trail.Decoration.Value.A,
				Start = start,
				End = end
			};
		}

		public void SetSecondaryTargetOverTarget(string option)
		{
			_pickPlaceMovements.ResetEntryPose(option == "simple");
			IsChanged = true;
		}

		public override object Clone()
		{
			var clone = base.Clone() as PickPlaceVM;
			clone._pickPlaceMovements = clone._pickPlaceMovements.Clone() as PickPlaceMovements;
			clone.Entities = clone._pickPlaceMovements.Movements;
			clone.SetSecondaryTargetOverTargetCommand = new RelayCommand<string>(clone.SetSecondaryTargetOverTarget);
			return clone;
		}
	}

	class PickVM : PickPlaceVM
	{
		public PickVM(RecordingEditVM parent, RecordedMovement[] entities, IKinematics ik) : base(parent, entities, new PickMovements(ik, entities))
		{
		}

		protected override TrailDecorationType DecorationType => TrailDecorationType.Pick;
	}

	class PlaceVM : PickPlaceVM
	{
		public PlaceVM(RecordingEditVM parent, RecordedMovement[] entities, IKinematics ik) : base(parent, entities, new PlaceMovements(ik, entities))
		{
		}
		
		protected override TrailDecorationType DecorationType => TrailDecorationType.Place;
	}
}
