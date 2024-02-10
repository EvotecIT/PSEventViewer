using System.Collections.Generic;
using System;
using System.Management.Automation;
using System.Threading.Tasks;
using System.Collections;
using System.Management.Automation.Language;
using System.Linq;

namespace PSEventViewer.PowerShell {

    [Cmdlet(VerbsCommon.Find, "WinEvent", DefaultParameterSetName = "GenericEvents")]
    public sealed class CmdletFindEvent : AsyncPSCmdlet {
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "RecordId")]
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "GenericEvents")] public string LogName;
        [Parameter(Mandatory = false, Position = 1, ParameterSetName = "GenericEvents")] public List<int> EventId = null;

        [Alias("RecordId")]
        [Parameter(Mandatory = false, Position = 1, ParameterSetName = "RecordId")] public List<long> EventRecordId = null;

        [Alias("ComputerName", "ServerName")]
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
        public List<string> MachineName;

        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")] public string ProviderName;
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")] public Keywords? Keywords;
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")] public Level? Level;
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")] public DateTime? StartTime;
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")] public DateTime? EndTime;
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")] public string UserId;

        [Parameter(Mandatory = false, ParameterSetName = "RecordId")]
        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        public int NumberOfThreads { get; set; } = 8;

        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        public int MaxEvents = 0;

        [Parameter(Mandatory = false, ParameterSetName = "GenericEvents")]
        [Parameter(Mandatory = false, ParameterSetName = "NamedEvents")]
        public ParallelOption ParallelOption { get; set; } = ParallelOption.Parallel;

        //[Parameter(Mandatory = true, ParameterSetName = "NamedEvents")]
        //[ArgumentCompleter(typeof(NamedEventsCompleter))] public string[] Type;
        //[Parameter(Mandatory = true, ParameterSetName = "NamedEvents")] public string[] Type;
        //[Parameter(Mandatory = true, ParameterSetName = "NamedEvents")] public List<NamedEvents> Type;

        [Parameter(Mandatory = true, ParameterSetName = "NamedEvents")] public NamedEvents[] Type;

        protected override Task BeginProcessingAsync() {
            // Initialize the logger to be able to see verbose, warning, debug, error, progress, and information messages.
            var internalLogger = new InternalLogger(false);
            var internalLoggerPowerShell = new InternalLoggerPowerShell(internalLogger, this.WriteVerbose, this.WriteWarning, this.WriteDebug, this.WriteError, this.WriteProgress, this.WriteInformation);
            var eventSearching = new EventSearching(internalLogger);
            return Task.CompletedTask;
        }
        protected override Task ProcessRecordAsync() {
            if (Type != null) {
                // let's find the events prepared for search
                //var types = ParseNamedEvents(Type);
                List<NamedEvents> typeList = Type.ToList();
                foreach (var eventObject in EventSearchingTargeted.FindEventsByNamedEvents(typeList, MachineName)) {
                    WriteObject(eventObject);
                }
            } else {
                // Lets find the events by generic log name, event id, machine name, provider name, keywords, level, start time, end time, user id, and max events.
                if (ParallelOption == ParallelOption.Disabled) {
                    if (MachineName == null) {
                        foreach (var eventObject in EventSearching.QueryLog(LogName, EventId, null, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId)) {
                            WriteObject(eventObject);
                        }
                    } else {
                        foreach (var machine in MachineName) {
                            foreach (var eventObject in EventSearching.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId)) {
                                WriteObject(eventObject);
                            }
                        }
                    }
                } else if (ParallelOption == ParallelOption.Parallel) {
                    foreach (var eventObject in EventSearching.QueryLogsParallel(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, NumberOfThreads, EventRecordId)) {
                        WriteObject(eventObject);
                    }
                }
                //else if (Mode == Modes.ParallelForEach) {
                //    var options = new ParallelOptions { MaxDegreeOfParallelism = NumberOfThreads };
                //    Parallel.ForEach(MachineName, options, machine => {
                //        foreach (var eventObject in EventSearching.QueryLog(LogName, EventId, machine, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, EventRecordId)) {
                //            WriteObject(eventObject);
                //        }
                //    });
                //} else if (Mode == Modes.ParallelForEachBuiltin) {
                //    foreach (var eventObject in EventSearching.QueryLogsParallelForEach(LogName, EventId, MachineName, ProviderName, Keywords, Level, StartTime, EndTime, UserId, MaxEvents, NumberOfThreads)) {
                //        WriteObject(eventObject);
                //    }
                //}
            }

            return Task.CompletedTask;
        }

        ///// <summary>
        ///// Parses string to NamedEvents
        ///// </summary>
        ///// <param name="typeStrings"></param>
        ///// <returns></returns>
        //private List<NamedEvents> ParseNamedEvents(string[] typeStrings) {
        //    var namedEvents = new List<NamedEvents>();
        //    foreach (var typeString in typeStrings) {
        //        if (Enum.TryParse(typeString, out NamedEvents namedEvent)) {
        //            namedEvents.Add(namedEvent);
        //        } else {
        //            // Handle invalid values here
        //        }
        //    }
        //    return namedEvents;
        //}

        ///// <summary>
        ///// Provides auto-completion for NamedEvents from strings
        ///// </summary>
        //public class NamedEventsCompleter : IArgumentCompleter {
        //    public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters) {
        //        foreach (var namedEvent in Enum.GetNames(typeof(NamedEvents))) {
        //            if (namedEvent.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase)) {
        //                yield return new CompletionResult(namedEvent);
        //            }
        //        }
        //    }
        //}
    }
}