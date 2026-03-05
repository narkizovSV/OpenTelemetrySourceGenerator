using System.Threading;
using System.Threading.Tasks;
using TraceUtils.Attributes;

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

public readonly struct CoordinatesDto
{
    public CoordinatesDto(double lat, double lon)
    {
        Lat = lat;
        Lon = lon;
    }

    public double Lat { get; }

    public double Lon { get; }
}

public interface IAllSignaturesMethodsGrouped
{
    // ========== VOID МЕТОДЫ ==========

    /// <summary>Void без параметров</summary>
    [TracerEvent("VoidNoParams")]
    void VoidNoParams();

    /// <summary>Void с параметрами</summary>
    [TracerEvent("VoidWithParams")]
    void VoidWithParams(
        [TracerProperty("arg.id")] int id,
        [TracerProperty("arg.name")] string name);

    /// <summary>Void с параметрами по умолчанию</summary>
    [TracerEvent("VoidWithDefaults")]
    void VoidWithDefaults(
        [TracerProperty("arg.retryCount")] int retryCount = 3,
        [TracerProperty("arg.timeout")] int timeout = 5000);

    // ========== ВОЗВРАЩАЕМЫЕ ТИПЫ ==========

    /// <summary>Метод с возвращаемым значением без параметров</summary>
    [TracerEvent("GetValue")]
    int GetValue();

    /// <summary>Метод с возвращаемым значением и параметрами</summary>
    [TracerEvent("Calculate")]
    double Calculate(
        [TracerProperty("arg.x")] double x,
        [TracerProperty("arg.y")] double y);

    /// <summary>Метод с возвращаемым значением и параметрами по умолчанию</summary>
    [TracerEvent("GetConfig")]
    string GetConfig(
        [TracerProperty("arg.key")] string key,
        [TracerProperty("arg.defaultValue")] string defaultValue = "default");

    // ========== ASYNC TASK МЕТОДЫ ==========

    /// <summary>Async Task без параметров</summary>
    [TracerEvent("AsyncTaskNoParams")]
    Task AsyncTaskNoParams();

    /// <summary>Async Task с параметрами</summary>
    [TracerEvent("AsyncTaskWithParams")]
    Task AsyncTaskWithParams(
        [TracerProperty("arg.userId")] string userId,
        [TracerProperty("arg.action")] string action);

    /// <summary>Async Task с CancellationToken</summary>
    [TracerEvent("AsyncTaskWithCancellation")]
    Task AsyncTaskWithCancellation(
        [TracerProperty("arg.operation")] string operation,
        CancellationToken cancellationToken);

    // ========== ASYNC TASK<T> МЕТОДЫ ==========

    /// <summary>Async Task<T> без параметров</summary>
    [TracerEvent("AsyncGetDataNoParams")]
    Task<string> AsyncGetDataNoParams();

    /// <summary>Async Task<T> с параметрами</summary>
    [TracerEvent("AsyncGetDataWithParams")]
    Task<int> AsyncGetDataWithParams(
        [TracerProperty("arg.filter")] string filter,
        [TracerProperty("arg.limit")] int limit);

    /// <summary>Async Task<T> с параметрами по умолчанию и CancellationToken</summary>
    [TracerEvent("AsyncFetchWithDefaults")]
    Task<List<string>> AsyncFetchWithDefaults(
        [TracerProperty("arg.category")] string category = "all",
        [TracerProperty("arg.maxItems")] int maxItems = 100,
        CancellationToken cancellationToken = default);

    // ========== VALUETASK МЕТОДЫ ==========

    /// <summary>ValueTask без параметров</summary>
    [TracerEvent("ValueTaskNoParams")]
    ValueTask ValueTaskNoParams();

    /// <summary>ValueTask с параметрами</summary>
    [TracerEvent("ValueTaskWithParams")]
    ValueTask ValueTaskWithParams(
        [TracerProperty("arg.data")] byte[] data,
        [TracerProperty("arg.offset")] int offset);

    // ========== VALUETASK<T> МЕТОДЫ ==========

    /// <summary>ValueTask<T> без параметров</summary>
    [TracerEvent("ValueTaskGetNoParams")]
    ValueTask<bool> ValueTaskGetNoParams();

    /// <summary>ValueTask<T> с параметрами</summary>
    [TracerEvent("ValueTaskGetWithParams")]
    ValueTask<decimal> ValueTaskGetWithParams(
        [TracerProperty("arg.accountId")] Guid accountId,
        [TracerProperty("arg.currency")] string currency);

