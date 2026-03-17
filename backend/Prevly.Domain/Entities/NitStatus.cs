namespace Prevly.Domain.Entities;

public enum NitStatus
{
    /// <summary>
    /// NIT aguardando consulta de titularidade (plugin Meu INSS).
    /// </summary>
    PendingOwnershipCheck = 0,

    /// <summary>
    /// Consulta de titularidade em andamento pelo worker.
    /// </summary>
    OwnershipCheckInProgress = 1,

    /// <summary>
    /// NIT rejeitado porque ja pertence a outra pessoa.
    /// </summary>
    RejectedOwnedByAnotherPerson = 2,

    /// <summary>
    /// NIT aprovado na titularidade e pendente de calcular anos de contribuicao.
    /// </summary>
    PendingContributionCalculation = 3,

    /// <summary>
    /// Calculo de contribuicao concluido e pronto para vincular com person.
    /// </summary>
    ReadyForPersonBinding = 4,

    /// <summary>
    /// NIT ja vinculado a uma person.
    /// </summary>
    BoundToPerson = 5
}
