using GridCasting.Utils.BVH.Point;
using IgdrasilEngine.Engine.Math.Boxes;
using IgdrasilEngine.Engine.Math.Vectors;

namespace GridCasting.Utils.BVH;

/// <summary>
/// Дерево BVH для точек в 2D пространстве.
/// </summary>
/// <typeparam name="T">Тип точек в дереве BVH.</typeparam>
public class PointBVH2D<T> : IReadOnlyPointBVH2D<T> where T : PointBVH2DTransform<T>
{
    /// <summary>
    /// Стек для обхода дерева BVH.
    /// </summary>
    private readonly Stack<Node> _stack = new();
    /// <summary>
    /// Корневой узел BVH дерева.
    /// </summary>
    private readonly Branch _root;
    /// <summary>
    /// Инициализирует новый экземпляр дерева BVH для точек в 2D пространстве.
    /// </summary>
    public PointBVH2D()
    {
        _root = new Branch(null, default!);
        _root.Root = _root;
    }

    /// <summary>
    /// Глубина BVH дерева.
    /// </summary>
    /// <returns>Глубина дерева.</returns>
    public uint Depth() => _root.Depth();

    /// <summary>
    /// Добавляет точку в BVH дерево.
    /// </summary>
    /// <param name="value">Точка для добавления.</param>
    public void Add(T value)
    {
        lock (_root)
        {
            _root.Add(value);
        }
    }

    /// <summary>
    /// Оптимизированно добавляет точку в BVH дерево.
    /// </summary>
    /// <param name="value">Точка для добавления.</param>
    public void OptimizedAdd(T value)
    {
        lock (_root)
        {
            _root.OptimizedAdd(value);
        }
    }

    /// <summary>
    /// Удаляет точку из BVH дерева.
    /// </summary>
    /// <param name="value">Точка для удаления.</param>
    public void Remove(T value)
    {
        lock (_root)
        {
            _root.Remove(value);
        }
    }

    /// <summary>
    /// Находит все точки в пределах заданного радиуса от указанной позиции и добавляет их в предоставленный список.
    /// </summary>
    /// <param name="position">Позиция для поиска ближайших точек.</param>
    /// <param name="radius">Радиус поиска.</param>
    /// <param name="result">Список для добавления найденных точек.</param>
    public void FindNearest(FVector2 position, float radius, List<T> result)
    {
        lock (_root)
        {
            _root.FindNearestFwd(position, radius, result);
        }
    }

    /// <summary>
    /// Находит все точки в пределах заданного радиуса от указанной позиции.
    /// </summary>
    /// <param name="position">Позиция для поиска ближайших точек.</param>
    /// <param name="radius">Радиус поиска.</param>
    /// <returns>Список найденных точек.</returns>
    public List<T> FindNearest(FVector2 position, float radius)
    {
        var result = new List<T>();
        FindNearest(position, radius, result);
        return result;
    }
    /// <summary>
    /// Очищает BVH дерево, удаляя все точки.
    /// </summary>
    public void Clear()
    {
        lock (_root)
        {
            _root.Left = null;
            _root.Right = null;
            _root.AABB = new FBox2(FVector2.Zero, FVector2.Zero);
        }
    }

    /// <summary>
    /// Получает граничный прямоугольник, охватывающий все точки в BVH дереве.
    /// </summary>
    /// <returns>Граничный прямоугольник.</returns>
    public FBox2 GetBoundaryBox() => _root.AABB;

    /// <summary>
    /// Базовый класс для узлов BVH дерева.
    /// </summary>
    public abstract class Node
    {
        /// <summary>
        /// Корень BVH дерева.
        /// </summary>
        protected internal Branch Root;
        /// <summary>
        /// Родительский узел BVH дерева.
        /// </summary>
        protected internal Branch? Parent;
        /// <summary>
        /// Ограничивающий прямоугольник узла BVH дерева.
        /// </summary>
        protected internal FBox2 AABB;

        /// <summary>
        /// Инициализирует новый экземпляр узла BVH дерева.
        /// </summary>
        /// <param name="aabb">Ограничивающий прямоугольник узла.</param>
        /// <param name="parent">Родительский узел.</param>
        /// <param name="root">Корень дерева.</param>
        protected Node(FBox2 aabb, Branch? parent, Branch root)
        {
            AABB = aabb;
            Parent = parent;
            Root = root;
        }

