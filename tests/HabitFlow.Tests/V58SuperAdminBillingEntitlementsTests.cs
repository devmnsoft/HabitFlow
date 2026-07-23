using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class V58SuperAdminBillingEntitlementsTests
{
    [Fact] public void Cpf_validates_normalizes_and_formats()
    {
        var v = new DocumentValidator();
        Assert.Equal("52998224725", v.Normalize("529.982.247-25"));
        Assert.True(v.ValidateCpf("529.982.247-25"));
        Assert.Equal("529.982.247-25", v.FormatCpf("52998224725"));
    }
    [Theory] [InlineData("111.111.111-11")] [InlineData("123")] public void Cpf_invalid_is_rejected(string cpf) => Assert.False(new DocumentValidator().ValidateCpf(cpf));
    [Fact] public void Cnpj_validates_normalizes_and_formats()
    {
        var v = new DocumentValidator();
        Assert.Equal("11222333000181", v.Normalize("11.222.333/0001-81"));
        Assert.True(v.ValidateCnpj("11.222.333/0001-81"));
        Assert.Equal("11.222.333/0001-81", v.FormatCnpj("11222333000181"));
    }
    [Theory] [InlineData("00.000.000/0000-00")] [InlineData("123")] public void Cnpj_invalid_is_rejected(string cnpj) => Assert.False(new DocumentValidator().ValidateCnpj(cnpj));
    [Fact] public void SuperAdmin_role_and_client_saas_statuses_exist()
    {
        Assert.Contains(UserRole.SuperAdmin, Enum.GetValues<UserRole>());
        Assert.Contains(ClientBenefitsStatus.PremiumBlocked, Enum.GetValues<ClientBenefitsStatus>());
        Assert.Contains(ClientPaymentStatus.Overdue, Enum.GetValues<ClientPaymentStatus>());
    }
}
