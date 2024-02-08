﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PSEventViewer {
    public class EventObjectSlim {
        internal EventObject _eventObject;
        public int EventID; // = _eventObject.Id;
        public long? RecordID; // = _eventObject.RecordId;
        public string GatheredFrom; // = _eventObject.MachineName;
        public string GatheredLogName; // = _eventObject.LogName;


        public EventObjectSlim(EventObject eventObject) {
            _eventObject = eventObject;
            EventID = _eventObject.Id;
            RecordID = _eventObject.RecordId;
            GatheredFrom = _eventObject.QueriedMachine;
            GatheredLogName = _eventObject.ContainerLog;
        }

        internal static string ConvertToObjectAffected(EventObject eventObject) {
            if (eventObject.Data.ContainsKey("TargetUserName") && eventObject.Data.ContainsKey("TargetDomainName")) {
                return eventObject.Data["TargetDomainName"] + "\\" + eventObject.Data["TargetUserName"];
            } else if (eventObject.Data.ContainsKey("TargetUserName")) {
                return eventObject.Data["TargetUserName"];
            } else {
                return "";
            }
        }
        internal static string ConvertToSamAccountName(EventObject eventObject) {
            if (eventObject.Data.ContainsKey("SamAccountName")) {
                return eventObject.Data["SamAccountName"];
            } else {
                return "";
            }
        }
    }
}