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

        values.Select(value => (int)value).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8);
        values.Select(value => value.ToString()).Should().Equal("A", "Aaaa", "Cname", "Mx", "Txt", "Ns", "Srv", "Caa");
        Enum.IsDefined((RegistrarDnsRecordType)0).Should().BeFalse();
    }

    /// <summary>The reasons a name cannot be registered are pinned.</summary>
    [Fact]
    public void RegistrarAvailabilityReason_Values_ArePinned()
    {
        var values = Enum.GetValues<RegistrarAvailabilityReason>();

        values.Select(value => (int)value).Should().Equal(1, 2, 3, 4, 5, 6);
        values.Select(value => value.ToString()).Should().Equal("Registered", "Reserved", "TldNotSupported", "InvalidName", "Premium", "Other");
        Enum.IsDefined((RegistrarAvailabilityReason)0).Should().BeFalse();
    }

    /// <summary>The kinds of a saved payment method are pinned; the card is 0, the default of a method built before kinds existed.</summary>
    [Fact]
    public void SavedMethodKind_Values_ArePinned()
    {
        var values = Enum.GetValues<SavedMethodKind>();

        values.Select(value => (int)value).Should().Equal(0, 1, 2, 3);
        values.Select(value => value.ToString()).Should().Equal("Card", "BankAccount", "Mandate", "Wallet");
        default(SavedMethodKind).Should().Be(SavedMethodKind.Card);
    }

    /// <summary>The enums of a virtual server firewall rule are pinned, and zero is never valid.</summary>
    [Fact]
    public void VirtualServerFirewall_Values_ArePinned()
    {
        Enum.GetValues<VirtualServerFirewallDirection>().Select(value => (int)value).Should().Equal(1, 2);
        Enum.GetValues<VirtualServerFirewallAction>().Select(value => (int)value).Should().Equal(1, 2);
        Enum.GetValues<VirtualServerFirewallProtocol>().Select(value => (int)value).Should().Equal(1, 2, 3, 4);
        Enum.IsDefined((VirtualServerFirewallDirection)0).Should().BeFalse();
        Enum.IsDefined((VirtualServerFirewallAction)0).Should().BeFalse();
        Enum.IsDefined((VirtualServerFirewallProtocol)0).Should().BeFalse();
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

    /// <summary>The payment states were released with implicit values from zero; they are explicit now and unchanged.</summary>
    [Fact]
    public void GatewayPaymentState_Values_ArePinned()
    {
        Enum.GetValues<GatewayPaymentState>().Select(value => (int)value).Should().Equal(0, 1, 2);
        Enum.GetValues<GatewayPaymentState>().Select(value => value.ToString()).Should().Equal("Pending", "Paid", "Declined");
    }

    /// <summary>The host access failures were released with implicit values from zero; they are explicit now and unchanged.</summary>
    [Fact]
    public void PluginHostAccessFailure_Values_ArePinned()
    {
        Enum.GetValues<PluginHostAccessFailure>().Select(value => (int)value).Should().Equal(0, 1, 2, 3, 4, 5, 6);
        Enum.GetValues<PluginHostAccessFailure>().Select(value => value.ToString())
            .Should().Equal("NotAllowed", "NotFound", "Unavailable", "LimitReached", "ServerUnavailable", "InvalidRequest", "Conflict");
    }

    /// <summary>The outcome enums shared by every contract are pinned too, because the new contracts reuse them.</summary>
    [Fact]
    public void SharedOutcomes_Values_ArePinned()
    {
        Enum.GetValues<PanelOperationOutcome>().Select(value => (int)value).Should().Equal(1, 2, 3, 4, 5, 6);
        Enum.GetValues<PanelAccountLevel>().Select(value => (int)value).Should().Equal(1, 2);
        Enum.GetValues<PanelConnectionOutcome>().Select(value => (int)value).Should().Equal(1, 2, 3, 4);
        Enum.GetValues<PluginType>().Select(value => (int)value).Should().Equal(0, 1, 2, 3, 4, 5, 6, 7);
    }

    /// <summary>The registrar connection roles are explicit values from 1, and zero is not a role.</summary>
    [Fact]
    public void RegistrarConnectionRole_Values_ArePinned()
    {
        var values = Enum.GetValues<RegistrarConnectionRole>();

        values.Select(value => (int)value).Should().Equal(1, 2, 3, 4, 5);
        values.Select(value => value.ToString()).Should().Equal("AccountName", "ApiKey", "ApiUrl", "ClientIp", "TestMode");
        Enum.IsDefined((RegistrarConnectionRole)0).Should().BeFalse();
    }

    /// <summary>The domain field enums are explicit values and zero is none of them; the operations are bit flags.</summary>
    [Fact]
    public void DomainFieldEnums_Values_ArePinned()
    {
        Enum.GetValues<PluginDomainFieldType>().Select(value => value.ToString()).Should().Equal("Text", "Textarea", "Select", "Date", "Number", "Url");
        Enum.GetValues<PluginDomainFieldType>().Select(value => (int)value).Should().Equal(1, 2, 3, 4, 5, 6);
        Enum.GetValues<PluginDomainHolder>().Select(value => value.ToString()).Should().Equal("Any", "Individual", "Organization");
        Enum.GetValues<PluginDomainHolder>().Select(value => (int)value).Should().Equal(1, 2, 3);
        Enum.GetValues<PluginDomainFieldViolationReason>().Select(value => value.ToString())
            .Should().Equal("Unknown", "Required", "Pattern", "Length", "Option", "Format");
        Enum.GetValues<PluginDomainFieldViolationReason>().Select(value => (int)value).Should().Equal(1, 2, 3, 4, 5, 6);
        Enum.GetValues<PluginDomainOperations>().Select(value => (int)value).Should().Equal(0, 1, 2, 4, 7);
        Enum.IsDefined((PluginDomainFieldType)0).Should().BeFalse();
        Enum.IsDefined((PluginDomainHolder)0).Should().BeFalse();
        Enum.IsDefined((PluginDomainFieldViolationReason)0).Should().BeFalse();
        PluginDomainOperations.All.Should().Be(PluginDomainOperations.Register | PluginDomainOperations.Transfer | PluginDomainOperations.ContactChange);
    }
}
