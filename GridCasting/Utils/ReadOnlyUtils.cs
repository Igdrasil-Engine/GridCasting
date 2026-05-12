namespace GridCasting.Utils;

internal static class ReadOnlyUtils
{
    /// <summary>
    /// Находит индекс элемента в IReadOnly коллекции
    /// </summary>
    /// <typeparam name="T">Тип элементов коллекции</typeparam>
    /// <param name="self">Коллекция для поиска</param>
    /// <param name="elementToFind">Элемент для поиска</param>
    /// <returns>Индекс найденного элемента или -1, если элемент не найден</returns>
    public static int IndexOf<T>( this IEnumerable<T> self, T elementToFind )
    {
        var i = 0;
        foreach(var element in self )
        {
            if( Equals( element, elementToFind ) )
                return i;
            i++;
        }
        return -1;
    }
}