        /// <summary>
        /// Глубина BVH дерева.
        /// </summary>
        /// <returns>Глубина дерева.</returns>
        public abstract uint Depth();

        /// <summary>
        /// Добавляет точку в BVH дерево.
        /// </summary>
        /// <param name="value">Точка для добавления.</param>
        public abstract void Add(T value);
        /// <summary>
        /// Добавляет точку в BVH дерево.
        /// </summary>
        /// <param name="value">Точка для добавления.</param>
        /// <param name="stack">Стек узлов для оптимизации добавления.</param>
        public abstract void Add(T value, Stack<Node> stack);
        /// <summary>
        /// Оптимизированно добавляет точку в BVH дерево.
        /// </summary>
        /// <param name="value">Точка для добавления.</param>
        public abstract void OptimizedAdd(T value);
        /// <summary>
        /// Оптимизированно добавляет точку в BVH дерево.
        /// </summary>
        /// <param name="value">Точка для добавления.</param>
        /// <param name="stack">Стек узлов для оптимизации добавления.</param>
        public abstract void OptimizedAdd(T value, Stack<Node> stack);
        /// <summary>
        /// Удаляет точку из BVH дерева.
        /// </summary>
        /// <param name="value">Точка для удаления.</param>
        public abstract void Remove(T value);
        /// <summary>
        /// Удаляет точку из BVH дерева.
        /// </summary>
        /// <param name="value">Точка для удаления.</param>
        /// <param name="stack">Стек узлов для оптимизации удаления.</param>
        public abstract void Remove(T value, Stack<Node> stack);
        /// <summary>
        /// Находит все точки в пределах заданного радиуса от указанной позиции.
        /// </summary>
        /// <param name="position">Позиция для поиска ближайших точек.</param>
        /// <param name="radius">Радиус поиска.</param>
        /// <param name="result">Список для хранения найденных точек.</param>
        public abstract void FindNearestFwd(FVector2 position, float radius, List<T> result);
        /// <summary>
        /// Находит все точки в пределах заданного радиуса от указанной позиции.
        /// </summary>
        /// <param name="position">Позиция для поиска ближайших точек.</param>
        /// <param name="radius">Радиус поиска.</param>
        /// <param name="result">Список для хранения найденных точек.</param>
        /// <param name="stack">Стек узлов для оптимизации поиска.</param>
        public abstract void FindNearestFwd(FVector2 position, float radius, List<T> result, Stack<Node> stack);
        /// <summary>
        /// Находит все точки в пределах заданного радиуса от указанной позиции, обходя дерево вверх.
        /// </summary>
        /// <param name="position">Позиция для поиска ближайших точек.</param>
        /// <param name="radius">Радиус поиска.</param>
        /// <param name="result">Список для хранения найденных точек.</param>
        public void FindNearestBwd(FVector2 position, float radius, List<T> result)
        {
            lock (Root)
            {
                if (Parent == null) return;
                if (Parent.Left == this)
                    Parent.Right?.FindNearestFwd(position, radius, result);
                else
                    Parent.Left?.FindNearestFwd(position, radius, result);
                Parent.FindNearestBwd(position, radius, result);
            }
        }

        /// <summary>
        /// Удаляет текущий узел из BVH дерева.
        /// </summary>
        protected void RemoveCurrentNode()
        {
            if (Parent == null) return;
            if (Parent.Left == this)
            {
                Parent.Left = null;
                if (Parent.Right == null)
                    Parent.RemoveCurrentNode();
                else
                    Parent.Replace(Parent.Right);
            }
            else
            {
                Parent.Right = null;
                if (Parent.Left == null)
                    Parent.RemoveCurrentNode();
                else
                    Parent.Replace(Parent.Left);

            }
        }

        /// <summary>
        /// Заменяет текущий узел на указанный узел в BVH дереве
        /// </summary>
        /// <param name="node">Узел для замены.</param>
        protected void Replace(Node node)
        {
            if (Parent == null) return;
            if (Parent.Left == this)
                Parent.Left = node;
            else
                Parent.Right = node;
            node.Parent = Parent;
            Parent.UpdateAABB();
        }
    }

