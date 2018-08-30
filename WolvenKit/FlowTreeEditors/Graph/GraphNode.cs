using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using WolvenKit.CR2W;

namespace WolvenKit.FlowTreeEditors {
    public class GraphNode: IEquatable<GraphNode>
    {
        public CR2WChunk content { get; private set; }
        public float weight { get; set; }
        public bool active { get; private set; }
        public GraphNode parent { get; private set; }
        public EditorNode editorNode { get; private set; }
        public IList<GraphNode> children { get; private set; }

        public GraphNode(CR2WChunk content, float weight = 1.0f, bool active = false)
        {
            this.content = content;
            this.weight = weight;
            this.active = active;
            children = new List<GraphNode>();
            this.editorNode = new QuestPhaseEditor(content);
        }

        public void AddChild(GraphNode child)
        {
            if (child == this) throw new Exception("Circular graphs not supported.");
            if (child.parent == this) return;

            children.Add(child);
            child.parent = this;
        }

        public int depth
        {
            get { return GetDepthRecursive(this); }
        }

        private static int GetDepthRecursive(GraphNode node)
        {
            if (node.parent == null) return 1;
            return 1 + GetDepthRecursive(node.parent);
        }

        public virtual Type GetContentType()
        {
            return content == null ? null : content.GetType();
        }

        public virtual string GetContentTypeName()
        {
            Type type = GetContentType();
            return type == null ? "Null" : type.ToString();
        }

        public virtual string GetContentTypeShortName()
        {
            return GetContentTypeName().Split('.').Last();
        }

        public override string ToString()
        {
            return "Node content: " + GetContentTypeName();
        }

        public virtual Color GetColor()
        {
            Type type = GetContentType();
            if (type == null)
                return Color.Red;

            string shortName = type.ToString().Split('.').Last();
            float h = (float)Math.Abs(shortName.GetHashCode()) / int.MaxValue;
            return Color.Aqua;
        }


        public bool Equals(GraphNode other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(content.crc, other.content.crc);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((GraphNode) obj);
        }

        public override int GetHashCode() {
            return (content.crc != null ? content.crc.GetHashCode() : 0);
        }
    }
}