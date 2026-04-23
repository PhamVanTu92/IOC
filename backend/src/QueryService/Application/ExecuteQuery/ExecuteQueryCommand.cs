using MediatR;
using SemanticEngine.Models;

namespace QueryService.Application.ExecuteQuery;

/// <summary>
/// Command thực thi dynamic query qua Semantic Layer.
/// Handler sẽ được implement trong STEP 2 (SQL Builder + Query Engine).
/// </summary>
public sealed record ExecuteQueryCommand(QueryInput Input) : IRequest<QueryResult>;