    /// <summary>
    /// Ветвь BVH дерева.
    /// </summary>
    public class Branch : Node
    {
        /// <summary>
        /// Левый дочерний узел ветви BVH дерева.
        /// </summary>
        protected internal Node? Left;
        /// <summary>
        /// Правый дочерний узел ветви BVH дерева.
        /// </summary>
        protected internal Node? Right;

        /// <summary>
        /// Инициализирует новый экземпляр ветви BVH дерева.
        /// </summary>
        /// <param name="parent">Родительская ветвь.</param>
        /// <param name="root">Корневая ветвь.</param>
        public Branch(Branch? parent, Branch root) : base(new FBox2(FVector2.Zero, FVector2.Zero), parent, root)
        {
        }

        /// <summary>
        /// Глубина BVH дерева.
        /// </summary>
        /// <returns>Глубина дерева.</returns>
        public override uint Depth()
        {
            var left = Left?.Depth() ?? 0;
            var right = Right?.Depth() ?? 0;
            return System.Math.Max(left, right) + 1;
        }

        /// <summary>
        /// Добавляет точку в BVH дерево.
        /// </summary>
        /// <param name="value">Точка для добавления.</param>
        public override void Add(T value)
        {
            var left = Left == null ? 0 : FBox2.Distance(Left.AABB, value.TreePosition);
            var right = Right == null ? 0 : FBox2.Distance(Right.AABB, value.TreePosition);

            if (left < right)
            {
                if (Left == null)
                    Left = new Leaf(this, Root, value);
                else Left.Add(value);
            }
            else
            {
                if (Right == null)
                    Right = new Leaf(this, Root, value);
                else Right.Add(value);
            }

            UpdateAABB();
        }

        /// <summary>
        /// Добавляет точку в BVH дерево.
        /// </summary>
        /// <param name="value">Точка для добавления.</param>
        /// <param name="stack">Стек узлов для обхода.</param>
        public override void Add(T value, Stack<Node> stack)
        {
            var left = Left == null ? 0 : FBox2.Distance(Left.AABB, value.TreePosition);
            var right = Right == null ? 0 : FBox2.Distance(Right.AABB, value.TreePosition);

            if (left < right)
            {
                if (Left == null)
                    Left = new Leaf(this, Root, value);
                else stack.Push(Left);
            }
            else
            {
                if (Right == null)
                    Right = new Leaf(this, Root, value);
                else stack.Push(Right);
            }
        }

        /// <summary>
        /// Добавляет точку в BVH дерево.
        /// </summary>
        /// <param name="value">Точка для добавления.</param>
        public override void OptimizedAdd(T value)
        {
            var left = Left == null ? 0 : FBox2.Distance(Left.AABB, value.TreePosition);
            var right = Right == null ? 0 : FBox2.Distance(Right.AABB, value.TreePosition);

            if (left < right)
            {
                if (Left == null)
                {
                    Left = new Leaf(this, Root, value);
                    Balance();
                    UpdateAABB();
                }
                else if (left <= 0)
                    Left.OptimizedAdd(value);
                else
                {
                    var node = new Branch(this, Root);
                    node.Left = new Leaf(node, Root, value);
                    node.Right = Left;
                    Left.Parent = node;
                    Left = node;
                    node.Balance();
                    node.UpdateAABB();
                }
            }
            else
            {
                if (Right == null)
                {
                    Right = new Leaf(this, Root, value);
                    Balance();
                    UpdateAABB();
                }
                else if (right <= 0)
                    Right.OptimizedAdd(value);
                else
                {
                    var node = new Branch(this, Root);
                    node.Left = new Leaf(node, Root, value);
                    node.Right = Right;
                    Right.Parent = node;
                    Right = node;
                    node.Balance();
                    node.UpdateAABB();
                }
            }

            UpdateAABB();
        }

        /// <summary>
        /// Добавляет точку в BVH дерево.
        /// </summary>
        /// <param name="value">Точка для добавления.</param>
        /// <param name="stack">Стек узлов для обхода.</param>
        public override void OptimizedAdd(T value, Stack<Node> stack)
        {
            var left = Left == null ? 0 : FBox2.Distance(Left.AABB, value.TreePosition);
            var right = Right == null ? 0 : FBox2.Distance(Right.AABB, value.TreePosition);

            if (left < right)
            {
                if (Left == null)
                    Left = new Leaf(this, Root, value);
                else if (left <= 0)
                    stack.Push(Left);
                else
                {
                    var node = new Branch(Left.Parent, Root);
                    node.Left = new Leaf(node, Root, value);
                    node.Right = Left;
                    Left.Parent = node;
                    Left = node;
                    node.Balance();
                    node.UpdateAABB();
                }
            }
            else
            {
                if (Right == null)
                    Right = new Leaf(this, Root, value);
                else if (right <= 0)
                    stack.Push(Right);
                else
                {
                    var node = new Branch(Right.Parent, Root);
                    node.Left = new Leaf(node, Root, value);
                    node.Right = Right;
                    Right.Parent = node;
                    Right = node;
                    node.Balance();
                    node.UpdateAABB();
                }
            }
        }

