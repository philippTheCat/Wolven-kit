using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using WolvenKit.CR2W;
using WolvenKit.CR2W.Types;
using WolvenKit.FlowTreeEditors;

namespace WolvenKit
{
    public partial class frmQuestChunkFlowDiagram : DockContent
    {
        private readonly int connectionPointSize = 7;

      
        private readonly HashSet<EditorNode> selectedEditors;

        /// <summary>
        ///     Selection
        /// </summary>
        private readonly Brush selectionBackground;

        private readonly Pen selectionBorder;
        private readonly Pen selectionItemHighlight;
        private readonly Brush selectionItemHighlightBrush;

        public EditorNode EditorUnderCursor;
        private CR2WFile file;
        private bool isConnecting;
        private bool isSelecting;
        private bool isMoving;
        private Point prevMousePos;
        private int maxdepth;
        private Point selectionEnd;
        private Point selectionStart;
        public static float zoom = 1;
        private WolvenGraph graph;
        private ReingoldTilford reingoldTilford;

        public frmQuestChunkFlowDiagram()
        {
            InitializeComponent();

            selectionBackground = new SolidBrush(Color.FromArgb(100, SystemColors.Highlight));
            selectionBorder = new Pen(Color.FromArgb(200, SystemColors.Highlight));
            selectionItemHighlight = new Pen(Color.Green, 2.0f);
            selectionItemHighlightBrush = new SolidBrush(Color.Green);
            selectedEditors = new HashSet<EditorNode>();
        }

        public CR2WFile File
        {
            get { return file; }
            set
            {
                file = value;
                createNodeEditors();
            }
        }

        public event EventHandler<SelectChunkArgs> OnSelectChunkEvent;

        public void createNodeEditors()
        {
            if (File == null)
                return;
            

            graph = new WolvenGraph(File);
            graph.Refresh();

            reingoldTilford = new ReingoldTilford();
            reingoldTilford.CalculateLayout(graph);



            foreach (var vertex in reingoldTilford.vertices)
            {
                createEditor(0, vertex);
            }
        }

        private void createEditor(int depth, Vertex graphVertex)
        {
            EditorNode nodeEditorNode = graphVertex.node.editorNode;
            nodeEditorNode.OnManualMove += OnMove;
            nodeEditorNode.OnSelectChunk += OnSelectChunk;
            nodeEditorNode.LocationChanged += OnLocationChanged;
            Controls.Add(nodeEditorNode);
            nodeEditorNode.Location = new Point( (int) (graphVertex.position.X*400 * zoom), (int) (graphVertex.position.Y*100*zoom));
        }

        private void OnLocationChanged(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void OnMove(object sender, MoveEditorArgs e)
        {
            if (selectedEditors.Contains(sender))
            {
                foreach (var c in selectedEditors.Where(c => c != sender))
                {
                    c.Location = new Point(c.Location.X + e.Relative.X, c.Location.Y + e.Relative.Y);
                }
            }
            Invalidate();
            Refresh();
        }

        private void OnSelectChunk(object sender, SelectChunkArgs e)
        {
            OnSelectChunkEvent?.Invoke(sender, e);
        }


        private void frmChunkFlowView_Paint(object sender, PaintEventArgs e)
        {
            foreach (var c in graph.connections) {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                bool editorSelected = false; // selectedEditors.Contains(c.);

                var brush = editorSelected ? selectionItemHighlightBrush : Brushes.Black;
                var pen = editorSelected ? selectionItemHighlight : Pens.Black;
                Font drawFont = new Font("Arial", 12 * zoom);

           
                EditorNode fromEditor = reingoldTilford.m_NodeVertexLookup[c.@from].node.editorNode;
                EditorNode toEditor = reingoldTilford.m_NodeVertexLookup[c.@to].node.editorNode;
                
                fromEditor.addSocket(c.fromSocket, false);
                toEditor.addSocket(c.toSocket, true);
                
                var fromOffset = fromEditor.GetSocketConnectionLocation(c.fromSocket);
                var toOffset = toEditor.GetSocketConnectionLocation(c.toSocket);
                e.Graphics.FillRectangle(brush, fromEditor.Location.X + fromOffset.X,
                                         fromEditor.Location.Y + fromOffset.Y - connectionPointSize*zoom/2, connectionPointSize*zoom, connectionPointSize*zoom);

               
                e.Graphics.DrawString(c.fromSocket,drawFont, brush, new Rectangle(
                                                                                  (int) Math.Round(fromEditor.Location.X + fromOffset.X + connectionPointSize*zoom), 
                                                                                  fromEditor.Location.Y + fromOffset.Y ,
                                                                                  100,(int) (16 * zoom)));
                
                StringFormat format = new StringFormat();
                format.LineAlignment = StringAlignment.Near;
                format.Alignment = StringAlignment.Far;
                e.Graphics.DrawString(c.toSocket, drawFont, brush, new Rectangle( toEditor.Location.X + toOffset.X - 100, 
                                                                                  toEditor.Location.Y + toOffset.Y,
                                                                                  100,(int) (16 * zoom)), format);

                
                DrawConnectionBezier(e.Graphics, pen,
                                     (int) Math.Round(fromEditor.Location.X + fromOffset.X + connectionPointSize*zoom), 
                                     fromEditor.Location.Y + fromOffset.Y,
                                     
                                     toEditor.Location.X + toOffset.X, 
                                     toEditor.Location.Y + toOffset.Y
                    );
            }

            if (isSelecting)
            {
                var x = selectionStart.X < selectionEnd.X ? selectionStart.X : selectionEnd.X;
                var y = selectionStart.Y < selectionEnd.Y ? selectionStart.Y : selectionEnd.Y;
                var w = Math.Abs(selectionStart.X - selectionEnd.X);
                var h = Math.Abs(selectionStart.Y - selectionEnd.Y);

                var rect = new Rectangle(x, y, w, h);

                e.Graphics.FillRectangle(selectionBackground, rect);
                e.Graphics.DrawRectangle(selectionBorder, rect);
            }
        }

        private static void DrawConnectionBezier(Graphics g, Pen c, int x1, int y1, int x2, int y2)
        {
            var yoffset = 0;
            var xoffset = Math.Max(Math.Min(Math.Abs(x1 - x2)/2, 200), 50);
            g.DrawBezier(c,
                x1, y1,
                x1 + xoffset, y1 + yoffset,
                x2 - xoffset, y2 - yoffset,
                x2, y2);
        }

        private void frmChunkFlowDiagram_Scroll(object sender, MouseEventArgs e)
        {
            float prevZoom = zoom;
            if(e.Delta > 0f)
                zoom += .07f;
            else
                zoom -= .07f;
            if (zoom < .23f)
                zoom = .23f;
            foreach (EditorNode c in Controls)
            {
                c.Zoom(zoom, prevZoom);
            }
            Invalidate();
        }

        private void frmChunkFlowDiagram_KeyDown(object sender, KeyEventArgs e)
        {
            Invalidate();
        }

        private void frmChunkFlowDiagram_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (Form.ModifierKeys != Keys.Control)
                {
                    selectionStart = e.Location;
                    isSelecting = true;
                }
                else
                {
                    prevMousePos = e.Location;
                    isMoving = true;
                }
            }
        }

