using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Genesys.ApiClient.Components.ComponentModel
{
    public abstract class ActiveGenesysComponent : GenesysComponent
    {
        readonly AwaitingStart awaitingStart = new AwaitingStart();

        ActivationStage activationStage = ActivationStage.Idle;

        protected bool AutoRecover { get; private set; }

        // Valid only during the Starting stage
        CancellationTokenSource startCancelToken;

        /// <summary>
        /// This method only triggers the activation procedure, it does not wait for completion, nor
        /// guarantees a succesful activation.
        /// After calling this method, this object will automatically be recovered.
        /// In order to wait for completion, please use <see cref="StartAsync()"/>.
        /// </summary>
        public void Start()
        {
            UpdateTree(Start);
        }
            
        protected void Start(UpdateResult updateResult)
        {
            AutoRecover = true;

            if (activationStage == ActivationStage.Idle && CanStart() != null)
            {
                var _ = DoStartAsync(updateResult, background: true); // assigment to prevent warning for not using await
            }
        }

        /// <summary>
        /// Loads current object state and subscribes for its events, in order to keep up-to-date.
        /// This method waits for completion of the activation procedure, and will throw an exception if
        /// activation fails for some reason.
        /// If this method completes successfully, this object will be automatically recovered on reconnections
        /// (when the connection is lost and then open again). If the method fails, then automatic recovery will
        /// not be enabled.
        /// </summary>
        /// <exception cref="ActivationException">If object activation failed.</exception>
        public Task StartAsync()
        {
            return UpdateTreeAsync(StartAsync);
        }

        protected async Task StartAsync(UpdateResult result)
        {
            if (activationStage == ActivationStage.Idle)
            {
                var exc = CanStart();
                if (exc != null)
                    throw exc;

                await DoStartAsync(result, background: false);
            }
            else if (activationStage == ActivationStage.Starting)
            {
                // Start is ongoing. Await completion
                await awaitingStart.Await();
            }
            else
            {
                // Already started. Nothing to do
            }
        }

        async Task DoStartAsync(UpdateResult updateResult, bool background)
        {
            try
            {
                SetActivationStage(updateResult.NotificationsOnAwait, ActivationStage.Starting);
                startCancelToken = new CancellationTokenSource();
                await StartImplAsync(updateResult, startCancelToken.Token);
                AutoRecover = true;
                SetActivationStage(updateResult.Notifications, ActivationStage.Started);
                awaitingStart.Complete(null);
            }
            catch (Exception e)
            {
                StopImpl(updateResult);
                SetActivationStage(updateResult.Notifications, ActivationStage.Idle);
                awaitingStart.Complete(e);

                if (background)
                    RaiseRecoveryFailed(new ActivationException(e));
                else
                    throw;
            }
        }

        public void Stop()
        {
            UpdateTree(Stop);
        }

        protected void Stop(UpdateResult result)
        {
            if (activationStage == ActivationStage.Started)
            {
                StopImpl(result);
                SetActivationStage(result.Notifications, ActivationStage.Idle);
            }
            else if (activationStage == ActivationStage.Starting)
            {
                startCancelToken.Cancel();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Stop();

            base.Dispose(disposing);
        }

        protected abstract Task StartImplAsync(UpdateResult result, CancellationToken cancellationToken);

        protected virtual Exception CanStart()
        {
            return null;
        }

        /// <summary>
        /// Implementations must not throw exceptions.
        /// </summary>
        protected abstract void StopImpl(UpdateResult result);

        void SetActivationStage(INotifications notifs, ActivationStage newStage)
        {
            if (activationStage != newStage)
            {
                activationStage = newStage;
                OnActivationStageChanged(notifs);
            }
        }

        protected ActivationStage InternalActivationStage
        {
            get { return activationStage; }
        }

        protected virtual void OnActivationStageChanged(INotifications notifs)
        {
        }

        /// <summary>
        /// Raised when an automatic recovery (re-activation) of this resource failed.
        /// </summary>
        public event EventHandler<RecoveryFailedEventArgs> RecoveryFailed;

        void RaiseRecoveryFailed(ActivationException e)
        {
            if (RecoveryFailed != null)
                RecoveryFailed(this, new RecoveryFailedEventArgs(e));
        }

        class AwaitingStart
        {
            readonly IList<TaskCompletionSource<object>> completionSources = new List<TaskCompletionSource<object>>();

            internal async Task Await()
            {
                var c = new TaskCompletionSource<object>();
                completionSources.Add(c);
                await c.Task;
            }

            internal void Complete(Exception exc)
            {
                foreach (var c in completionSources)
                {
                    if (exc == null)
                        c.SetResult(null);
                    else if (exc is OperationCanceledException)
                        c.SetCanceled();
                    else
                        c.SetException(exc);
                }

                completionSources.Clear();
            }
        }

        protected enum ActivationStage
        {
            Idle,
            Starting,
            Started
        }
    }

    public class ActivationException : Exception
    {
        public ActivationException(Exception innerException)
            : base(innerException.Message, innerException)
        {
        }

        public ActivationException(string message)
            : base(message)
        {
        }
    }

    public class RecoveryFailedEventArgs : EventArgs
    {
        private readonly ActivationException exception;

        public RecoveryFailedEventArgs(ActivationException exception)
        {
            this.exception = exception;
        }

        public ActivationException ActivationException
        {
            get { return exception; }
        }
    }
}
