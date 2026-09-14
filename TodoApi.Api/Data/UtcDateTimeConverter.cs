using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TodoApi.Api.Data;

/// <summary>
/// SQLite has no timezone-aware column type, so EF reads DateTime back as Kind=Unspecified.
/// Normalise to UTC on the way in and stamp Kind=Utc on the way out so JSON output always carries "Z".
/// </summary>
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    toStore => toStore.Kind == DateTimeKind.Unspecified
        ? DateTime.SpecifyKind(toStore, DateTimeKind.Utc)
        : toStore.ToUniversalTime(),
    fromStore => DateTime.SpecifyKind(fromStore, DateTimeKind.Utc));
