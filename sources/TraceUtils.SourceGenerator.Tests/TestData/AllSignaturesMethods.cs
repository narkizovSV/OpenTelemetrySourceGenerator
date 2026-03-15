using System.Threading;
using System.Threading.Tasks;
using TraceUtils;

namespace TraceUtils.SourceGenerator.Tests.TestData;

public enum UserKind
{
    Unknown = 0,
    Internal = 1,
    External = 2
}

public sealed class ProfileDto
{
    public long ProfileId { get; set; }
    public string? Email { get; set; }
}

public readonly struct CoordinatesDto(double lat, double lon)
{
    public double Lat { get; } = lat;
    public double Lon { get; } = lon;
}

public interface IAllSignaturesMethodsGrouped
{
    // ========== БАЗОВЫЕ СЦЕНАРИИ ==========

    [ActivityOperation("VoidNoParams", ActivityType.Internal)]
    void VoidNoParams();

    [ActivityOperation("SyncWithReturn", ActivityType.Internal)]
    int SyncWithReturn();

    [ActivityOperation("AsyncTask", ActivityType.Internal)]
    Task AsyncTask();

    [ActivityOperation("AsyncTaskWithResult", ActivityType.Internal)]
    Task<string> AsyncTaskWithResult();

    [ActivityOperation("AsyncValueTask", ActivityType.Internal)]
    ValueTask AsyncValueTask();

    [ActivityOperation("AsyncValueTaskWithResult", ActivityType.Internal)]
    ValueTask<int> AsyncValueTaskWithResult();

    // ========== ПРИМИТИВЫ (передаются напрямую) ==========

    [ActivityOperation("PrimitiveTags", ActivityType.Internal)]
    void PrimitiveTags(
        [SpanTag("tag.int")] int intVal,
        [SpanTag("tag.long")] long longVal,
        [SpanTag("tag.double")] double doubleVal,
        [SpanTag("tag.decimal")] decimal decimalVal,
        [SpanTag("tag.bool")] bool boolVal,
        [SpanTag("tag.string")] string stringVal,
        [SpanTag("tag.char")] char charVal);

    [ActivityOperation("PrimitiveDefaults", ActivityType.Internal)]
    void PrimitiveDefaults(
        [SpanTag("tag.count")] int count = 10,
        [SpanTag("tag.name")] string name = "default",
        [SpanTag("tag.enabled")] bool enabled = true);

    // ========== DATETIME (форматируется ToString("O")) ==========

    [ActivityOperation("DateTimeTags", ActivityType.Internal)]
    void DateTimeTags(
        [SpanTag("tag.date")] DateTime date,
        [SpanTag("tag.dateOffset")] DateTimeOffset dateOffset,
        [SpanTag("tag.dateOnly")] DateOnly dateOnly,
        [SpanTag("tag.timeOnly")] TimeOnly timeOnly);

    [ActivityOperation("DateTimeIn", ActivityType.Internal)]
    void DateTimeIn(
        [SpanTag("tag.timestamp")] in DateTime timestamp);

    [ActivityOperation("NullableDateTimeTags", ActivityType.Internal)]
    void NullableDateTimeTags(
        [SpanTag("tag.date")] DateTime? date,
        [SpanTag("tag.dateOffset")] DateTimeOffset? dateOffset,
        [SpanTag("tag.dateOnly")] DateOnly? dateOnly,
        [SpanTag("tag.timeOnly")] TimeOnly? timeOnly);

    // ========== GUID (форматируется ToString()) ==========

    [ActivityOperation("GuidTags", ActivityType.Internal)]
    void GuidTags(
        [SpanTag("tag.id")] Guid id,
        [SpanTag("tag.correlationId")] Guid? correlationId);

    // ========== ENUM (передаётся напрямую) ==========

    [ActivityOperation("EnumTags", ActivityType.Internal)]
    void EnumTags(
        [SpanTag("tag.kind")] UserKind kind,
        [SpanTag("tag.nullableKind")] UserKind? nullableKind,
        [SpanTag("tag.defaultKind")] UserKind defaultKind = UserKind.Internal);

    // ========== TIMESPAN (передаётся напрямую) ==========

    [ActivityOperation("TimeSpanTags", ActivityType.Internal)]
    void TimeSpanTags(
        [SpanTag("tag.duration")] TimeSpan duration,
        [SpanTag("tag.timeout")] TimeSpan? timeout);

    // ========== МАССИВЫ: ShouldSerialize=true (сериализуются) ==========