        /// <summary>
        /// Балансирует BVH дерево, удаляя пустые узлы.
        /// </summary>
        private void Balance() => RemoveHoles();

        /// <summary>
        /// Удаляет пустые узлы из BVH дерева.
        /// </summary>
        private void RemoveHoles()
        {
            if (Left == null)
            {
                if (Right == null)
                    RemoveCurrentNode();
                else
                {
                    if (Right is Branch br)
                        br.RemoveHoles();
                    Replace(Right);
                }

                return;
            }

            if (Right == null)
            {
                if (Left == null)
                    RemoveCurrentNode();
                else
                {
                    if (Left is Branch br)
                        br.RemoveHoles();
                    Replace(Left);
                }

                return;
            }
        }

        /// <summary>
        /// Удаляет точку из BVH дерева.
        /// </summary>
        /// <param name="value">Точка для удаления.</param>
        public override void Remove(T value)
        {

            Left?.Remove(value);
            Right?.Remove(value);
            if (Left == null && Right == null)
                RemoveCurrentNode();
            else UpdateAABB();
        }
        /// <summary>
        /// Удаляет точку из BVH дерева.
        /// </summary>
        /// <param name="value">Точка для удаления.</param>
        /// <param name="stack">Стек узлов для обхода.</param>
        public override void Remove(T value, Stack<Node> stack)
        {
            if (Left != null) stack.Push(Left);
            if (Right != null) stack.Push(Right);
        }
        /// <summary>
        /// Находит все точки в пределах заданного радиуса от указанной позиции.
        /// </summary>
        /// <param name="position">Позиция центра поиска.</param>
        /// <param name="radius">Радиус поиска.</param>
        /// <param name="result">Список для хранения найденных точек.</param>
        public override void FindNearestFwd(FVector2 position, float radius, List<T> result)
        {
            if (!FBox2.SphereIntersection(AABB, position, radius)) return;
            Left?.FindNearestFwd(position, radius, result);
            Right?.FindNearestFwd(position, radius, result);
        }
        /// <summary>
        /// Находит все точки в пределах заданного радиуса от указанной позиции.
        /// </summary>
        /// <param name="position">Позиция центра поиска.</param>
        /// <param name="radius">Радиус поиска.</param>
        /// <param name="result">Список для хранения найденных точек.</param>
        /// <param name="stack">Стек узлов для обхода.</param>
        public override void FindNearestFwd(FVector2 position, float radius, List<T> result, Stack<Node> stack)
        {
            if (!FBox2.SphereIntersection(AABB, position, radius)) return;
            if (Left != null) stack.Push(Left);
            if (Right != null) stack.Push(Right);
        }
        
        /// <summary>
        /// Обновляет ограничивающий прямоугольник узла BVH дерева.
        /// </summary>
        protected internal void UpdateAABB()
        {
            if (Left == null && Right == null) return;
            var left = Left?.AABB ?? Right!.AABB;
            var right = Right?.AABB ?? Left!.AABB;
            AABB = FBox2.Union(left, right);
        }
        /// <summary>
        /// Перемещает точку в BVH дереве.
        /// </summary>
        /// <param name="value">Точка для перемещения.</param>
        public void Relocate(T value)
        {
            if (Parent == null)
            {
                OptimizedAdd(value);
                return;
            }

            if (Left == null && Right == null)
                RemoveCurrentNode();
            else Parent.UpdateAABB();

            if (Parent.AABB.ContainsInclusive(value.TreePosition))
            {
                Parent.OptimizedAdd(value);
                return;
            }

            Parent.Relocate(value);
        }
    }
    /// <summary>
    /// Лист BVH дерева.
    /// </summary>
    public class Leaf : Node
    {
        /// <summary>
        /// Значение точки, хранящейся в листе BVH дерева.
        /// </summary>
        private readonly T _value;
        /// <summary>
        /// Инициализирует новый экземпляр листа BVH дерева.
        /// </summary>
        /// <param name="parent">Родительский узел.</param>
        /// <param name="root">Корень дерева.</param>
        /// <param name="value">Значение точки.</param>
        public Leaf(Branch? parent, Branch root, T value) : base(new FBox2(value.TreePosition, value.TreePosition),
            parent, root)
        {
            _value = value;
            _value.Location = this;
        }