        private void frmChunkFlowDiagram_MouseMove(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                selectionEnd = e.Location;

                SelectChunks();

                Invalidate();
            }

            if (isConnecting)
            {
                selectionEnd = e.Location;

                CheckConnectTarget();

                Invalidate();
            }

            if (isMoving)
            {
                foreach(Control c in Controls)
                {
                    c.Left -= prevMousePos.X - e.Location.X;
                    c.Top -= prevMousePos.Y - e.Location.Y;
                }

                Invalidate();
            }
            
            Refresh();
            prevMousePos = e.Location;
        }

        private void CheckConnectTarget()
        {
           /* connectingTarget = null;

            foreach (var c in editorNodes.Values)
            {
                var r = new Rectangle(c.Location, c.Size);
                if (r.Contains(selectionEnd.X, selectionEnd.Y))
                {
                    connectingTarget = c;
                    break;
                }
            }*/
        }

        private void frmChunkFlowDiagram_MouseUp(object sender, MouseEventArgs e)
        {
            if (isSelecting)
            {
                selectionEnd = e.Location;
                isSelecting = false;
                SelectChunks();
                Invalidate();
            }

            if (isConnecting)
            {
                selectionEnd = e.Location;
                isConnecting = false;

                //DoConnect();

                Invalidate();
            }

            if (isMoving)
            {
                isMoving = false;

                Invalidate();
            }
        }

       

        private void SelectChunks()
        {
            selectedEditors.Clear();

            var x = selectionStart.X < selectionEnd.X ? selectionStart.X : selectionEnd.X;
            var y = selectionStart.Y < selectionEnd.Y ? selectionStart.Y : selectionEnd.Y;
            var w = Math.Abs(selectionStart.X - selectionEnd.X);
            var h = Math.Abs(selectionStart.Y - selectionEnd.Y);

            var rect = new Rectangle(x, y, w, h);

           /* foreach (var c in from c in editorNodes.Values let r = new Rectangle(c.Location, c.Size) where rect.IntersectsWith(r) select c)
            {
                selectedEditors.Add(c);
            }*/
        }

        protected override Point ScrollToControl(Control activeControl)
        {
            return AutoScrollPosition;
        }

        private void frmChunkFlowDiagram_Load(object sender, EventArgs e)
        {
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            EditorUnderCursor = null;
            /*foreach (var c in editorNodes.Values)
            {
                var r = new Rectangle(PointToScreen(c.Location), c.Size);
                if (r.Contains(contextMenuStrip1.Left, contextMenuStrip1.Top))
                {
                    EditorUnderCursor = c;
                    break;
                }
            }*/

            copyToolStripMenuItem.Enabled = selectedEditors.Count > 0 || EditorUnderCursor != null;
            copyDisplayTextToolStripMenuItem.Enabled = selectedEditors.Count > 0 || EditorUnderCursor != null;
            pasteToolStripMenuItem.Enabled = CopyController.ChunkList != null;
        }

        private void copyDisplayTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var text = new StringBuilder();

            if (selectedEditors.Count == 0)
            {
                var editor = EditorUnderCursor;
                if (editor != null)
                {
                    //text.AppendLine(editor.GetCopyText());
                }
            }
            else
            {
                foreach (var editor in selectedEditors)
                {
                    //text.AppendLine(editor.GetCopyText());
                }
            }

            if (text.Length > 0)
            {
                Clipboard.SetText(text.ToString());
            }
        }
    }
}