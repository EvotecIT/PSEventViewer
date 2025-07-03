namespace EventViewerX {
    /// <summary>
    /// Defines the named events that can be searched for
    /// </summary>
    public enum NamedEvents {
        /// <summary>
        /// Active Directory computer account created or modified
        /// </summary>
        ADComputerCreateChange,
      
        /// <summary>
        /// Active Directory computer account deleted
        /// </summary>
        ADComputerDeleted,
      
        /// <summary>
        /// Detailed changes for computer accounts
        /// </summary>
        ADComputerChangeDetailed,

        /// <summary>
        /// Modifications to group membership
        /// </summary>
        ADGroupMembershipChange,
      
        /// <summary>
        /// Group membership enumeration events
        /// </summary>
        ADGroupEnumeration,
      
        /// <summary>
        /// Active Directory group created, changed or deleted
        /// </summary>
        ADGroupChange,
      
        /// <summary>
        /// Group creation or deletion events
        /// </summary>
        ADGroupCreateDelete,
      
        /// <summary>
        /// Detailed changes for group objects
        /// </summary>
        ADGroupChangeDetailed,

        /// <summary>
        /// Changes to Group Policy Objects
        /// </summary>
        ADGroupPolicyChanges,
      
        /// <summary>
        /// Edits to Group Policy Objects
        /// </summary>
        ADGroupPolicyEdits,
      
        /// <summary>
        /// Links or unlinks of Group Policy Objects
        /// </summary>
        ADGroupPolicyLinks,

        /// <summary>
        /// Group Policy Object created
        /// </summary>
        GpoCreated,
      
        /// <summary>
        /// Group Policy Object deleted
        /// </summary>
        GpoDeleted,
      
        /// <summary>
        /// Group Policy Object modified
        /// </summary>
        GpoModified,

        /// <summary>
        /// Summary of LDAP binding activity
        /// </summary>
        ADLdapBindingSummary,
      
        /// <summary>
        /// Detailed LDAP binding information
        /// </summary>
        ADLdapBindingDetails,
      
        /// <summary>
        /// Active Directory user account created or changed
        /// </summary>
        ADUserCreateChange,
      
        /// <summary>
        /// User account enabled, disabled, unlocked or deleted
        /// </summary>
        ADUserStatus,
      
        /// <summary>
        /// Detailed changes for user accounts
        /// </summary>
        ADUserChangeDetailed,
      
        /// <summary>
        /// User account lockout events
        /// </summary>
        ADUserLockouts,
      
        /// <summary>
        /// Successful user logon
        /// </summary>
        ADUserLogon,
      
        /// <summary>
        /// NTLMv1 logon tracking
        /// </summary>
        ADUserLogonNTLMv1,
      
        /// <summary>
        /// Kerberos authentication ticket requests
        /// </summary>
        ADUserLogonKerberos,
      
        /// <summary>
        /// Failed user logon attempts
        /// </summary>
        ADUserLogonFailed,
      
        /// <summary>
        /// User account unlocked
        /// </summary>
        ADUserUnlocked,
      
        /// <summary>
        /// Special privileges assigned to new logon
        /// </summary>
        ADUserPrivilegeUse,
      
        /// <summary>
        /// User rights assigned or removed
        /// </summary>
        ADUserRightsAssignment,
      
        /// <summary>
        /// Kerberos TGT requests
        /// </summary>
        KerberosTGTRequest,

        /// <summary>
        /// Kerberos service ticket requests and renewals
        /// </summary>
        KerberosServiceTicket,
      
        /// <summary>
        /// Kerberos ticket request failures
        /// </summary>
        KerberosTicketFailure,
      
        /// <summary>
        /// Kerberos policy changed
        /// </summary>
        KerberosPolicyChange,
      
        /// <summary>
        /// Organizational unit created, deleted or moved
        /// </summary>
        ADOrganizationalUnitChangeDetailed,
      
        /// <summary>
        /// Detailed changes for other directory objects
        /// </summary>
        ADOtherChangeDetailed,

        /// <summary>
        /// SMB1 access audit information
        /// </summary>
        ADSMBServerAuditV1,

        /// <summary>
        /// Security log cleared
        /// </summary>
        LogsClearedSecurity,
      
        /// <summary>
        /// Application or system log cleared
        /// </summary>
        LogsClearedOther,
      
        /// <summary>
        /// Security log is full
        /// </summary>
        LogsFullSecurity,

        /// <summary>
        /// NPS granted or denied network access
        /// </summary>
        NetworkAccessAuthenticationPolicy,

        /// <summary>
        /// Certificate issued by Certificate Authority
        /// </summary>
        CertificateIssued,

        /// <summary>
        /// System audit policy was changed
        /// </summary>
        AuditPolicyChange,

        /// <summary>
        /// Windows Firewall rule modified
        /// </summary>
        FirewallRuleChange,

        /// <summary>
        /// DHCP lease creation event
        /// </summary>
        DhcpLeaseCreated,

        /// <summary>
        /// BitLocker protection key changed or backed up
        /// </summary>
        BitLockerKeyChange,

        /// <summary>
        /// BitLocker protection was suspended
        /// </summary>
        BitLockerSuspended,

        /// <summary>
        /// External device recognized by the system
        /// </summary>
        DeviceRecognized,

        /// <summary>
        /// Device was disabled
        /// </summary>
        DeviceDisabled,

        /// <summary>
        /// Object deleted
        /// </summary>
        ObjectDeletion,

        /// <summary>
        /// Scheduled task deleted
        /// </summary>
        ScheduledTaskDeleted,
        /// <summary>
        /// Scheduled task created
        /// </summary>
        ScheduledTaskCreated,

        /// <summary>
        /// Unexpected system shutdown
        /// </summary>
        OSCrash,

        /// <summary>
        /// Bugcheck event describing a system crash
        /// </summary>
        OSBugCheck,

        /// <summary>
        /// System start-up, shutdown or crash events
        /// </summary>
        OSStartupShutdownCrash,
      
        /// <summary>
        /// System time changed
        /// </summary>
        OSTimeChange,
      
        /// <summary>
        /// Windows Update installation failure
        /// </summary>
        WindowsUpdateFailure,
      
        /// <summary>
        /// Group Policy client-side processing events from Application log
        /// </summary>
        ClientGroupPoliciesApplication,
      
        /// <summary>
        /// Group Policy client-side processing events from System log
        /// </summary>
        ClientGroupPoliciesSystem,
      
        /// <summary>
        /// Hyper-V virtual machine was shut down
        /// </summary>
        HyperVVmShutdown,
      
        /// <summary>
        /// Hyper-V virtual machine started
        /// </summary>
        HyperVVirtualMachineStarted,

        /// <summary>
        /// IIS site failed to register binding (W3SVC event 1007)
        /// </summary>
        IisSiteBindingFailure,
      
        /// <summary>
        /// Hyper-V checkpoint created
        /// </summary>
        HyperVCheckpointCreated,
      
        /// <summary>
        /// IIS website stopped
        /// </summary>
        IISSiteStopped,
      
        /// <summary>
        /// Exchange mailbox database mounted successfully
        /// </summary>
        ExchangeDatabaseMounted,
      
        /// <summary>
        /// DFS Replication partner error
        /// </summary>
        DfsReplicationError,

        /// <summary>
        /// SQL Server database created
        /// </summary>
        SqlDatabaseCreated,

        /// <summary>
        /// Password synchronization failure
        /// </summary>
        PasswordSyncFailed,
    }
}