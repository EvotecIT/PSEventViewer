using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace PSEventViewer;

#nullable enable

/// <summary>
/// An abstract base class for asynchronous PowerShell cmdlets.
/// </summary>
public abstract class AsyncPSCmdlet : PSCmdlet, IDisposable {
    private const int PipelineBufferCapacity = 64;
    /// <summary>
    /// Defines the types of pipelines used in the cmdlet.
    /// </summary>
    private enum PipelineType {
        Output,
        OutputEnumerate,
        Error,
        Warning,
        Verbose,
        Debug,
        Information,
        Progress,
        ShouldProcess,
    }

    /// <summary>
    /// Cancels the processing of the cmdlet.
    /// </summary>
    private CancellationTokenSource _cancelSource = new();
    private InternalLogger? _eventViewerLogger;

    private BlockingCollection<(object?, PipelineType)>? _currentOutPipe;
    private BlockingCollection<object?>? _currentReplyPipe;

    /// <summary>
    /// Gets the cancellation token that is triggered when the cmdlet is stopped.
    /// </summary>
    protected internal CancellationToken CancelToken { get => _cancelSource.Token; }

#if NET8_0_OR_GREATER
    /// <summary>
    /// Exposes a stopping token compatible with newer PowerShell versions.
    /// </summary>
    protected CancellationToken StoppingToken => CancelToken;
#endif

    /// <summary>
    /// Begins processing the cmdlet asynchronously.
    /// </summary>
    protected override void BeginProcessing()
        => RunBlockInAsync(BeginProcessingAsync);

    /// <summary>
    /// Override this method to implement asynchronous begin processing logic.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task BeginProcessingAsync()
        => Task.CompletedTask;

    /// <summary>
    /// Processes a record asynchronously.
    /// </summary>
    protected override void ProcessRecord()
        => RunBlockInAsync(ProcessRecordAsync);

    /// <summary>
    /// Override this method to implement asynchronous record processing logic.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task ProcessRecordAsync()
        => Task.CompletedTask;

    /// <summary>
    /// Ends processing the cmdlet asynchronously.
    /// </summary>
    protected override void EndProcessing()
        => RunBlockInAsync(EndProcessingAsync);

    /// <summary>
    /// Override this method to implement asynchronous end processing logic.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected virtual Task EndProcessingAsync()
        => Task.CompletedTask;

    /// <summary>
    /// Stops the processing of the cmdlet.
    /// </summary>
    protected override void StopProcessing()
        => _cancelSource?.Cancel();

    /// <summary>
    /// Runs the specified task asynchronously and handles the output and reply pipelines.
    /// </summary>
    /// <param name="task">The task to run asynchronously.</param>
    private void RunBlockInAsync(Func<Task> task) {
        using BlockingCollection<(object?, PipelineType)> outPipe = new(PipelineBufferCapacity);
        using BlockingCollection<object?> replyPipe = new(1);
        Task blockTask = Task.Run(async () => {
            try {
                _currentOutPipe = outPipe;
                _currentReplyPipe = replyPipe;
                if (_eventViewerLogger != null) {
                    Settings._logger = _eventViewerLogger;
                }
                await task();
            } finally {
                _currentOutPipe = null;
                _currentReplyPipe = null;
                outPipe.CompleteAdding();
                replyPipe.CompleteAdding();
            }
        });

        foreach ((object? data, PipelineType pipelineType) in outPipe.GetConsumingEnumerable()) {
            switch (pipelineType) {
                case PipelineType.Output:
                    base.WriteObject(data);
                    break;

                case PipelineType.OutputEnumerate:
                    base.WriteObject(data, true);
                    break;

                case PipelineType.Error:
                    base.WriteError((ErrorRecord)data!);
                    break;

                case PipelineType.Warning:
                    base.WriteWarning((string)data!);
                    break;

                case PipelineType.Verbose:
                    base.WriteVerbose((string)data!);
                    break;

                case PipelineType.Debug:
                    base.WriteDebug((string)data!);
                    break;

                case PipelineType.Information:
                    base.WriteInformation((InformationRecord)data!);
                    break;

                case PipelineType.Progress:
                    base.WriteProgress((ProgressRecord)data!);
                    break;

                case PipelineType.ShouldProcess:
                    (string target, string action) = (ValueTuple<string, string>)data!;
                    bool res = base.ShouldProcess(target, action);
                    replyPipe.Add(res);
                    break;
            }
        }

        blockTask.GetAwaiter().GetResult();
        if (_cancelSource.IsCancellationRequested) {
            throw new PipelineStoppedException();
        }
    }

