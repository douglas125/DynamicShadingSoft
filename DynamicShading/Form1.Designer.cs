namespace DynamicShading
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.panel1 = new System.Windows.Forms.Panel();
            this.pbWaterMark = new System.Windows.Forms.PictureBox();
            this.picThumb = new System.Windows.Forms.PictureBox();
            this.numThresh = new System.Windows.Forms.NumericUpDown();
            this.picOutline = new System.Windows.Forms.PictureBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnRender = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.flowStrokes = new System.Windows.Forms.FlowLayoutPanel();
            this.picStrokeColor = new System.Windows.Forms.PictureBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.lblProg = new System.Windows.Forms.Label();
            this.picProgColor = new System.Windows.Forms.PictureBox();
            this.progRender = new System.Windows.Forms.ProgressBar();
            this.picShaded = new System.Windows.Forms.PictureBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveImageWithStrokesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbWaterMark)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picThumb)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThresh)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picOutline)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picStrokeColor)).BeginInit();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picProgColor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picShaded)).BeginInit();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(8, 23);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(472, 272);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.panel1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(2);
            this.tabPage1.Size = new System.Drawing.Size(464, 246);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Outline";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.AutoScroll = true;
            this.panel1.Controls.Add(this.pbWaterMark);
            this.panel1.Controls.Add(this.picThumb);
            this.panel1.Controls.Add(this.numThresh);
            this.panel1.Controls.Add(this.picOutline);
            this.panel1.Location = new System.Drawing.Point(4, 4);
            this.panel1.Margin = new System.Windows.Forms.Padding(2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(459, 242);
            this.panel1.TabIndex = 0;
            // 
            // pbWaterMark
            // 
            this.pbWaterMark.Image = ((System.Drawing.Image)(resources.GetObject("pbWaterMark.Image")));
            this.pbWaterMark.Location = new System.Drawing.Point(298, 79);
            this.pbWaterMark.Margin = new System.Windows.Forms.Padding(2);
            this.pbWaterMark.Name = "pbWaterMark";
            this.pbWaterMark.Size = new System.Drawing.Size(133, 87);
            this.pbWaterMark.TabIndex = 3;
            this.pbWaterMark.TabStop = false;
            this.pbWaterMark.Visible = false;
            // 
            // picThumb
            // 
            this.picThumb.Location = new System.Drawing.Point(98, 2);
            this.picThumb.Margin = new System.Windows.Forms.Padding(2);
            this.picThumb.Name = "picThumb";
            this.picThumb.Size = new System.Drawing.Size(155, 99);
            this.picThumb.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picThumb.TabIndex = 2;
            this.picThumb.TabStop = false;
            // 
            // numThresh
            // 
            this.numThresh.Location = new System.Drawing.Point(2, 2);
            this.numThresh.Margin = new System.Windows.Forms.Padding(2);
            this.numThresh.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.numThresh.Name = "numThresh";
            this.numThresh.Size = new System.Drawing.Size(80, 20);
            this.numThresh.TabIndex = 1;
            this.numThresh.Value = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.numThresh.ValueChanged += new System.EventHandler(this.numThresh_ValueChanged);
            // 
            // picOutline
            // 
            this.picOutline.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picOutline.Location = new System.Drawing.Point(2, 2);
            this.picOutline.Margin = new System.Windows.Forms.Padding(2);
            this.picOutline.Name = "picOutline";
            this.picOutline.Size = new System.Drawing.Size(349, 227);
            this.picOutline.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picOutline.TabIndex = 0;
            this.picOutline.TabStop = false;
            this.picOutline.Click += new System.EventHandler(this.picOutline_Click);
            this.picOutline.MouseDown += new System.Windows.Forms.MouseEventHandler(this.picOutline_MouseDown);
            this.picOutline.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picOutline_MouseMove);
            this.picOutline.MouseUp += new System.Windows.Forms.MouseEventHandler(this.picOutline_MouseUp);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Controls.Add(this.panel2);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(2);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(2);
            this.tabPage2.Size = new System.Drawing.Size(464, 246);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Shading";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.groupBox1.Controls.Add(this.btnRender);
            this.groupBox1.Controls.Add(this.btnRefresh);
            this.groupBox1.Controls.Add(this.flowStrokes);
            this.groupBox1.Controls.Add(this.picStrokeColor);
            this.groupBox1.Location = new System.Drawing.Point(4, 6);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(2);
            this.groupBox1.Size = new System.Drawing.Size(75, 239);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Strokes";
            // 
            // btnRender
            // 
            this.btnRender.Location = new System.Drawing.Point(4, 40);
            this.btnRender.Margin = new System.Windows.Forms.Padding(2);
            this.btnRender.Name = "btnRender";
            this.btnRender.Size = new System.Drawing.Size(63, 39);
            this.btnRender.TabIndex = 2;
            this.btnRender.Text = "Render && save";
            this.btnRender.UseVisualStyleBackColor = true;
            this.btnRender.Click += new System.EventHandler(this.btnRender_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(4, 14);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(2);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(63, 23);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // flowStrokes
            // 
            this.flowStrokes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.flowStrokes.AutoScroll = true;
            this.flowStrokes.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flowStrokes.Location = new System.Drawing.Point(4, 131);
            this.flowStrokes.Margin = new System.Windows.Forms.Padding(2);
            this.flowStrokes.Name = "flowStrokes";
            this.flowStrokes.Size = new System.Drawing.Size(67, 105);
            this.flowStrokes.TabIndex = 1;
            // 
            // picStrokeColor
            // 
            this.picStrokeColor.BackColor = System.Drawing.Color.Blue;
            this.picStrokeColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picStrokeColor.Location = new System.Drawing.Point(4, 83);
            this.picStrokeColor.Margin = new System.Windows.Forms.Padding(2);
            this.picStrokeColor.Name = "picStrokeColor";
            this.picStrokeColor.Size = new System.Drawing.Size(42, 43);
            this.picStrokeColor.TabIndex = 0;
            this.picStrokeColor.TabStop = false;
            this.picStrokeColor.Click += new System.EventHandler(this.picStrokeColor_Click);
            // 
            // panel2
            // 
            this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel2.AutoScroll = true;
            this.panel2.Controls.Add(this.lblProg);
            this.panel2.Controls.Add(this.picProgColor);
            this.panel2.Controls.Add(this.progRender);
            this.panel2.Controls.Add(this.picShaded);
            this.panel2.Location = new System.Drawing.Point(83, 4);
            this.panel2.Margin = new System.Windows.Forms.Padding(2);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(380, 242);
            this.panel2.TabIndex = 0;
            // 
            // lblProg
            // 
            this.lblProg.AutoSize = true;
            this.lblProg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblProg.Location = new System.Drawing.Point(22, 21);
            this.lblProg.Name = "lblProg";
            this.lblProg.Size = new System.Drawing.Size(23, 15);
            this.lblProg.TabIndex = 3;
            this.lblProg.Text = "0%";
            this.lblProg.Visible = false;
            // 
            // picProgColor
            // 
            this.picProgColor.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picProgColor.Location = new System.Drawing.Point(63, 16);
            this.picProgColor.Name = "picProgColor";
            this.picProgColor.Size = new System.Drawing.Size(24, 23);
            this.picProgColor.TabIndex = 2;
            this.picProgColor.TabStop = false;
            this.picProgColor.Visible = false;
            // 
            // progRender
            // 
            this.progRender.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progRender.Location = new System.Drawing.Point(93, 16);
            this.progRender.Name = "progRender";
            this.progRender.Size = new System.Drawing.Size(576, 23);
            this.progRender.TabIndex = 1;
            this.progRender.Visible = false;
            // 
            // picShaded
            // 
            this.picShaded.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picShaded.Location = new System.Drawing.Point(2, 2);
            this.picShaded.Margin = new System.Windows.Forms.Padding(2);
            this.picShaded.Name = "picShaded";
            this.picShaded.Size = new System.Drawing.Size(546, 367);
            this.picShaded.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.picShaded.TabIndex = 0;
            this.picShaded.TabStop = false;
            this.picShaded.Paint += new System.Windows.Forms.PaintEventHandler(this.picShaded_Paint);
            this.picShaded.MouseDown += new System.Windows.Forms.MouseEventHandler(this.picShaded_MouseDown);
            this.picShaded.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picShaded_MouseMove);
            this.picShaded.MouseUp += new System.Windows.Forms.MouseEventHandler(this.picShaded_MouseUp);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(4, 1, 0, 1);
            this.menuStrip1.Size = new System.Drawing.Size(485, 31);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveImageWithStrokesToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(50, 29);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // importToolStripMenuItem
            // 
            this.importToolStripMenuItem.Name = "importToolStripMenuItem";
            this.importToolStripMenuItem.Size = new System.Drawing.Size(275, 30);
            this.importToolStripMenuItem.Text = "&Import...";
            this.importToolStripMenuItem.Click += new System.EventHandler(this.importToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(275, 30);
            this.saveToolStripMenuItem.Text = "&Save...";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // saveImageWithStrokesToolStripMenuItem
            // 
            this.saveImageWithStrokesToolStripMenuItem.Name = "saveImageWithStrokesToolStripMenuItem";
            this.saveImageWithStrokesToolStripMenuItem.Size = new System.Drawing.Size(275, 30);
            this.saveImageWithStrokesToolStripMenuItem.Text = "Save image with strokes";
            this.saveImageWithStrokesToolStripMenuItem.Click += new System.EventHandler(this.saveImageWithStrokesToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(485, 298);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "Dynamic Shader";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbWaterMark)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picThumb)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numThresh)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picOutline)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picStrokeColor)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picProgColor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picShaded)).EndInit();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox picOutline;
        private System.Windows.Forms.NumericUpDown numThresh;
        private System.Windows.Forms.PictureBox picThumb;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.PictureBox picShaded;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PictureBox picStrokeColor;
        private System.Windows.Forms.FlowLayoutPanel flowStrokes;
        private System.Windows.Forms.Button btnRender;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.PictureBox pbWaterMark;
        private System.Windows.Forms.PictureBox picProgColor;
        private System.Windows.Forms.ProgressBar progRender;
        private System.Windows.Forms.Label lblProg;
        private System.Windows.Forms.ToolStripMenuItem saveImageWithStrokesToolStripMenuItem;
    }
}