    // ========== GENERIC МЕТОДЫ (NON-CONSTRAINT) ==========

    /// <summary>Generic метод без ограничений</summary>
    [TracerEvent("GenericProcess")]
    T GenericProcess<T>(
        [TracerProperty("arg.input")] T input);

    /// <summary>Generic метод с двумя типами</summary>
    [TracerEvent("GenericTransform")]
    TOutput GenericTransform<TInput, TOutput>(
        [TracerProperty("arg.source")] TInput source,
        [TracerProperty("arg.converter")] Func<TInput, TOutput> converter);

    // ========== GENERIC МЕТОДЫ (С ОГРАНИЧЕНИЯМИ) ==========

    /// <summary>Generic метод с where class</summary>
    [TracerEvent("GenericWhereClass")]
    void GenericWhereClass<T>(
        [TracerProperty("arg.entity")] T entity)
        where T : class;

    /// <summary>Generic метод с where struct</summary>
    [TracerEvent("GenericWhereStruct")]
    T GenericWhereStruct<T>(
        [TracerProperty("arg.value")] T value)
        where T : struct;

    /// <summary>Generic метод с where new()</summary>
    [TracerEvent("GenericWhereNew")]
    T GenericWhereNew<T>()
        where T : new();

    /// <summary>Generic метод с интерфейсным ограничением</summary>
    [TracerEvent("GenericWhereInterface")]
    Task<int> GenericWhereInterface<T>(
        [TracerProperty("arg.items")] IEnumerable<T> items)
        where T : IComparable<T>;

    /// <summary>Generic метод с множественными ограничениями</summary>
    [TracerEvent("GenericMultipleConstraints")]
    Task GenericMultipleConstraints<T>(
        [TracerProperty("arg.entity")] T entity,
        CancellationToken cancellationToken)
        where T : class, IDisposable, new();

    // ========== GENERIC ASYNC МЕТОДЫ ==========

    /// <summary>Generic async Task</summary>
    [TracerEvent("GenericAsync")]
    Task<T> GenericAsync<T>(
        [TracerProperty("arg.id")] string id);

    /// <summary>Generic async ValueTask</summary>
    [TracerEvent("GenericAsyncValueTask")]
    ValueTask<T> GenericAsyncValueTask<T>(
        [TracerProperty("arg.key")] int key)
        where T : struct;

    // ========== IN/OUT/REF ПАРАМЕТРЫ ==========

    /// <summary>Метод с in параметром (read-only reference)</summary>
    [TracerEvent("MethodWithIn")]
    bool MethodWithIn(
        [TracerProperty("arg.timestamp")] in DateTime timestamp,
        [TracerProperty("arg.threshold")] int threshold);

    /// <summary>Метод с out параметром</summary>
    [TracerEvent("MethodWithOut")]
    bool MethodWithOut(
        [TracerProperty("arg.input")] string input,
        [TracerProperty("arg.result")] out int result);

    /// <summary>Метод с ref параметром</summary>
    [TracerEvent("MethodWithRef")]
    void MethodWithRef(
        [TracerProperty("arg.counter")] ref int counter,
        [TracerProperty("arg.increment")] int increment);

    // ========== PARAMS И КОЛЛЕКЦИИ ==========

    /// <summary>Метод с params массивом</summary>
    [TracerEvent("MethodWithParams")]
    int MethodWithParams(
        [TracerProperty("arg.operation")] string operation,
        [TracerProperty("arg.values")] params int[] values);

    /// <summary>Метод с коллекциями</summary>
    [TracerEvent("MethodWithCollections")]
    Task<Dictionary<string, object>> MethodWithCollections(
        [TracerProperty("arg.keys")] IReadOnlyList<string> keys,
        [TracerProperty("arg.options")] IDictionary<string, string> options);

    // ========== TUPLE ВОЗВРАЩАЕМЫЕ ЗНАЧЕНИЯ ==========

    /// <summary>Метод с tuple возвращаемым значением</summary>
    [TracerEvent("MethodReturningTuple")]
    (bool success, string message, int code) MethodReturningTuple(
        [TracerProperty("arg.operation")] string operation);

    /// <summary>Async метод с tuple</summary>
    [TracerEvent("AsyncMethodReturningTuple")]
    Task<(int count, DateTime lastUpdate)> AsyncMethodReturningTuple(
        [TracerProperty("arg.filter")] string filter);