    /// <summary>
    /// Keeps the EventViewerX logger attached across PowerShell lifecycle phases, which run in separate execution contexts.
    /// </summary>
    /// <param name="logger">Logger connected to this cmdlet's PowerShell streams.</param>
    protected void SetEventViewerLogger(InternalLogger logger) {
        _eventViewerLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        Settings._logger = logger;
    }

    /// <summary>
    /// Determines whether the cmdlet should continue processing.
    /// </summary>
    /// <param name="target">The target of the operation.</param>
    /// <param name="action">The action to be performed.</param>
    /// <returns>True if the cmdlet should continue processing; otherwise, false.</returns>
    public new bool ShouldProcess(string target, string action) {
        AddToOutputPipe(((target, action), PipelineType.ShouldProcess));
        return (bool)_currentReplyPipe?.Take(CancelToken)!;
    }

    /// <summary>
    /// Writes an object to the output pipeline.
    /// </summary>
    /// <param name="sendToPipeline">The object to send to the pipeline.</param>
    public new void WriteObject(object? sendToPipeline) => WriteObject(sendToPipeline, false);

    /// <summary>
    /// Writes an object to the output pipeline, optionally enumerating collections.
    /// </summary>
    /// <param name="sendToPipeline">The object to send to the pipeline.</param>
    /// <param name="enumerateCollection">If true, enumerates the collection.</param>
    public new void WriteObject(object? sendToPipeline, bool enumerateCollection) {
        AddToOutputPipe(
            (sendToPipeline, enumerateCollection ? PipelineType.OutputEnumerate : PipelineType.Output));
    }

    /// <summary>
    /// Writes an error record to the error pipeline.
    /// </summary>
    /// <param name="errorRecord">The error record to write.</param>
    public new void WriteError(ErrorRecord errorRecord) {
        AddToOutputPipe((errorRecord, PipelineType.Error));
    }

    /// <summary>
    /// Writes a warning message to the warning pipeline.
    /// </summary>
    /// <param name="message">The warning message to write.</param>
    public new void WriteWarning(string message) {
        AddToOutputPipe((message, PipelineType.Warning));
    }

    /// <summary>
    /// Writes a verbose message to the verbose pipeline.
    /// </summary>
    /// <param name="message">The verbose message to write.</param>
    public new void WriteVerbose(string message) {
        AddToOutputPipe((message, PipelineType.Verbose));
    }

    /// <summary>
    /// Writes a debug message to the debug pipeline.
    /// </summary>
    /// <param name="message">The debug message to write.</param>
    public new void WriteDebug(string message) {
        AddToOutputPipe((message, PipelineType.Debug));
    }

    /// <summary>
    /// Writes an information record to the information pipeline.
    /// </summary>
    /// <param name="informationRecord">The information record to write.</param>
    public new void WriteInformation(InformationRecord informationRecord) {
        AddToOutputPipe((informationRecord, PipelineType.Information));
    }

    /// <summary>
    /// Writes a progress record to the progress pipeline.
    /// </summary>
    /// <param name="progressRecord">The progress record to write.</param>
    public new void WriteProgress(ProgressRecord progressRecord) {
        AddToOutputPipe((progressRecord, PipelineType.Progress));
    }

    private void AddToOutputPipe((object?, PipelineType) entry) {
        ThrowIfStopped();
        _currentOutPipe?.Add(entry, CancelToken);
    }

    /// <summary>
    /// Throws a <see cref="PipelineStoppedException"/> if the cmdlet has been stopped.
    /// </summary>
    internal void ThrowIfStopped() {
        if (_cancelSource.IsCancellationRequested) {
            throw new PipelineStoppedException();
        }
    }

    /// <summary>
    /// Disposes the resources used by the cmdlet.
    /// </summary>
    public void Dispose() {
        _cancelSource?.Dispose();
    }
}
