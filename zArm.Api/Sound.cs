using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using zArm.Api.Commands;

namespace zArm.Api
{
    public class Sound
    {
        CommandRunner _runner;
        Arm _arm;

        public event EventHandler<DataEventArg<SoundPlayNotesResponse>> PlayNotesDone;
        public event EventHandler<DataEventArg<SoundStatusResponse>> StatusReceived;

        internal Sound(CommandRunner runner, Arm arm)
        {
            //set fields
            _runner = runner;
            _arm = arm;

            //register responses
            _runner.RegisterForResponse<SoundPlayNotesResponse>(PlayNotesResponse);
            _runner.RegisterForResponse<SoundStatusResponse>(StatusResponse);
        }

        /// <summary>
        /// play a series of notes
        /// </summary>
        /// <param name="notes">format [note:A-F][#][octave:1-7][duration:1-128] ex. D#5-8,C,A-4,R</param>
        public void PlayNotes(string notes = null)
        {
            _runner.Execute(new SoundPlayNotesCommand() { Notes = notes });
        }

        /// <summary>
        /// play a series of notes, return when done
        /// </summary>
        /// <param name="notes">format [note:A-F][#][octave:1-7][duration:1-128] ex. D#5-8,C,A-4,R</param>
        public async Task<SoundPlayNotesResponse> PlayNotesAsync(string notes = null, CancellationToken? cancelationToken = null)
        {
            return await _runner.ExecuteAsync(new AsyncCommandFirstResponse<SoundPlayNotesResponse>(new SoundPlayNotesCommand() { Notes = notes, CallbackWhenDone = true }, cancelationToken, ()=> Stop(), null, 4000));
        }

        /// <summary>
        /// Stop playing sounds
        /// </summary>
        public void Stop()
        {
            _runner.Execute(new SoundStopCommand());
        }

        /// <summary>
        /// play a frequency
        /// </summary>
        /// <param name="Frequency">from 1 - 4000</param>
        public void PlayFrequency(int? Frequency = null)
        {
            _runner.Execute(new SoundPlayFreqCommand() { Frequency = Frequency });
        }

        /// <summary>
        /// will play notes when the button is pressed
        /// </summary>
        /// <param name="notes">format [note:A-F][#][octave:1-7][duration:1-128] ex. D#5-8,C,A-4,R</param>
        public void SyncWithButton(string notes = null)
        {
            _runner.Execute(new SoundSyncButtonCommand() { Notes = notes });
        }

        /// <summary>
        /// will turn off playing notes when the button is pressed
        /// </summary>
        public void SyncWithButtonOff()
        {
            _runner.Execute(new SoundSyncButtonOffCommand());
        }

        /// <summary>
        /// will play notes when the LED is on
        /// </summary>
        /// <param name="notes">format [note:A-F][#][octave:1-7][duration:1-128] ex. D#5-8,C,A-4,R</param>
        public void SyncWithLed(string notes = null)
        {
            _runner.Execute(new SoundSyncLedCommand() { Notes = notes });
        }

        /// <summary>
        /// will turn off playing notes when the LED is on
        /// </summary>
        public void SyncWithLedOff()
        {
            _runner.Execute(new SoundSyncLedOffCommand());
        }

        /// <summary>
        /// request to receive the current status of the playing sound.  the StatusReceived event will fire after this call
        /// </summary>
        public async Task<SoundStatusResponse> GetStatusAsync()
        {
            return await _runner.ExecuteAsync(new AsyncCommandFirstResponse<SoundStatusResponse>(new SoundStatusCommand()));
        }

        void StatusResponse(SoundStatusResponse status)
        {
            //fire the event
            StatusReceived?.Invoke(_arm, new DataEventArg<SoundStatusResponse>(status));
        }

        void PlayNotesResponse(SoundPlayNotesResponse status)
        {
            //fire the event
            PlayNotesDone?.Invoke(_arm, new DataEventArg<SoundPlayNotesResponse>(status));
        }
    }

}
