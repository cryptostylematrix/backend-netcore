namespace LegacyPlacesXmindExporter;

public sealed record Place(long Id, long? ParentId, short Position, short Type, string Login, DateTime CreatedAt);

public sealed class PlaceNode(Place place)
{
    public Place Place { get; } = place;
    public List<PlaceNode> Children { get; } = [];
}

public static class PlaceHierarchy
{
    public static IReadOnlyList<PlaceNode> Build(IReadOnlyList<Place> places, CancellationToken cancellationToken = default, ExportProgress? progress = null, long? rootPlaceId = null)
    {
        if (places.Count == 0)
            throw new InvalidDataException("В выбранной структуре нет мест.");
        progress?.Start("Индексация мест", places.Count);
        var nodes = new Dictionary<long, PlaceNode>(places.Count);
        foreach (var place in places)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (place.Type is not (0 or 1))
                throw new InvalidDataException($"Место {place.Id}: неизвестный p_type {place.Type}.");
            if (place.Position < 0)
                throw new InvalidDataException($"Место {place.Id}: отрицательная позиция.");
            if (!nodes.TryAdd(place.Id, new PlaceNode(place)))
                throw new InvalidDataException($"Повторяется place_id {place.Id}.");
            progress?.Advance();
        }

        if (rootPlaceId is { } selectedId)
        {
            if (!nodes.TryGetValue(selectedId, out var selected))
                throw new InvalidDataException($"Начальное место {selectedId} отсутствует в выгрузке.");
            // In a descendant-only result, the root's parent can be present only in a cycle.
            if (selected.Place.ParentId is { } parentId && nodes.ContainsKey(parentId))
                throw new InvalidDataException($"Обнаружен цикл parent_id через начальное место {selectedId}.");
        }

        var roots = new List<PlaceNode>();
        progress?.Complete();
        progress?.Start("Построение связей", places.Count);
        foreach (var node in nodes.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node.Place.Id == rootPlaceId || node.Place.ParentId is not { } parentId)
                roots.Add(node);
            else
            {
                if (!nodes.TryGetValue(parentId, out var parent))
                    throw new InvalidDataException($"Место {node.Place.Id}: родитель {parentId} отсутствует в выбранной структуре.");
                parent.Children.Add(node);
            }
            progress?.Advance();
        }

        static int Compare(PlaceNode a, PlaceNode b)
        {
            var position = a.Place.Position.CompareTo(b.Place.Position);
            return position != 0 ? position : a.Place.Id.CompareTo(b.Place.Id);
        }
        progress?.Complete();
        progress?.Start("Сортировка позиций", places.Count);
        roots.Sort(Compare);
        foreach (var node in nodes.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            node.Children.Sort(Compare);
            for (var i = 1; i < node.Children.Count; i++)
                if (node.Children[i - 1].Place.Position == node.Children[i].Place.Position)
                    throw new InvalidDataException($"У родителя {node.Place.Id} повторяется позиция {node.Children[i].Place.Position}.");
            progress?.Advance();
        }
        progress?.Complete();
        progress?.Start("Проверка дерева", places.Count);

        var pending = new Stack<PlaceNode>(roots);
        var visited = 0;
        while (pending.TryPop(out var item))
        {
            cancellationToken.ThrowIfCancellationRequested();
            visited++;
            progress?.Advance();
            foreach (var child in item.Children)
                pending.Push(child);
        }
        // Each node has at most one parent. Components unreachable from roots contain a cycle.
        if (visited != places.Count)
            throw new InvalidDataException($"Обнаружен цикл parent_id: {places.Count - visited} мест недоступны от корней.");
        progress?.Complete();
        return roots;
    }
}
