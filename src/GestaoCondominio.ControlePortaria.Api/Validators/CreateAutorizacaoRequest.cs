using FluentValidation;
using GestaoCondominio.ControlePortaria.Api.DTOs;

public sealed class CreateAutorizacaoRequestValidator : AbstractValidator<CreateAutorizacaoRequest>
{
    public CreateAutorizacaoRequestValidator()
    {
        RuleFor(x => x.CondominioId).NotEmpty();
        RuleFor(x => x.Tipo)
            .NotEmpty()
            .Must(t => t.Equals("visitante", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Telefone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Cpf).MaximumLength(14);
        RuleFor(x => x.Rg).MaximumLength(30);
        RuleFor(x => x.Empresa).MaximumLength(150);
        RuleFor(x => x.Cnpj).MaximumLength(18);

        RuleFor(x => x.Periodo)
            .NotEmpty()
            .Must(p => p.Equals("unico", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.DataInicio).LessThanOrEqualTo(x => x.DataFim);

        RuleFor(x => x.Autorizador).NotNull();
        RuleFor(x => x.Autorizador.Unidade).NotEmpty();
        RuleFor(x => x.Autorizador.Nome).NotEmpty();
        RuleFor(x => x.Autorizador.Telefone).NotEmpty();

        When(x => x.Periodo.Equals("intervalo", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.DiasSemanaPermitidos)
                .NotNull().Must(d => d!.Count > 0)
                .WithMessage("Dias da semana obrigatórios quando periodo = intervalo.");

            RuleFor(x => x.JanelaHorarioInicio).NotNull();
            RuleFor(x => x.JanelaHorarioFim).NotNull();
            RuleFor(x => x)
                .Must(x => x.JanelaHorarioInicio <= x.JanelaHorarioFim)
                .WithMessage("Janela de horário inválida.");
        });
    }
}