        /// <summary>
        /// Глубина BVH дерева.
        /// </summary>
        /// <returns>Глубина дерева.</returns>
        public override uint Depth() => 1;
        /// <summary>
        /// Добавляет точку в BVH дерево.
        /// </summary>
        /// <param name="value">Точка для добавления.</param>
        public override void Add(T value) => OptimizedAdd(value);
        /// <summary>
        /// Добавляет точку в BVH дерево.
        /// </summary>
        /// <param name="value">Точка для добавления.</param>
        /// <param name="stack">Стек узлов.</param>
        public override void Add(T value, Stack<Node> stack) => OptimizedAdd(value);
        /// <summary>
        /// Перемещает точку в BVH дереве.
        /// </summary>
        public void Relocate()
        {
            lock (Root)
            {
                // if (Parent == null)
                // {
                //     OptimizedAdd(_value);
                //     return;
                // }
                Root.Remove(_value);
                Root.OptimizedAdd(_value);
                // Parent.UpdateAABB();
                //
                // if (Parent.AABB.ContainsInclusive(_value.TreePosition))
                // {
                //     Parent.OptimizedAdd(_value);
                //     return;
                // }
                // Parent.Relocate(_value);
            }
        }
        /// <summary>
        /// Оптимизированно добавляет точку в BVH дерево.
        /// </summary>
        /// <param name="value">Точка для добавления.</param>
        public override void OptimizedAdd(T value)
        {
            if (Parent == null) return;
            if (Parent.Left == this)
            {
                var node = new Branch(Parent, Root);
                node.Left = new Leaf(node, Root, value);
                node.Right = this;
                Parent.Left = node;
                Parent = node;
                node.UpdateAABB();
            }
            else
            {
                var node = new Branch(Parent, Root);
                node.Left = new Leaf(node, Root, value);
                node.Right = this;
                Parent.Right = node;
                Parent = node;
                node.UpdateAABB();
            }
        }

        /// <summary>
        /// Оптимизированно добавляет точку в BVH дерево.
        /// </summary>
        /// <param name="value">Точка для добавления.</param>
        /// <param name="stack">Стек узлов.</param>
        public override void OptimizedAdd(T value, Stack<Node> stack) => OptimizedAdd(value);

        /// <summary>
        /// Удаляет точку из BVH дерева.
        /// </summary>
        /// <param name="value">Точка для удаления.</param>
        public override void Remove(T value)
        {
            if (!_value.Equals(value)) return;
            RemoveCurrentNode();
        }

        /// <summary>
        /// Удаляет точку из BVH дерева.
        /// </summary>
        /// <param name="value">Точка для удаления.</param>
        /// <param name="stack">Стек узлов.</param>
        public override void Remove(T value, Stack<Node> stack) => Remove(value);

        /// <summary>
        /// Находит все точки в пределах заданного радиуса от указанной позиции.
        /// </summary>
        /// <param name="position">Позиция для поиска.</param>
        /// <param name="radius">Радиус поиска.</param>
        /// <param name="result">Список для хранения найденных точек.</param>
        public override void FindNearestFwd(FVector2 position, float radius, List<T> result)
        {
            if (FVector2.Distance(position, _value.TreePosition) > radius) return;
            result.Add(_value);
        }
        /// <summary>
        /// Находит все точки в пределах заданного радиуса от указанной позиции.
        /// </summary>
        /// <param name="position">Позиция для поиска.</param>
        /// <param name="radius">Радиус поиска.</param>
        /// <param name="result">Список для хранения найденных точек.</param>
        /// <param name="stack">Стек узлов.</param>
        public override void FindNearestFwd(FVector2 position, float radius, List<T> result, Stack<Node> stack) => FindNearestFwd(position, radius, result);
    }
}