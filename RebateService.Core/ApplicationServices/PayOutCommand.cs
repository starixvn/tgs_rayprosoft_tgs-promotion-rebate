namespace Tgs.Services.Rebate.ApplicationServices;

public record PayOutCommand(Guid PlanId, Guid DistributeId);