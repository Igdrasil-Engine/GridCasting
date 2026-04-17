---
title: Начало работы
order: -10
---

# Начало работы с GridCasting

## Требования

Для работы с проектом требуется:

- .NET 8 SDK
- доступ к зависимому проекту `IgdrasilMath`
- решение `GridCasting.sln`, если нужно запускать тесты и работать со всей структурой проекта

## Сборка проекта

```bash
dotnet build D:\Projects\CSharpProjects\GridCasting\GridCasting.sln
```

Если требуется проверить библиотеку автоматизированно:

```bash
dotnet test D:\Projects\CSharpProjects\GridCasting\GridCastingTests\GridCastingTests.csproj
```

## Минимальный сценарий использования

Ниже приведен упрощенный пример создания графа, регистрации команды и вызова фасада `GridCasting`.

```csharp
var graph = new GridGraph();
var node = new GridGraphNode();
for (var i = 0; i < 6; i++)
    node.Edges.Add(new GridGraphEdge(node, node, MathF.PI * i / 3, 1));
graph.Nodes.Add(node);

var casting = new GridCasting.GridCasting(graph, 0.1f);
casting.Executor.AddCommand(new DemoCommand(), new Path(0, 1, 1, 2));
casting.Execute(points);
```

## Базовый конвейер

Работа библиотеки строится по следующей схеме:

1. Пользователь передает массив координат `FVector2[]`.
2. `GridCasting` вызывает `GridResolver.GetPath(...)`.
3. `GridResolver` определяет ближайшую графовую точку и строит дискретный путь.
4. `PathExecutor` применяет трансформации и ищет совпадение в `Trie`.
5. При совпадении вызывается `ICommand.Execute(...)`.

## Структура проекта

```text
GridCasting/
├── GridCasting/            # Основная библиотека
├── GridCastingTests/       # NUnit тесты
└── GridCasting.sln         # Решение
```

Внутри библиотеки основные папки:

```text
GridCasting/
├── Executor/
├── Models/
├── Transform/
└── Utils/
```

## Что читать дальше

- [Архитектура](../architecture/) — если нужен обзор внутреннего конвейера
- [Справочник API](../api-reference/) — если нужны основные типы и точки расширения
- [Конфигурация](../configuration/) — если нужна структура решения и зависимости
