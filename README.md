# OpenTelemetrySourceGenerator

Source Generator, который генерирует extension-методы для оборачивания вызовов интерфейсов в tracing (`Activity`/span).

## Что в репозитории

- `sources/TraceUtils` - атрибуты и runtime-утилиты.
- `sources/TraceUtils.SourceGenerator` - Roslyn Source Generator.
- `sources/TraceUtils.SourceGenerator.Tests` - тесты генератора (NUnit + Verify snapshots).

## Быстрый старт локально

1. Установите .NET SDK (рекомендуется актуальная версия).
2. Клонируйте репозиторий и откройте решение `sources/TraceUtils.slnx`.
3. Выполните сборку:

```powershell
dotnet build .\sources\TraceUtils.slnx
```

4. Запустите тесты генератора:

```powershell
dotnet test .\sources\TraceUtils.SourceGenerator.Tests\TraceUtils.SourceGenerator.Tests.csproj
```

## Как подключить генератор в свой проект

Есть 2 основных способа: через `ProjectReference` (удобно для локальной разработки) или через NuGet (для использования в других репозиториях).

### Вариант 1: локально через ProjectReference

В проект-потребитель (`.csproj`) добавьте ссылку на generator-проект как analyzer:

```xml
<ItemGroup>
  <ProjectReference Include="..\TraceUtils\TraceUtils.csproj" />
  <ProjectReference Include="..\TraceUtils.SourceGenerator\TraceUtils.SourceGenerator.csproj"
                    OutputItemType="Analyzer"
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

`TraceUtils` нужен для атрибутов, а `TraceUtils.SourceGenerator` - для генерации кода на этапе компиляции.

### Как сохранять сгенерированные `.cs` файлы на диск

Для удобной диагностики можно включить вывод generated-файлов в проекте-потребителе:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated\$(TargetFramework)</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Чтобы файлы из этой папки не компилировались повторно как обычный исходный код, добавьте исключение:

```xml
<ItemGroup>
  <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
</ItemGroup>
```

После этого можно смотреть результат генерации в папке `Generated/<TargetFramework>`.

### Вариант 2: через NuGet (когда появится пакет)

Подключите пакет с runtime-частью и пакет с analyzer/source generator.
Для analyzer-пакета важно, чтобы он попадал как `Analyzer`, а не обычная runtime-зависимость.

## Как настроить Roslyn-профиль для отладки генератора

Рекомендуемый путь - отладка как Roslyn Component:

Перед настройкой профиля убедитесь, что установлены:

- .NET SDK.
- компонент/пакет `.NET Compiler Platform` (Roslyn tools в IDE).

1. Откройте `sources/TraceUtils.SourceGenerator/TraceUtils.SourceGenerator.csproj`.
2. Добавьте в `PropertyGroup`:

```xml
<IsRoslynComponent>true</IsRoslynComponent>
```

3. Перезапустите IDE (если профиль не появился сразу).
4. Создайте/выберите Debug Profile типа `Roslyn Component`.
5. В профиле укажите целевой проект (проект-потребитель, где подключен генератор).
6. Поставьте breakpoints в `TelemetryExtensionsGenerator` и коде рендеринга/трансформации.
7. Запустите отладку профиля - IDE поднимет компиляцию целевого проекта, и генератор выполнится под отладчиком.

## Отладка через тесты (тоже поддерживается)

Генератор удобно дебажить и через тесты:

1. Откройте проект `TraceUtils.SourceGenerator.Tests`.
2. Поставьте breakpoints в код генератора и/или в тесте, который его вызывает.
3. Запустите конкретный тест в режиме Debug (например, `TelemetryExtensionsGeneratorTests`).

Плюсы этого подхода:

- Быстрый воспроизводимый сценарий.
- Легко покрывать edge-cases через test data.
- Snapshot-проверки (`Verify`) помогают быстро увидеть, что именно сгенерировалось и что изменилось.

## Полезные команды

```powershell
# Сборка всего решения
dotnet build .\sources\TraceUtils.slnx

# Прогон тестов генератора
dotnet test .\sources\TraceUtils.SourceGenerator.Tests\TraceUtils.SourceGenerator.Tests.csproj
```