    // ========== NULLABLE REFERENCE TYPES ==========

    /// <summary>Метод с nullable параметрами и возвращаемым значением</summary>
    [TracerEvent("MethodWithNullable")]
    string? MethodWithNullable(
        [TracerProperty("arg.id")] int? id,
        [TracerProperty("arg.fallback")] string? fallback = null);

    // ========== SPAN И MEMORY ==========

    /// <summary>Метод с Span параметром</summary>
    [TracerEvent("MethodWithSpan")]
    int MethodWithSpan(
        ReadOnlySpan<byte> data,
        [TracerProperty("arg.encoding")] string encoding);

    /// <summary>Метод с Memory параметром</summary>
    [TracerEvent("MethodWithMemory")]
    ValueTask<int> MethodWithMemory(
        ReadOnlyMemory<char> buffer,
        CancellationToken cancellationToken);

    // ========== СОБЫТИЯ И CALLBACK ==========

    /// <summary>Метод с callback делегатом</summary>
    [TracerEvent("MethodWithCallback")]
    Task MethodWithCallback(
        [TracerProperty("arg.data")] string data,
        Action<string> onComplete,
        Action<Exception>? onError = null);

    /// <summary>Метод с generic callback</summary>
    [TracerEvent("MethodWithGenericCallback")]
    Task<TResult> MethodWithGenericCallback<TInput, TResult>(
        [TracerProperty("arg.input")] TInput input,
        Func<TInput, Task<TResult>> processor);

    // ========== КОВАРИАНТНОСТЬ/КОНТРАВАРИАНТНОСТЬ ==========

    /// <summary>Метод с ковариантным generic out параметром</summary>
    [TracerEvent("CovariantMethod")]
    IEnumerable<TOutput> CovariantMethod<TInput, TOutput>(
        [TracerProperty("arg.items")] IEnumerable<TInput> items)
        where TOutput : TInput;

    /// <summary>Метод с контравариантным параметром</summary>
    [TracerEvent("ContravariantMethod")]
    void ContravariantMethod<T>(
        [TracerProperty("arg.handler")] Action<T> handler,
        [TracerProperty("arg.data")] T data);

    // ========== МЕТОДЫ С ENUM (UserKind) ==========

    /// <summary>Метод с enum параметром</summary>
    [TracerEvent("MethodWithEnum")]
    void MethodWithEnum(
        [TracerProperty("arg.userId")] long userId,
        [TracerProperty("arg.userKind")] UserKind userKind);

    /// <summary>Метод возвращающий enum</summary>
    [TracerEvent("MethodReturningEnum")]
    UserKind MethodReturningEnum(
        [TracerProperty("arg.email")] string email);

    /// <summary>Async метод с enum и значением по умолчанию</summary>
    [TracerEvent("AsyncMethodWithEnumDefault")]
    Task<int> AsyncMethodWithEnumDefault(
        [TracerProperty("arg.kind")] UserKind kind = UserKind.Unknown,
        CancellationToken cancellationToken = default);

    /// <summary>Метод с nullable enum</summary>
    [TracerEvent("MethodWithNullableEnum")]
    bool MethodWithNullableEnum(
        [TracerProperty("arg.kind")] UserKind? kind,
        [TracerProperty("arg.fallbackKind")] UserKind fallbackKind = UserKind.Internal);

    // ========== МЕТОДЫ С CLASS DTO (ProfileDto) ==========

    /// <summary>Метод с class параметром</summary>
    [TracerEvent("MethodWithClassDto")]
    void MethodWithClassDto(
        [TracerProperty("arg.profile")] ProfileDto profile);

    /// <summary>Метод возвращающий class</summary>
    [TracerEvent("MethodReturningClassDto")]
    ProfileDto MethodReturningClassDto(
        [TracerProperty("arg.profileId")] long profileId);

    /// <summary>Async метод с nullable class параметром</summary>
    [TracerEvent("AsyncMethodWithNullableClassDto")]
    Task<bool> AsyncMethodWithNullableClassDto(
        [TracerProperty("arg.profile")] ProfileDto? profile,
        CancellationToken cancellationToken = default);

    /// <summary>Async метод возвращающий nullable class</summary>
    [TracerEvent("AsyncMethodReturningNullableClassDto")]
    Task<ProfileDto?> AsyncMethodReturningNullableClassDto(
        [TracerProperty("arg.email")] string? email);

