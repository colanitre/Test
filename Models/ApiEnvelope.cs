namespace RpgApi.Models;

public record ApiMeta(DateTime ServerTime, string TraceId, ApiPaginationMeta? Pagination = null);

public record ApiPaginationMeta(int Page, int PageSize, int TotalItems, int TotalPages);

public record ApiError(string Code, string Message, object? Details = null);

public record ApiEnvelope<T>(T? Data, ApiMeta Meta, ApiError? Error = null);
