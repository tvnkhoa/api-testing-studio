using ApiTestingStudio.Domain.Entities;
using ApiTestingStudio.Shared.Results;

namespace ApiTestingStudio.Application.Variables;

/// <summary>
/// Create/read/update/delete variables for the open workspace across scopes. Secret variables have
/// their value encrypted through <c>ISecretProtector</c> before persistence.
/// </summary>
public interface IVariableService
{
    Task<Result<IReadOnlyList<Variable>>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result<Variable>> CreateAsync(VariableDraft draft, CancellationToken cancellationToken = default);

    Task<Result<Variable>> UpdateAsync(Guid id, VariableDraft draft, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
