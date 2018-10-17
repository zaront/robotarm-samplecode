using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace zArm.Api.Commands
{
    public class CommandRunner
    {
        ICommunication _comm;
        MessageHandlerRegistry _messageHandlerRegistry = new MessageHandlerRegistry();
        List<AsyncCommand> _registeredCommandResponses = new List<AsyncCommand>();

        public CommandRunner(ICommunication comm)
        {
            //set fields
            _comm = comm;

            //connect to events
            _comm.ReceivedResponse += Comm_ReceivedResponse;
        }

        private void Comm_ReceivedResponse(object sender, ComResponseEventArgs e)
        {
            //validate
            if (e.Responses == null || e.Responses.Length == 0)
                return;

            //call registered responses
            foreach (var response in e.Responses)
            {
                //async callback
                if (_registeredCommandResponses.Count != 0)
                {
                    lock (_registeredCommandResponses)
                    {
                        foreach (var cr in _registeredCommandResponses)
                        {
                            if (cr.HasReceivedResponse(response))
                                break;
                        }
                    }
                }

                //standard callback
                _messageHandlerRegistry.HandleMessage(response);
            }
        }

        public void Execute(params Command[] commands)
        {
            _comm.Send(commands);
        }

        public async Task<T> ExecuteAsync<T>(AsyncCommand<T> command)
        {
            //return result if already canceled
            if (!command.SetupCancelation())
                return await command.GetCancellationResult();

            //register
            lock (_registeredCommandResponses)
            {
                _registeredCommandResponses.Add(command);
            }

            //execute
            Execute(command.Command);

            //await results
            var result = await command.GetResponseAsync();

            //unregister
            lock (_registeredCommandResponses)
            {
                _registeredCommandResponses.Remove(command);
            }

            return result;
        }

        public void RegisterForResponse<T>(Action<T> callback)
            where T : Response
        {
            _messageHandlerRegistry.Register<T>(callback);
        }
    }
}
