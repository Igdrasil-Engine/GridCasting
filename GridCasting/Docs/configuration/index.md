---
title: Конфигурация
order: 2
---

# Конфигурация проекта GridCasting

В отличие от приложений с пользовательским интерфейсом, GridCasting не требует отдельного конфигурационного файла для запуска. Основная конфигурация проекта задается на уровне решения, `.csproj` файлов и структуры зависимостей.

## Основной проект

Файл: `GridCasting/GridCasting.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
  </PropertyGroup>
</Project>
```

## Зависимости

Главная внешняя зависимость библиотеки:

- `IgdrasilMath` — математические типы и операции (`FVector2`, `FBox2` и др.)

Подключение задается через `ProjectReference`:

```xml
<ItemGroup>
  <ProjectReference Include="..\\..\\IgdrasilEngine\\IgdrasilMath\\IgdrasilMath.csproj" />
</ItemGroup>
```

## Тестовый проект

Файл: `GridCastingTests/GridCastingTests.csproj`

Используемые пакеты:

- `Microsoft.NET.Test.Sdk`
- `NUnit`
- `NUnit3TestAdapter`
- `coverlet.collector`

## Конфигурация на уровне кода

Основные параметры работы библиотеки задаются программно:

- `GridGraph graph`
- `float sensitivity`
- `IEnumerable<IPathTransform> transforms`

Пример:

```csharp
var casting = new GridCasting.GridCasting(graph, 0.1f, transforms);
```

## Структура документации

В папке `Docs` сосуществуют:

- инженерная документация по библиотеке
- папка `Reports` с отчетами по практике

## Ограничения текущей конфигурации

- нет отдельного демонстрационного приложения
- конкретные реализации `IPathTransform` пока не выделены в самостоятельный набор
- часть решения зависит от соседних проектов `IgdrasilEngine`
