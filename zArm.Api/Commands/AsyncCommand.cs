using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace zArm.Api.Commands
{
    public abstract class AsyncCommand
    {
        public abstract bool HasReceivedResponse(Response response);
    }

    public class AsyncCommand<T> : AsyncCommand
    {
        TaskCompletionSource<T> _result;
        int _timeoutMS;
        Func<Response, T> _resultBuilder;
        CancellationToken? _cancellationToken;
        Action _cancellationAction;
        Func<Task<T>, Task<T>> _cancellationResult;
        TaskCompletionSource<bool> _cancelTask;

        public Command Command { get; }

        public AsyncCommand (Command command, Func<Response, T> resultBuilder = null, CancellationToken? cancellationToken = null, Action cancellationAction = null, Func<Task<T>, Task<T>> cancellationResult = null, int timeoutMS = 2000)
        {
            //set fields
            Command = command;
            _result = new TaskCompletionSource<T>();
            _timeoutMS = timeoutMS;
            _resultBuilder = resultBuilder;
            _cancellationToken = cancellationToken;
            _cancellationAction = cancellationAction;
            _cancellationResult = cancellationResult;

            //default cancelation result
            if (_cancellationResult == null)
                _cancellationResult = (i) => Task.FromResult(default(T));
        }

        public override bool HasReceivedResponse(Response response)
        {
            var result = _resultBuilder(response);
            if (!EqualityComparer<T>.Default.Equals(result, default(T)))
            {
                if (!_result.Task.IsCompleted)
                    _result.SetResult(result);
                return true;
            }
            return false;
        }

        public bool SetupCancelation()
        {
            //register cancelation as task
            _cancelTask = new TaskCompletionSource<bool>();
            if (_cancellationToken != null)
            {
                if (_cancellationToken.Value.IsCancellationRequested)
                    return false; //already canceled
                _cancellationToken.Value.Register(() => _cancelTask.TrySetResult(false));
            }
            return true;
        }

        public async Task<T> GetCancellationResult()
        {
            return await _cancellationResult(null);
        }

        public async Task<T> GetResponseAsync()
        {
            //create timout task
            var timeout = Task.Delay(_timeoutMS);

            //wait for results
            var completedTask = await Task.WhenAny(_result.Task, timeout, _cancelTask.Task);

            //results if canceled or timmed out
            if (completedTask == _cancelTask.Task || completedTask == timeout)
            {
                _cancellationAction?.Invoke();
                return await _cancellationResult(_result.Task);
            }

            //otherwise await the final result
            return await _result.Task;
        }
    }

    public class AsyncCommandFirstResponse<T> : AsyncCommand<T>
        where T : Response
    {
        public AsyncCommandFirstResponse(Command command, CancellationToken? cancellationToken = null, Action cancellationAction = null, Func<Task<T>, Task<T>> cancellationResult = null, int timeoutMS = 2000) : base(command, GetFirstResponse, cancellationToken, cancellationAction, cancellationResult, timeoutMS)
        { }

        static T GetFirstResponse(Response response)
        {
            var correctResponse = response as T;
            if (correctResponse != null)
                return correctResponse;
            return null;
        }
    }

    public class AsyncCommandFirstServoResponse<T> : AsyncCommand<T>
        where T : ServoResponse
    {
        public AsyncCommandFirstServoResponse(int servoID, Command command, CancellationToken? cancellationToken = null, Action cancellationAction = null, Func<Task<T>, Task<T>> cancellationResult = null, int timeoutMS = 2000) : base(command, i=> GetFirstServoResponse(servoID,i), cancellationToken, cancellationAction, cancellationResult, timeoutMS)
        { }

        static T GetFirstServoResponse(int servoID, Response response)
        {
            var correctResponse = response as T;
            if (correctResponse != null && correctResponse.ServoID == servoID)
                return correctResponse;
            return null;
        }
    }

}