    [ActivityOperation("ArraySerialize", ActivityType.Internal)]
    void ArraySerialize(
        [SpanTag("tag.bytes", true)] byte[] bytes,
        [SpanTag("tag.ids", true)] int[] ids,
        [SpanTag("tag.names", true)] string[] names);

    // ========== МАССИВЫ: ShouldSerialize=false (пропускаются) ==========

    [ActivityOperation("ArrayNoSerialize", ActivityType.Internal)]
    void ArrayNoSerialize(
        [SpanTag("tag.bytes", false)] byte[] bytes,
        [SpanTag("tag.ids", false)] int[] ids,
        [SpanTag("tag.names", false)] string[] names);

    // ========== GENERIC КОЛЛЕКЦИИ: ShouldSerialize=true ==========

    [ActivityOperation("GenericCollectionSerialize", ActivityType.Internal)]
    void GenericCollectionSerialize(
        [SpanTag("tag.list", true)] List<int> list,
        [SpanTag("tag.dict", true)] Dictionary<string, int> dict,
        [SpanTag("tag.enumerable", true)] IEnumerable<string> enumerable,
        [SpanTag("tag.readonlyList", true)] IReadOnlyList<long> readonlyList);

    // ========== GENERIC КОЛЛЕКЦИИ: ShouldSerialize=false ==========

    [ActivityOperation("GenericCollectionNoSerialize", ActivityType.Internal)]
    void GenericCollectionNoSerialize(
        [SpanTag("tag.list", false)] List<int> list,
        [SpanTag("tag.dict", false)] Dictionary<string, int> dict,
        [SpanTag("tag.enumerable", false)] IEnumerable<string> enumerable);

    // ========== CLASS DTO: ShouldSerialize=true (сериализуется) ==========

    [ActivityOperation("ClassSerialize", ActivityType.Internal)]
    void ClassSerialize(
        [SpanTag("tag.profile", true)] ProfileDto profile,
        [SpanTag("tag.nullableProfile", true)] ProfileDto? nullableProfile);

    // ========== CLASS DTO: ShouldSerialize=false (пропускается) ==========

    [ActivityOperation("ClassNoSerialize", ActivityType.Internal)]
    void ClassNoSerialize(
        [SpanTag("tag.profile", false)] ProfileDto profile,
        [SpanTag("tag.nullableProfile", false)] ProfileDto? nullableProfile);

    // ========== STRUCT: ShouldSerialize=true (сериализуется) ==========

    [ActivityOperation("StructSerialize", ActivityType.Internal)]
    void StructSerialize(
        [SpanTag("tag.coords", true)] CoordinatesDto coords,
        [SpanTag("tag.nullableCoords", true)] CoordinatesDto? nullableCoords);

    // ========== STRUCT: ShouldSerialize=false (пропускается) ==========

    [ActivityOperation("StructNoSerialize", ActivityType.Internal)]
    void StructNoSerialize(
        [SpanTag("tag.coords", false)] CoordinatesDto coords,
        [SpanTag("tag.nullableCoords", false)] CoordinatesDto? nullableCoords);

    // ========== СМЕШАННЫЙ: разные типы с разными флагами ==========

    [ActivityOperation("MixedTags", ActivityType.Internal)]
    void MixedTags(
        [SpanTag("tag.id")] int id,
        [SpanTag("tag.name")] string name,
        [SpanTag("tag.date")] DateTime date,
        [SpanTag("tag.guid")] Guid guid,
        [SpanTag("tag.kind")] UserKind kind,
        [SpanTag("tag.profile", true)] ProfileDto profile,
        [SpanTag("tag.coords", true)] CoordinatesDto coords,
        [SpanTag("tag.ids", true)] int[] ids,
        [SpanTag("tag.skipProfile", false)] ProfileDto skipProfile,
        [SpanTag("tag.skipCoords", false)] CoordinatesDto skipCoords,
        [SpanTag("tag.skipIds", false)] int[] skipIds);

    // ========== GENERIC МЕТОДЫ ==========

    [ActivityOperation("GenericMethod", ActivityType.Internal)]
    T GenericMethod<T>(
        [SpanTag("tag.input", true)] T input);

    [ActivityOperation("GenericMethodNoSerialize", ActivityType.Internal)]
    T GenericMethodNoSerialize<T>(
        [SpanTag("tag.input", false)] T input);

    [ActivityOperation("GenericWithConstraints", ActivityType.Internal)]
    void GenericWithConstraints<T>(
        [SpanTag("tag.entity", true)] T entity)
        where T : class, new();

    [ActivityOperation("GenericMultipleTypes", ActivityType.Internal)]
    TOutput GenericMultipleTypes<TInput, TOutput>(
        [SpanTag("tag.source", true)] TInput source)
        where TOutput : TInput;

