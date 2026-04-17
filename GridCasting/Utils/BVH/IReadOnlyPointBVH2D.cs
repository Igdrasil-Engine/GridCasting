using IgdrasilEngine.Engine.Math.Boxes;
using IgdrasilEngine.Engine.Math.Vectors;

namespace GridCasting.Utils.BVH;
/// <summary>
/// Интерфейс для чтения BVH дерева точек в 2D пространстве
/// </summary>
/// <typeparam name="T">Тип точек в дереве BVH.</typeparam>
public interface IReadOnlyPointBVH2D<T> where T : PointBVH2DTransform<T>
{
    /// <summary>
    /// Глубина BVH дерева.
    /// </summary>
    /// <returns>Глубина дерева.</returns>
    public uint Depth();
    /// <summary>
    /// Находит все точки в пределах заданного радиуса от указанной позиции.
    /// </summary>
    /// <param name="position">Позиция для поиска ближайших точек.</param>
    /// <param name="radius">Радиус поиска.</param>
    /// <returns>Список точек, найденных в пределах радиуса.</returns>
    public List<T> FindNearest(FVector2 position, float radius);

    /// <summary>
    /// Finds the nearest point in the BVH tree to the specified position.
    /// </summary>
    /// <param name="position">The position for which the nearest point is to be found.</param>
    /// <returns>The nearest point of type <typeparamref name="T"/> to the specified position.</returns>
    public T? FindNearest(FVector2 position);

    /// <summary>
    /// Находит все точки в пределах заданного радиуса от указанной позиции и добавляет их в предоставленный список.
    /// </summary>
    /// <param name="position">Позиция для поиска ближайших точек.</param>
    /// <param name="radius">Радиус поиска.</param>
    /// <param name="result">Список для добавления найденных точек.</param>
    public void FindNearest(FVector2 position, float radius, List<T> result);
    /// <summary>
    /// Получает граничный прямоугольник, охватывающий все точки в BVH дереве.
    /// </summary>
    /// <returns>Граничный прямоугольник.</returns>
    public FBox2 GetBoundaryBox();
}