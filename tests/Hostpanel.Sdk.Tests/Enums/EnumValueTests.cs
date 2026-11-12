using Hostpanel.Sdk.Plugins.Enums;

namespace Hostpanel.Sdk.Tests.Enums;

/// <summary>
/// Pins every value of the enums that cross the plugin boundary as numbers. A plugin built on an earlier version carries
/// these numbers in its binary, so a value may be appended but never renumbered or removed, and zero is never valid.
/// </summary>
public sealed class EnumValueTests
{
    /// <summary>The panel refusal reasons keep the values of 3.5.0 and the two appended for virtual servers follow them.</summary>
    [Fact]
    public void PanelRefusalReason_Values_ArePinned()
    {
        var values = Enum.GetValues<PanelRefusalReason>();

        values.Select(value => (int)value).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9);
        values.Select(value => value.ToString()).Should().Equal(
            "PackageUnknown", "UsernameInvalid", "DomainInvalid", "PasswordRejected", "LimitReached", "AccessDenied", "Other", "ImageUnknown", "NameInvalid");
        Enum.IsDefined((PanelRefusalReason)0).Should().BeFalse();
    }

    /// <summary>The registrar refusal reasons are pinned.</summary>
    [Fact]
    public void RegistrarRefusalReason_Values_ArePinned()
    {
        var values = Enum.GetValues<RegistrarRefusalReason>();

        values.Select(value => (int)value).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        values.Select(value => value.ToString()).Should().Equal(
            "DomainUnavailable",
            "TldNotSupported",
            "EppCodeInvalid",
            "TransferNotAllowed",
            "ContactInvalid",
            "NameserverInvalid",
            "DnsRecordInvalid",
            "InsufficientFunds",
            "AccessDenied",
            "Other");
        Enum.IsDefined((RegistrarRefusalReason)0).Should().BeFalse();
    }

    /// <summary>The DNS record types are pinned.</summary>
    [Fact]
    public void RegistrarDnsRecordType_Values_ArePinned()
    {
        var values = Enum.GetValues<RegistrarDnsRecordType>();

        values.Select(value => (int)value).Should().Equal(1, 2, 3, 4, 5, 6, 7);
        values.Select(value => value.ToString()).Should().Equal("A", "Aaaa", "Cname", "Mx", "Txt", "Ns", "Srv");
        Enum.IsDefined((RegistrarDnsRecordType)0).Should().BeFalse();
    }

    /// <summary>The power states of a virtual server are pinned.</summary>
    [Fact]
    public void VirtualServerPowerState_Values_ArePinned()
    {
        var values = Enum.GetValues<VirtualServerPowerState>();

        values.Select(value => (int)value).Should().Equal(1, 2, 3, 4);
        values.Select(value => value.ToString()).Should().Equal("Running", "Stopped", "Suspended", "Provisioning");
        Enum.IsDefined((VirtualServerPowerState)0).Should().BeFalse();
    }

    /// <summary>The payment states and the host access failures were released with implicit values from zero; they are explicit now and unchanged.</summary>
    [Fact]
    public void GatewayPaymentStateAndHostAccessFailure_Values_ArePinned()
    {
        Enum.GetValues<GatewayPaymentState>().Select(value => (int)value).Should().Equal(0, 1, 2);
        Enum.GetValues<GatewayPaymentState>().Select(value => value.ToString()).Should().Equal("Pending", "Paid", "Declined");
        Enum.GetValues<PluginHostAccessFailure>().Select(value => (int)value).Should().Equal(0, 1, 2, 3, 4);
        Enum.GetValues<PluginHostAccessFailure>().Select(value => value.ToString())
            .Should().Equal("NotAllowed", "NotFound", "Unavailable", "LimitReached", "ServerUnavailable");
    }

    /// <summary>The outcome enums shared by every contract are pinned too, because the new contracts reuse them.</summary>
    [Fact]
    public void SharedOutcomes_Values_ArePinned()
    {
        Enum.GetValues<PanelOperationOutcome>().Select(value => (int)value).Should().Equal(1, 2, 3, 4, 5);
        Enum.GetValues<PanelConnectionOutcome>().Select(value => (int)value).Should().Equal(1, 2, 3, 4);
        Enum.GetValues<PluginType>().Select(value => (int)value).Should().Equal(0, 1, 2, 3, 4, 5, 6, 7);
    }
}
