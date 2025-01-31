﻿#pragma warning disable IDE0073
//based on: https://www.geeksforgeeks.org/depth-first-search-or-dfs-for-a-graph/
//with modification


namespace OData2Poco.graph;

// This class represents a directed graph using adjacency list representation
internal class Graph
{
    // Array of lists for Adjacency List Representation
    private readonly List<int>[] _adj;
    private readonly int _v; // No. of vertices

    public Graph(int v)
    {
        _v = v;
        _adj = new List<int>[v];
        for (var i = 0; i < v; ++i)
            _adj[i] = new List<int>();
    }

    // Add an edge into the graph
    internal void AddEdge(int v, int w)
    {
        _adj[v].Add(w); // Add w to v's list.
    }

    // Used by DFS
    private void DfsUtil(int v, bool[] visited, List<int> found)
    {
        // Mark the current node as visited             
        visited[v] = true;

        // Recur for all the vertices adjacent to this vertex
        var vList = _adj[v];
        foreach (var n in vList)
            if (!visited[n])
            {
                found.Add(n);
                DfsUtil(n, visited, found);
            }
    }

    // DFS traversal, It uses recursive DFSUtil()
    public List<int> Dfs(int v)
    {
        List<int> found = new();

        // Mark all the vertices as not visited (set as false by default)
        var visited = new bool[_v];

        // Call the recursive helper function for traversal
        DfsUtil(v, visited, found);
        return found;
    }
}