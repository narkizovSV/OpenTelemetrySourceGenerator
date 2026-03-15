# OpenTelemetrySourceGenerator

Source Generator, который генерирует extension-методы для оборачивания вызовов интерфейсов в tracing (`Activity`/span).

## Что в репозитории

- `sources/TraceUtils` - атрибуты и runtime-утилиты.
- `sources/TraceUtils.SourceGenerator` - Roslyn Source Generator.
- `sources/TraceUtils.SourceGenerator.Tests` - тесты генератора (NUnit + Verify snapshots).

## Как использовать

### 1. Настройка проекта

В проект добавить NuGet‑ссылки:

+ `TraceUtils`
+ `TraceUtils.SourceGenerator`
+ `OpenTelemetry (официальный .NET SDK)`

### 2. Разметка интерфейса

+ **На методах**: `[ActivityOperation("ИмяОперации", ActivityType.Internal)]` — задаёт имя операции и тип activity.
+ **На аргументах**: `[SpanTag("tag.name")] или [SpanTag("tag.name", shouldSerialize: true)]` — записывает параметры в span как теги (второй вариант сериализует сложный тип).

### 3. Вызов сгенерированных методов

Генератор создаёт extension‑методы в TraceUtils.Extensions:

+ **sync**: `MethodNameWithTrace(...)`
+ **async**: `MethodNameWithTraceAsync(...)`

Вместо прямого `_service.Method(...)` вызывается `_service.MethodWithTrace(...)`, чтобы вызов попал в span.

### 4. Регистрация в OpenTelemetry

При конфигурации `OpenTelemetry TracerProvider` зарегистрировать источник активностей из `TraceUtils` (через AddSource(TraceUtils.Utils.ServiceName)), чтобы сгенерированные `span’`ы экспортировались выбранным экспортером.

```csharp
using OpenTelemetry;
using OpenTelemetry.Trace;

// При настройке провайдера трассировки:
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(TraceUtils.Utils.ServiceName)
        .AddYourExporter(...));
```
