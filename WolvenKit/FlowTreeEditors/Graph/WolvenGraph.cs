using System.Collections.Generic;
using System.Linq;
using WolvenKit.CR2W;
using WolvenKit.CR2W.Types;

namespace WolvenKit.FlowTreeEditors {
    public class WolvenGraph: Graph {
        private readonly CR2WFile file;
        public List<Connection> connections = new List<Connection>();

        public WolvenGraph(CR2WFile file) {
            this.file = file;
        }

        protected override IEnumerable<GraphNode> GetChildren(GraphNode node) {
            List<GraphNode> children = new List<GraphNode>();
            CR2WChunk chunk = (CR2WChunk) node.content;
            
            if (chunk != null) {
                var cachedConnections = chunk.GetVariableByName("cachedConnections");
                if (cachedConnections != null && cachedConnections is CArray) {
                    var cachedConnectionsArray = ((CArray) cachedConnections);
                    foreach (CVariable conn in cachedConnectionsArray) {

                        if (conn is CVector) {
                            CVector connection = (CVector) conn;
                            CName socketId = (CName) connection.GetVariableByName("socketId");
                            CArray blocks = (CArray) connection.GetVariableByName("blocks");

                            
                            if (blocks is CArray) {
                                foreach (CVariable block in blocks) {
                                    if (block is CVector) {
                                        CVector blockVector = (CVector) block;
                                        Connection connectionObject = new Connection();
                                        connectionObject.@from = new GraphNode(chunk);

                                        foreach (CVariable blockVectorVariable in blockVector.variables) {
                                            if (blockVectorVariable is CPtr) {
                                                CPtr pointingToNode = (CPtr) blockVectorVariable;

                                                GraphNode connectionObjectTo = new GraphNode(pointingToNode.PtrTarget);
                                                connectionObject.to = connectionObjectTo;
                                                children.Add(connectionObjectTo);
                                                continue;
                                            }

                                            if (blockVectorVariable is CName) {
                                                connectionObject.toSocket = ((CName) blockVectorVariable).Value;
                                            }
                                        }

                                        if (socketId != null) {
                                            connectionObject.fromSocket = socketId.Value;
                                        }
                                        connections.Add(connectionObject);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return children;
        }

        protected override void Populate() {
       
            var questGraph = file.chunks[0].GetVariableByName("graph");
            
            if (questGraph != null && questGraph is CPtr)
            {
                CR2WChunk controlParts = ((CPtr)questGraph).PtrTarget;

                var graphBlocks = controlParts.GetVariableByName("graphBlocks");

                if (graphBlocks != null && graphBlocks is CArray) {
                    var graphParts = (CArray) graphBlocks;

                    foreach (CPtr graphPart in graphParts) {
                        AddNodeHierarchy(new GraphNode(graphPart.PtrTarget));
                    }

                }
            }
            
            
        }
    }
}