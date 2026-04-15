using GridCasting.Utils.BVH.Point;
using IgdrasilEngine.Engine.Math.Vectors;

namespace GridCasting.Utils.BVH;

/// <summary>
/// Базовый класс для точек, хранящихся в BVH дереве в 2D пространстве.
/// </summary>
/// <typeparam name="T">Тип точек в дереве BVH.</typeparam>
public abstract class PointBVH2DTransform<T> where T : PointBVH2DTransform<T>
{
    /// <summary>
    /// Позиция точки в дереве BVH.
    /// </summary>
    public abstract FVector2 TreePosition { get; protected set; }
    /// <summary>
    /// Лист BVH дерева, в котором находится эта точка.
    /// </summary>
    public PointBVH2D<T>.Leaf? Location { get; protected internal set; }

    /// <summary>
    /// Находит все точки в пределах заданного радиуса от позиции этой точки и добавляет их в предоставленный список.
    /// </summary>
    /// <param name="radius">Радиус поиска.</param>
    /// <param name="result">Список для добавления найденных точек.</param>
    public void FindNearest(float radius, ref List<T> result) => Location?.FindNearestBwd(TreePosition, radius, result);

    /// <summary>
    /// Находит все точки в пределах заданного радиуса от позиции этой точки.
    /// </summary>
    /// <param name="radius">Радиус поиска.</param>
    /// <returns>Список найденных точек.</returns>
    public List<T> FindNearest(float radius)
    {
        var result = new List<T>();
        FindNearest(radius, ref result);
        return result;
    }
}