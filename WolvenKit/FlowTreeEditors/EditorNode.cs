using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WolvenKit.CR2W;

namespace WolvenKit.FlowTreeEditors {


    public struct Connection {
        public GraphNode from;
        public GraphNode to;
        public string toSocket;
        public string fromSocket;
    }


    public class EditorNode: UserControl {
        public readonly CR2WChunk chunk;

        private IContainer components = null;

        protected Label titleLabel;
        
        public EditorNode(CR2WChunk chunk) {
            this.chunk = chunk;
            this.zoomFactor = 1f;
            
            InitializeComponent();
            setTitle(chunk.Name);
            UpdateView();
        }
        
        public virtual void UpdateView() { 
            int inputsize = inputSockets.Count;
            int outputsize = outputSockets.Count;


            
            int additionalLines = 0;

            if (inputsize == outputsize && outputsize == 0) {
                // 1 empty line
                additionalLines += 1;
            }
            
            Height = (int) ((Math.Max(inputsize, outputsize)+additionalLines) * lineHeight * zoomFactor) + titleLabel.Height;
            titleLabel.Height = (int) Math.Round(lineHeight + titleLabel.PreferredHeight * zoomFactor);
            titleLabel.Font = new Font("Arial", lineHeight * zoomFactor,FontStyle.Bold);
        }

        public virtual void setTitle(string title) {
            titleLabel.Text = title;
        }
        
        public virtual void Zoom(float zoom, float prevZoom) {
            zoomFactor = zoom;

            Size = new Size((int) Math.Round(Size.Width *  zoom / prevZoom), (int) Math.Round(Size.Height *  zoom / prevZoom));

            Left = (int) Math.Round(Left * zoom / prevZoom);
            Top = (int) Math.Round(Top * zoom / prevZoom);

            UpdateView();
        }

        
        
        #region GUI

        private int lineHeight = 18;
        private int fontHeight = 18 - 4;
        public float zoomFactor;


        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }


        protected virtual void InitializeComponent()
        {
            titleLabel = new Label();
            SuspendLayout();
           
            // title
            titleLabel.Anchor = (AnchorStyles.Top | AnchorStyles.Left) 
                            | AnchorStyles.Right;
            titleLabel.AutoEllipsis = true;
            titleLabel.BackColor = SystemColors.ActiveCaption;
            
            titleLabel.Font = new Font("Arial", fontHeight);
            titleLabel.ForeColor = SystemColors.ActiveCaptionText;
            titleLabel.Location = new Point(0, 0);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new Size(300, titleLabel.PreferredSize.Height);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "Title";
            titleLabel.TextAlign = ContentAlignment.TopLeft;
            titleLabel.Click += ClickHandler;
            titleLabel.MouseDown += MouseDownHandler;
            titleLabel.MouseMove += MouseMoveHandler;
            titleLabel.MouseUp += MouseUpHandler;
            
            
            
            // control setup
            AutoScaleDimensions = new SizeF(6F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(titleLabel);
            DoubleBuffered = true;
            Name = "EditorNode";
            Size = new Size(300, titleLabel.PreferredHeight + lineHeight);
            Click += ClickHandler;
            ResumeLayout(false);
            
        }
        
        #endregion

       
       
        #region Socket
        
        public List<string> outputSockets = new List<string>();
        public List<string> inputSockets = new List<string>();
    

        public virtual void addSocket(string name, bool isInput) {
            if (isInput && !inputSockets.Contains(name)) {
                
                inputSockets.Add(name);
            }
            else if (!isInput && !outputSockets.Contains(name)){
                outputSockets.Add(name);
            }

            UpdateView();
        }
        
        public Point GetSocketConnectionLocation(string socket) {

            int index;
            int x;
            
            if (inputSockets.Contains(socket)) {
                x = 0;
                index = inputSockets.IndexOf(socket);
            }
            else {
                x = Width;
                index = outputSockets.IndexOf(socket);
            }

            float halfLine = lineHeight * zoomFactor / 2 ;

            int y = (int) Math.Floor(titleLabel.Height + (lineHeight * zoomFactor * index + halfLine));
            
            return new Point(x,y);
        }
        

        #endregion

       

        #region events

        public event EventHandler<SelectChunkArgs> OnSelectChunk;
        public event EventHandler<MoveEditorArgs> OnManualMove;
        private Point mouseStart;
        private bool mouseMoving;


        public virtual void MouseUpHandler(object sender, MouseEventArgs e) {
            mouseMoving = false;
        }

        public virtual void MouseMoveHandler(object sender, MouseEventArgs e) {
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

        public virtual void MouseDownHandler(object sender, MouseEventArgs e) {
            mouseStart = e.Location;
            mouseMoving = true;
        }

        public virtual void ClickHandler(object sender, EventArgs e) {
            OnSelectChunk?.Invoke(this, new SelectChunkArgs {Chunk = chunk});

        }

        #endregion
    }
}