using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using WolvenKit.CR2W;
using WolvenKit.CR2W.Types;

namespace WolvenKit.FlowTreeEditors
{
    public partial class ChunkEditor : UserControl
    {
        private CR2WChunk chunk;
        private bool mouseMoving;
        private Point mouseStart;

        public ChunkEditor()
        {
            InitializeComponent();
        }

        public CR2WChunk Chunk
        {
            get { return chunk; }
            set
            {
                chunk = value;
                UpdateView();
            }
        }

        private Size originalSize;
        
        protected float zoomFactor = 1;

        public Size OriginalSize
        {
            get { return originalSize; }
            set { originalSize = value; }
        }

        public virtual string GetCopyText()
        {
            return chunk.Name;
        }

        public event EventHandler<SelectChunkArgs> OnSelectChunk;
        public event EventHandler<MoveEditorArgs> OnManualMove;

        protected void lblTitle_MouseDown(object sender, MouseEventArgs e)
        {
            mouseStart = e.Location;
            mouseMoving = true;
        }

        protected void lblTitle_MouseUp(object sender, MouseEventArgs e)
        {
            mouseMoving = false;
        }

        protected void lblTitle_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseMoving)
            {
                OnManualMove?.Invoke(this, new MoveEditorArgs
                {
                    Relative =
                        new Point((Location.X - mouseStart.X + e.X) - Location.X,
                            (Location.Y - mouseStart.Y + e.Y) - Location.Y)
                });
                Location = new Point(Location.X - mouseStart.X + e.X, Location.Y - mouseStart.Y + e.Y);
            }
        }

        public virtual void UpdateView()
        {
            lblTitle.Text = chunk.Name;
            UpdateHeight();
        }

        public virtual void UpdateHeight() {
            Height = (int) (lblTitle.Height * zoomFactor);
        }

        public virtual List<CPtr> GetConnections()
        {
            return null;
        }

        protected void lblTitle_Click(object sender, EventArgs e)
        {
            FireSelectEvent(Chunk);
        }

        protected void ChunkEditor_Click(object sender, EventArgs e)
        {
            FireSelectEvent(Chunk);
        }

        public void FireSelectEvent(CR2WChunk c)
        {
            OnSelectChunk?.Invoke(this, new SelectChunkArgs {Chunk = c});
        }

        public virtual Point GetConnectionLocation(int i)
        {
            return new Point(0, Height/2);
        }

        public virtual IEnumerable<Connection> getNewConnections() {
            return new List<Connection>();
        }

        public virtual Point GetNewConnectionLocation(string connection) {
            throw new NotImplementedException();
        }

        public virtual void addSocket(string name, bool isInput) {
            throw new NotImplementedException();
        }

       
    }
}