---
title: Справочник API
order: 6
---

# Справочник API GridCasting

## Точка входа

### GridCasting

Главный фасад библиотеки.

```csharp
public class GridCasting(
    GridGraph graph,
    float sensitivity,
    params IEnumerable<IPathTransform> transforms)
```

**Основной метод:**

```csharp
public void Execute(FVector2[] positions)
```

## Преобразование ввода

### GridResolver

Класс для построения пути и работы с графовой моделью.

**Основные методы:**

- `GetPath(FVector2[] positions)`
- `CreateGridGraph(Grid grid)`
- `VerifyGridGraph(GridGraph graph)`
- `GenerateGrid(FBox2 range)`
- `GetGridPositions(FBox2 range)`

### IPathTransform

Интерфейс преобразования маршрутов:

```csharp
public interface IPathTransform
{
    public bool IsRequired { get; }
    public Path Transform(GridGraph graph, Path path);
    public Path Reverse(GridGraph graph, Path path);
}
```

## Исполнение

### PathExecutor

Класс, отвечающий за регистрацию и выполнение команд.

**Основные методы:**

- `AddCommand(ICommand command, Path pattern, bool patternFamily = false)`
- `Execute(Path path)`
- `AddEnvironmentResolver(IEnvironmentResolver resolver)`
- `RemoveEnvironmentResolver(IEnvironmentResolver resolver)`
- `ResetStack()`

### ICommand

Интерфейс прикладной команды:

```csharp
public interface ICommand
{
    void Execute(CommandContext context);
}
```

### CommandContext

Контекст, передаваемый в исполняемую команду.

### IEnvironmentResolver

Интерфейс интеграции с внешним окружением.

## Модели данных

### Path

Дискретное представление маршрута пользователя.

### Grid

Прикладная сеточная структура, состоящая из `GridNode`.

### GridGraph

Абстрактный граф сетки, включающий:

- `GridGraph`
- `GridGraphNode`
- `GridGraphEdge`

## Вспомогательные структуры

### Trie<TKey, TValue>

Префиксная структура хранения паттернов команд.

### ListenableDictionary<TKey, TValue>

Словарь с событиями изменения, используемый для окружения исполнения.

### PointBVH2D<T>

Пространственный индекс для поиска ближайших точек.