    /// <summary>Метод с коллекцией class DTO</summary>
    [TracerEvent("MethodWithClassDtoCollection")]
    Task<List<ProfileDto>> MethodWithClassDtoCollection(
        [TracerProperty("arg.profileIds")] IEnumerable<long> profileIds,
        [TracerProperty("arg.kind")] UserKind kind);

    // ========== МЕТОДЫ С READONLY STRUCT (CoordinatesDto) ==========

    /// <summary>Метод с readonly struct параметром</summary>
    [TracerEvent("MethodWithReadonlyStruct")]
    double MethodWithReadonlyStruct(
        [TracerProperty("arg.coordinates")] CoordinatesDto coordinates);

    /// <summary>Метод возвращающий readonly struct</summary>
    [TracerEvent("MethodReturningReadonlyStruct")]
    CoordinatesDto MethodReturningReadonlyStruct(
        [TracerProperty("arg.lat")] double lat,
        [TracerProperty("arg.lon")] double lon);

    /// <summary>Метод с in readonly struct (оптимизация передачи)</summary>
    [TracerEvent("MethodWithInReadonlyStruct")]
    bool MethodWithInReadonlyStruct(
        in CoordinatesDto coordinates,
        [TracerProperty("arg.maxDistance")] double maxDistance);

    /// <summary>Async метод с readonly struct</summary>
    [TracerEvent("AsyncMethodWithReadonlyStruct")]
    Task<string> AsyncMethodWithReadonlyStruct(
        [TracerProperty("arg.coordinates")] CoordinatesDto coordinates,
        CancellationToken cancellationToken = default);

    /// <summary>ValueTask метод возвращающий readonly struct</summary>
    [TracerEvent("ValueTaskMethodReturningReadonlyStruct")]
    ValueTask<CoordinatesDto> ValueTaskMethodReturningReadonlyStruct(
        [TracerProperty("arg.locationId")] int locationId);

    /// <summary>Метод с nullable readonly struct</summary>
    [TracerEvent("MethodWithNullableReadonlyStruct")]
    CoordinatesDto? MethodWithNullableReadonlyStruct(
        [TracerProperty("arg.locationId")] int? locationId);

    // ========== КОМБИНИРОВАННЫЕ МЕТОДЫ С КАСТОМНЫМИ ТИПАМИ ==========

    /// <summary>Метод комбинирующий enum, class и struct</summary>
    [TracerEvent("MethodCombiningCustomTypes")]
    Task<ProfileDto?> MethodCombiningCustomTypes(
        [TracerProperty("arg.kind")] UserKind kind,
        [TracerProperty("arg.coordinates")] CoordinatesDto coordinates,
        [TracerProperty("arg.radius")] double radius);

    /// <summary>Метод возвращающий tuple с кастомными типами</summary>
    [TracerEvent("MethodReturningTupleWithCustomTypes")]
    Task<(ProfileDto profile, CoordinatesDto location, UserKind kind)> MethodReturningTupleWithCustomTypes(
        [TracerProperty("arg.profileId")] long profileId);

    /// <summary>Generic метод с ограничением на enum</summary>
    [TracerEvent("GenericMethodWhereEnum")]
    TEnum GenericMethodWhereEnum<TEnum>(
        [TracerProperty("arg.value")] string value)
        where TEnum : struct, Enum;

    /// <summary>Метод с out параметром readonly struct</summary>
    [TracerEvent("MethodWithOutReadonlyStruct")]
    bool MethodWithOutReadonlyStruct(
        [TracerProperty("arg.address")] string address,
        out CoordinatesDto coordinates);

    /// <summary>Метод с коллекцией readonly struct</summary>
    [TracerEvent("MethodWithReadonlyStructCollection")]
    Task<double> MethodWithReadonlyStructCollection(
        [TracerProperty("arg.coordinates")] IReadOnlyList<CoordinatesDto> coordinates);

    /// <summary>Метод с Dictionary содержащим кастомные типы</summary>
    [TracerEvent("MethodWithCustomTypesDictionary")]
    Task<Dictionary<UserKind, List<ProfileDto>>> MethodWithCustomTypesDictionary(
        [TracerProperty("arg.filter")] CoordinatesDto? centerPoint = null,
        [TracerProperty("arg.radiusKm")] double radiusKm = 10.0);
}
