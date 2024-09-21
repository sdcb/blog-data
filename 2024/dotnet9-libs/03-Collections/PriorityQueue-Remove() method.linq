<Query Kind="Program">
  <RuntimeVersion>9.0</RuntimeVersion>
</Query>

// 这个示例演示如何使用 PriorityQueue 进行Dijkstra算法的实现
// 图片：https://dreampuf.github.io/GraphvizOnline/#digraph%20G%20%7B%0D%0A%20%20%20%20A%20-%3E%20B%20%5Blabel%3D%221%22%5D%3B%0D%0A%20%20%20%20A%20-%3E%20C%20%5Blabel%3D%224%22%5D%3B%0D%0A%20%20%20%20B%20-%3E%20A%20%5Blabel%3D%221%22%5D%3B%0D%0A%20%20%20%20B%20-%3E%20C%20%5Blabel%3D%222%22%5D%3B%0D%0A%20%20%20%20B%20-%3E%20D%20%5Blabel%3D%225%22%5D%3B%0D%0A%20%20%20%20C%20-%3E%20A%20%5Blabel%3D%224%22%5D%3B%0D%0A%20%20%20%20C%20-%3E%20B%20%5Blabel%3D%222%22%5D%3B%0D%0A%20%20%20%20C%20-%3E%20D%20%5Blabel%3D%221%22%5D%3B%0D%0A%20%20%20%20D%20-%3E%20B%20%5Blabel%3D%225%22%5D%3B%0D%0A%20%20%20%20D%20-%3E%20C%20%5Blabel%3D%221%22%5D%3B%0D%0A%7D
class Program
{
    static void Main()
    {
        var graph = new Dictionary<string, List<(string, int)>>
        {
            ["A"] = new List<(string, int)> {("B", 1), ("C", 4)},
            ["B"] = new List<(string, int)> {("A", 1), ("C", 2), ("D", 5)},
            ["C"] = new List<(string, int)> {("A", 4), ("B", 2), ("D", 1)},
            ["D"] = new List<(string, int)> {("B", 5), ("C", 1)}
        };

        var shortestPaths = Dijkstra(graph, "A");

        // 输出从起点到其他节点的最短路径
        foreach (var path in shortestPaths)
        {
            Console.WriteLine($"{path.Key}: {path.Value}");
        }
    }

	// Dijkstra算法实现
	static Dictionary<string, int> Dijkstra(Dictionary<string, List<(string, int)>> graph, string start)
	{
		var priorityQueue = new PriorityQueue<string, int>();
		var distances = new Dictionary<string, int>();

		// 初始化起点距离为0，其他节点为无穷大，并加入优先队列
		foreach (var node in graph.Keys)
		{
			distances[node] = int.MaxValue;
		}
		distances[start] = 0;
		priorityQueue.Enqueue(start, 0);

		while (priorityQueue.Count > 0)
		{
			// 从优先队列中提取具有最小优先级（最短路径）的元素
			var current = priorityQueue.Dequeue();

			// 遍历当前节点的邻居节点
			foreach (var (neighbor, weight) in graph[current])
			{
				int newDist = distances[current] + weight;

				// 如果发现更短路径，则更新该节点的距离并更新优先队列
				if (newDist < distances[neighbor])
				{
					distances[neighbor] = newDist;
					priorityQueue.UpdatePriority(neighbor, newDist);
				}
			}
		}

		return distances;
	}
}

// 使用扩展方法来更新队列中的优先级
public static class PriorityQueueExtensions
{
	public static void UpdatePriority<TElement, TPriority>(
		this PriorityQueue<TElement, TPriority> queue,
		TElement element,
		TPriority priority)
	{
		// 这里使用 Remove 方法移除当前元素，模拟优先级更新，O(n): search, O(1): remove
		queue.Remove(element, out _, out _);
		// 重新以新的优先级插入元素
		queue.Enqueue(element, priority);
	}
}