    // ========== IN/OUT/REF ПАРАМЕТРЫ ==========

    [ActivityOperation("RefParameters", ActivityType.Internal)]
    void RefParameters(
        [SpanTag("tag.inVal")] in int inVal,
        [SpanTag("tag.refVal")] ref int refVal,
        [SpanTag("tag.outVal")] out int outVal);

    [ActivityOperation("InDateTime", ActivityType.Internal)]
    void InDateTime(
        [SpanTag("tag.date")] in DateTime date);

    [ActivityOperation("InStruct", ActivityType.Internal)]
    void InStruct(
        [SpanTag("tag.coords", true)] in CoordinatesDto coords);

    // ========== ПАРАМЕТРЫ БЕЗ SPANTAG (не включаются) ==========

    [ActivityOperation("NoTagParams", ActivityType.Internal)]
    void NoTagParams(
        int noTagInt,
        string noTagString,
        [SpanTag("tag.withTag")] int withTag);

    [ActivityOperation("CancellationOnly", ActivityType.Internal)]
    Task CancellationOnly(
        CancellationToken cancellationToken);

    // ========== PARAMS МАССИВ ==========

    [ActivityOperation("ParamsArray", ActivityType.Internal)]
    void ParamsArray(
        [SpanTag("tag.operation")] string operation,
        [SpanTag("tag.values", true)] params int[] values);

    [ActivityOperation("ParamsArrayNoSerialize", ActivityType.Internal)]
    void ParamsArrayNoSerialize(
        [SpanTag("tag.operation")] string operation,
        [SpanTag("tag.values", false)] params int[] values);

    // ========== TUPLE ВОЗВРАЩАЕМЫЕ ЗНАЧЕНИЯ ==========

    [ActivityOperation("TupleReturn", ActivityType.Internal)]
    (bool success, string message) TupleReturn(
        [SpanTag("tag.input")] string input);

    [ActivityOperation("AsyncTupleReturn", ActivityType.Internal)]
    Task<(int count, DateTime lastUpdate)> AsyncTupleReturn(
        [SpanTag("tag.filter")] string filter);

    // ========== NULLABLE PRIMITIVES (передаются напрямую) ==========

    [ActivityOperation("NullablePrimitives", ActivityType.Internal)]
    void NullablePrimitives(
        [SpanTag("tag.int")] int? nullableInt,
        [SpanTag("tag.long")] long? nullableLong,
        [SpanTag("tag.short")] short? nullableShort,
        [SpanTag("tag.byte")] byte? nullableByte,
        [SpanTag("tag.bool")] bool? nullableBool,
        [SpanTag("tag.char")] char? nullableChar,
        [SpanTag("tag.double")] double? nullableDouble,
        [SpanTag("tag.float")] float? nullableFloat,
        [SpanTag("tag.decimal")] decimal? nullableDecimal);

    [ActivityOperation("NullableTimeSpan", ActivityType.Internal)]
    void NullableTimeSpan(
        [SpanTag("tag.duration")] TimeSpan? duration);

    // ========== ACTIVITY TYPE CLIENT ==========

    [ActivityOperation("ClientActivity", ActivityType.Client)]
    Task<string> ClientActivity(
        [SpanTag("tag.url")] string url,
        [SpanTag("tag.method")] string method);

    // ========== ДЕЛЕГАТЫ И FUNC/ACTION (без SpanTag) ==========

    [ActivityOperation("WithDelegates", ActivityType.Internal)]
    Task WithDelegates(
        [SpanTag("tag.data")] string data,
        Action<string> callback,
        Func<string, Task<int>>? asyncCallback = null);

    // ========== КОЛЛЕКЦИИ КАСТОМНЫХ ТИПОВ ==========

    [ActivityOperation("CustomTypeCollections", ActivityType.Internal)]
    Task CustomTypeCollections(
        [SpanTag("tag.profiles", true)] List<ProfileDto> profiles,
        [SpanTag("tag.coordsList", true)] IReadOnlyList<CoordinatesDto> coordsList,
        [SpanTag("tag.kindDict", true)] Dictionary<UserKind, ProfileDto> kindDict);

    [ActivityOperation("CustomTypeCollectionsNoSerialize", ActivityType.Internal)]
    Task CustomTypeCollectionsNoSerialize(
        [SpanTag("tag.profiles", false)] List<ProfileDto> profiles,
        [SpanTag("tag.coordsList", false)] IReadOnlyList<CoordinatesDto> coordsList);
}