using Hostpanel.Sdk.Plugins.Enums;
using Hostpanel.Sdk.Plugins.Manifest;
using Hostpanel.Sdk.Plugins.Registrar;
using Hostpanel.Sdk.Tests.Fakes;

namespace Hostpanel.Sdk.Tests.Registrar;

/// <summary>Tests of the optional members and interfaces added to the registrar contract: their defaults, their masking and the additive guarantee.</summary>
public sealed class RegistrarAdditiveShapeTests
{
    /// <summary>The value an extended attribute carries in the tests, which must never be printed.</summary>
    private const string AttributeValue = "company-secret-303";

    /// <summary>A transfer built the old way asks for one year, so a plugin that reads the years gets a usable value.</summary>
    [Fact]
    public void TransferContext_Years_DefaultsToOne()
    {
        RegistrarFixtures.Transfer.Years.Should().Be(1);
        (RegistrarFixtures.Transfer with { Years = 2 }).Years.Should().Be(2);
    }

    /// <summary>A context built the old way carries no role contact, no attribute and no agreement, so a plugin that ignores them behaves as before.</summary>
    [Fact]
    public void Contexts_BuiltTheOldWay_CarryNoOptionalData()
    {
        RegistrarFixtures.Register.RoleContacts.Should().Be(RegistrarRoleContacts.None);
        RegistrarFixtures.Transfer.RoleContacts.Should().Be(RegistrarRoleContacts.None);
        RegistrarFixtures.ContactChange.RoleContacts.Should().Be(RegistrarRoleContacts.None);
        RegistrarFixtures.Register.ExtendedAttributes.Should().BeEmpty();
        RegistrarFixtures.Transfer.ExtendedAttributes.Should().BeEmpty();
        RegistrarFixtures.ContactChange.ExtendedAttributes.Should().BeEmpty();
        RegistrarFixtures.Register.Agreement.Should().BeNull();
        RegistrarFixtures.Transfer.Agreement.Should().BeNull();
    }

    /// <summary>The constructors of the contexts keep the parameters they had, so a host or a test built on an earlier version still binds to them.</summary>
    [Fact]
    public void Contexts_KeepTheirOriginalConstructors()
    {
        typeof(RegistrarRegisterContext).GetConstructors().Select(constructor => constructor.GetParameters().Length).Should().Contain(7);
        typeof(RegistrarTransferContext).GetConstructors().Select(constructor => constructor.GetParameters().Length).Should().Contain(5);
        typeof(RegistrarContactContext).GetConstructors().Select(constructor => constructor.GetParameters().Length).Should().Contain(3);
        typeof(RegistrarDnsRecord).GetConstructors().Select(constructor => constructor.GetParameters().Length).Should().Contain(6);
        typeof(RegistrarAvailability).GetConstructors().Select(constructor => constructor.GetParameters().Length).Should().Contain(1);
    }

    /// <summary>A role without a contact falls back to the registrant, and a role with one keeps it.</summary>
    [Fact]
    public void RoleContacts_FallBackToRegistrant()
    {
        var registrant = RegistrarFixtures.Contact;
        var contacts = new RegistrarRoleContacts(null, RegistrarFixtures.TechnicalContact, null);

        contacts.AdminOr(registrant).Should().BeSameAs(registrant);
        contacts.TechnicalOr(registrant).Should().BeSameAs(RegistrarFixtures.TechnicalContact);
        contacts.BillingOr(registrant).Should().BeSameAs(registrant);
        RegistrarRoleContacts.None.AdminOr(registrant).Should().BeSameAs(registrant);
    }

    /// <summary>The fallback needs a registrant, so a missing one is a programming error and not a silent null.</summary>
    [Fact]
    public void RoleContacts_FallbackWithoutRegistrant_Throws()
    {
        var admin = () => RegistrarRoleContacts.None.AdminOr(null!);
        var technical = () => RegistrarRoleContacts.None.TechnicalOr(null!);
        var billing = () => RegistrarRoleContacts.None.BillingOr(null!);

        admin.Should().Throw<ArgumentNullException>();
        technical.Should().Throw<ArgumentNullException>();
        billing.Should().Throw<ArgumentNullException>();
    }

    /// <summary>The role contacts print masked inside a context, because a contact masks itself.</summary>
    [Fact]
    public void RoleContacts_InsideContext_DoNotLeak()
    {
        var register = RegistrarFixtures.Register with { RoleContacts = new RegistrarRoleContacts(null, RegistrarFixtures.TechnicalContact, null) };

        register.ToString().Should().NotContain("Babbage").And.NotContain("charles@example.test");
    }

    /// <summary>The extended attributes and the agreement print masked in the register and transfer contexts, and the attributes in the contact update.</summary>
    [Fact]
    public void ExtendedAttributesAndAgreement_ToString_AreMasked()
    {
        var attributes = new Dictionary<string, string> { ["companyNumber"] = AttributeValue };
        var register = RegistrarFixtures.Register with { ExtendedAttributes = attributes, Agreement = RegistrarFixtures.Agreement };
        var transfer = RegistrarFixtures.Transfer with { ExtendedAttributes = attributes, Agreement = RegistrarFixtures.Agreement };
        var contactChange = RegistrarFixtures.ContactChange with { ExtendedAttributes = attributes };

        foreach (var printed in new[] { register.ToString(), transfer.ToString(), contactChange.ToString() })
        {
            printed.Should().NotContain(AttributeValue).And.NotContain("companyNumber").And.NotContain("198.51.100.77");
            printed.Should().Contain("ExtendedAttributes = ***");
        }

        register.ToString().Should().Contain("Agreement = RegistrarAgreement { IpAddress = ***");
    }

