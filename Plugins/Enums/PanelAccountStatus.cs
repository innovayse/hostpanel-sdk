namespace Hostpanel.Sdk.Plugins.Enums;

/// <summary>The state of an account on a hosting panel, as a listing for reconciliation reports it. The values start at 1 and 0 is not valid.</summary>
public enum PanelAccountStatus
{
    /// <summary>The account is active.</summary>
    Active = 1,

    /// <summary>The account is suspended.</summary>
    Suspended = 2,
}
