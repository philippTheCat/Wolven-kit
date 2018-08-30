using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WolvenKit.FlowTreeEditors;

public abstract class Graph : IEnumerable<GraphNode>
{
    private readonly List<GraphNode> m_Nodes = new List<GraphNode>();

    public ReadOnlyCollection<GraphNode> nodes
    {
        get { return m_Nodes.AsReadOnly(); }
    }

    protected class NodeWeight
    {
        public object node { get; set; }
        public float weight { get; set; }
    }

    // Derived class should specify the children of a given node.
    protected abstract IEnumerable<GraphNode> GetChildren(GraphNode node);

    // Derived class should implement how to populate this graph (usually by calling AddNodeHierarchy()).
    protected abstract void Populate();

    public void AddNodeHierarchy(GraphNode root)
    {
        AddNode(root);

        IEnumerable<GraphNode> children = GetChildren(root);
        if (children == null)
            return;

        foreach (GraphNode child in children)
        {
            if (!m_Nodes.Contains(child)) {
                root.AddChild(child);
                AddNodeHierarchy(child);
            }
        }
    }

    public void AddNode(GraphNode node)
    {
        m_Nodes.Add(node);
    }

    public void Clear()
    {
        m_Nodes.Clear();
    }

    public void Refresh()
    {
        // TODO optimize?
        Clear();
        Populate();
    }

    public IEnumerator<GraphNode> GetEnumerator()
    {
        return m_Nodes.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return m_Nodes.GetEnumerator();
    }

    public bool IsEmpty()
    {
        return m_Nodes.Count == 0;
    }
}