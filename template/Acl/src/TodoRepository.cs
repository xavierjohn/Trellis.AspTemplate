namespace TodoSample.AntiCorruptionLayer;

using Microsoft.EntityFrameworkCore;
using TodoSample.Application;
using TodoSample.Domain;
using Trellis.EntityFrameworkCore;

/// <summary>
/// EF Core implementation of ITodoRepository.
/// </summary>
internal class TodoRepository : ITodoRepository
{
    // Hard server-side cap on the page size, regardless of what the client requests.
    private const int ServerCap = 100;

    private readonly AppDbContext _context;

    public TodoRepository(AppDbContext context) => _context = context;

    public async Task<Maybe<TodoItem>> FindByIdAsync(TodoId id, CancellationToken cancellationToken) =>
        await _context.TodoItems.FirstOrDefaultMaybeAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(Specification<TodoItem> specification, CancellationToken cancellationToken) =>
        await _context.TodoItems
            .Where(specification)
            .ToListAsync(cancellationToken);

    public async Task<Result<Page<TodoItem>>> GetPageAsync(int requestedLimit, Cursor? cursor, CancellationToken cancellationToken)
    {
        var appliedLimit = Math.Min(requestedLimit <= 0 ? 10 : requestedLimit, ServerCap);

        Guid? afterId = null;
        if (cursor is { } c)
        {
            if (!TryDecodeCursor(c, out var decoded))
                return Result.Fail<Page<TodoItem>>(
                    new Error.BadRequest("invalid_cursor") { Detail = "Cursor is not a recognized token." });
            afterId = decoded;
        }

        // Fetch one extra row to detect "has more". TodoId.Value is V7 (time-ordered) so
        // ordering by Guid value gives a stable, monotonically increasing keyset.
        var query = _context.TodoItems.OrderBy(t => t.Id);
        var fetched = afterId is Guid g
            ? await query.Where(t => t.Id.Value.CompareTo(g) > 0).Take(appliedLimit + 1).ToListAsync(cancellationToken)
            : await query.Take(appliedLimit + 1).ToListAsync(cancellationToken);

        var hasMore = fetched.Count > appliedLimit;
        var items = hasMore ? fetched.Take(appliedLimit).ToList() : fetched;
        Cursor? next = hasMore && items.Count > 0
            ? new Cursor(EncodeCursor(items[^1].Id.Value))
            : null;

        return Result.Ok(new Page<TodoItem>(
            Items: items,
            Next: next,
            Previous: null,
            RequestedLimit: requestedLimit,
            AppliedLimit: appliedLimit));
    }

    public async Task<Result> SaveAsync(TodoItem todo, CancellationToken cancellationToken)
    {
        var entry = _context.Entry(todo);
        if (entry.State == EntityState.Detached)
            _context.TodoItems.Add(todo);

        return await _context.SaveChangesResultUnitAsync(cancellationToken);
    }

    public async Task<Result> DeleteAsync(TodoId id, CancellationToken cancellationToken)
    {
        var maybe = await FindByIdAsync(id, cancellationToken);
        return await maybe
            .ToResult(new Error.NotFound(new ResourceRef("Todo", id.Value.ToString())) { Detail = $"Todo {id} not found." })
            .Tap(todo => _context.TodoItems.Remove(todo))
            .BindAsync(_ => _context.SaveChangesResultUnitAsync(cancellationToken));
    }

    // Opaque base64url-encoded JSON. Keep encoding hidden from clients so the schema can
    // evolve (add a tie-breaker, swap to keyset on (CreatedAt, Id), etc.) without breaking
    // existing tokens.
    private static string EncodeCursor(Guid afterId)
    {
        var json = $"{{\"afterId\":\"{afterId}\"}}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return Base64UrlEncode(bytes);
    }

    private static bool TryDecodeCursor(Cursor cursor, out Guid afterId)
    {
        afterId = default;
        if (string.IsNullOrEmpty(cursor.Token))
            return false;
        try
        {
            var bytes = Base64UrlDecode(cursor.Token);
            using var doc = System.Text.Json.JsonDocument.Parse(bytes);
            if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) return false;
            if (!doc.RootElement.TryGetProperty("afterId", out var prop)) return false;
            return Guid.TryParse(prop.GetString(), out afterId);
        }
        catch (System.Text.Json.JsonException) { return false; }
        catch (FormatException) { return false; }
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string token)
    {
        var s = token.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