    /// <summary>The agreement prints its address masked and its moment as it is, and equality still compares the address.</summary>
    [Fact]
    public void Agreement_ToString_MasksIpAddress()
    {
        var printed = RegistrarFixtures.Agreement.ToString();

        printed.Should().NotContain("198.51.100.77").And.Contain("IpAddress = ***").And.Contain("AcceptedAt");
        RegistrarFixtures.Agreement.Should().NotBe(RegistrarFixtures.Agreement with { IpAddress = "198.51.100.78" });
    }

    /// <summary>An availability answered the old way, with the flag only, carries no reason and no premium price.</summary>
    [Fact]
    public void Availability_FlagOnly_HasNoReasonAndNoPremiumPrice()
    {
        var answer = new RegistrarAvailability(true);

        answer.Reason.Should().BeNull();
        answer.PremiumPrice.Should().BeNull();
    }

    /// <summary>An availability can carry a reason and a premium price, and they take part in equality.</summary>
    [Fact]
    public void Availability_WithReasonAndPremiumPrice_CarriesThem()
    {
        var premium = new RegistrarPremiumPrice("USD", 500m, 450m);
        var answer = new RegistrarAvailability(true) { Reason = RegistrarAvailabilityReason.Premium, PremiumPrice = premium };

        answer.Reason.Should().Be(RegistrarAvailabilityReason.Premium);
        answer.PremiumPrice.Should().Be(premium);
        answer.Should().NotBe(new RegistrarAvailability(true));
        new RegistrarAvailability(false) { Reason = RegistrarAvailabilityReason.Registered }.IsAvailable.Should().BeFalse();
    }

    /// <summary>A DNS record built the old way is not proxied by choice and has no comment, and the new members take part in equality.</summary>
    [Fact]
    public void DnsRecord_ProxiedAndComment_DefaultToNull()
    {
        var record = RegistrarFixtures.DnsRecord;

        record.Proxied.Should().BeNull();
        record.Comment.Should().BeNull();
        record.Should().NotBe(record with { Proxied = true });
        record.Should().NotBe(record with { Comment = "managed by the host" });
        (record with { Comment = "managed by the host" }).Comment.Should().Be("managed by the host");
    }

    /// <summary>A CAA record is a record type of its own, and a record of that type can be built.</summary>
    [Fact]
    public void DnsRecord_CaaType_CanBeBuilt()
    {
        var record = new RegistrarDnsRecord(null, RegistrarDnsRecordType.Caa, "@", "0 issue \"ca.example.test\"", 300, null);

        record.Type.Should().Be(RegistrarDnsRecordType.Caa);
    }

    /// <summary>The new optional interfaces are separate opt ins that extend the operations interface and add no member to it, each with its own capability name.</summary>
    [Fact]
    public void NewOptionalInterfaces_AreSeparateOptIns_WithOwnCapabilities()
    {
        var optional = new[]
        {
            typeof(IRegistrarChildNameserversPlugin),
            typeof(IRegistrarDnssecPlugin),
            typeof(IRegistrarRestorePlugin),
            typeof(IRegistrarDomainListPlugin),
            typeof(IRegistrarSuggestionsPlugin),
            typeof(IRegistrarBalancePlugin),
        };
        var operationNames = typeof(IRegistrarOperationsPlugin).GetMethods().Select(method => method.Name).ToList();

        foreach (var type in optional)
        {
            type.GetInterfaces().Should().Contain(typeof(IRegistrarOperationsPlugin));
            typeof(IRegistrarOperationsPlugin).GetInterfaces().Should().NotContain(type);
            type.GetMethods().Select(method => method.Name).Should().NotIntersectWith(operationNames);
        }

        PluginCapabilities.RegistrarChildNameservers.Should().Be("registrarChildNameservers");
        PluginCapabilities.RegistrarDnssec.Should().Be("registrarDnssec");
        PluginCapabilities.RegistrarRestore.Should().Be("registrarRestore");
        PluginCapabilities.RegistrarDomainList.Should().Be("registrarDomainList");
        PluginCapabilities.RegistrarSuggestions.Should().Be("registrarSuggestions");
        PluginCapabilities.RegistrarBalance.Should().Be("registrarBalance");
    }

    /// <summary>A plugin built on an earlier version, with the operations only, implements none of the new interfaces.</summary>
    [Fact]
    public void ScriptedPlugin_BuiltOnEarlierVersion_ImplementsNoNewInterface()
    {
        var legacy = new ScriptedRegistrarPlugin();

        legacy.Should().NotBeAssignableTo<IRegistrarChildNameserversPlugin>();
        legacy.Should().NotBeAssignableTo<IRegistrarDnssecPlugin>();
        legacy.Should().NotBeAssignableTo<IRegistrarRestorePlugin>();
        legacy.Should().NotBeAssignableTo<IRegistrarDomainListPlugin>();
        legacy.Should().NotBeAssignableTo<IRegistrarSuggestionsPlugin>();
        legacy.Should().NotBeAssignableTo<IRegistrarBalancePlugin>();
    }
}
