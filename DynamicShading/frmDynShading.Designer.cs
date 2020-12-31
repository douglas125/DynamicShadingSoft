namespace DynamicShading
{
    partial class frmDynShading
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmDynShading));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openwithoutColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importOutlineAndStrokesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveRenderAndStrokesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.renderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stepbystepRenderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.beginRenderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.revertToOriginalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.autorenderWhenChangingColorsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblLicData = new System.Windows.Forms.Label();
            this.picShadeImg = new System.Windows.Forms.PictureBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.ctxStroke = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setToPaletteColorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.redefineColorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.changeRegionColorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picShadeImg)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.ctxStroke.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.renderToolStripMenuItem});
            this.menuStrip1.Name = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            resources.ApplyResources(this.fileToolStripMenuItem, "fileToolStripMenuItem");
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.openwithoutColorsToolStripMenuItem,
            this.importOutlineAndStrokesToolStripMenuItem,
            this.saveRenderAndStrokesToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            // 
            // openToolStripMenuItem
            // 
            resources.ApplyResources(this.openToolStripMenuItem, "openToolStripMenuItem");
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // openwithoutColorsToolStripMenuItem
            // 
            resources.ApplyResources(this.openwithoutColorsToolStripMenuItem, "openwithoutColorsToolStripMenuItem");
            this.openwithoutColorsToolStripMenuItem.Name = "openwithoutColorsToolStripMenuItem";
            this.openwithoutColorsToolStripMenuItem.Click += new System.EventHandler(this.openwithoutColorsToolStripMenuItem_Click);
            // 
            // importOutlineAndStrokesToolStripMenuItem
            // 
            resources.ApplyResources(this.importOutlineAndStrokesToolStripMenuItem, "importOutlineAndStrokesToolStripMenuItem");
            this.importOutlineAndStrokesToolStripMenuItem.Name = "importOutlineAndStrokesToolStripMenuItem";
            this.importOutlineAndStrokesToolStripMenuItem.Click += new System.EventHandler(this.importOutlineAndStrokesToolStripMenuItem_Click);
            // 
            // saveRenderAndStrokesToolStripMenuItem
            // 
            resources.ApplyResources(this.saveRenderAndStrokesToolStripMenuItem, "saveRenderAndStrokesToolStripMenuItem");
            this.saveRenderAndStrokesToolStripMenuItem.Name = "saveRenderAndStrokesToolStripMenuItem";
            this.saveRenderAndStrokesToolStripMenuItem.Click += new System.EventHandler(this.saveRenderAndStrokesToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            resources.ApplyResources(this.editToolStripMenuItem, "editToolStripMenuItem");
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            // 
            // undoToolStripMenuItem
            // 
            resources.ApplyResources(this.undoToolStripMenuItem, "undoToolStripMenuItem");
            this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
            // 
            // renderToolStripMenuItem
            // 
            resources.ApplyResources(this.renderToolStripMenuItem, "renderToolStripMenuItem");
            this.renderToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stepbystepRenderToolStripMenuItem,
            this.beginRenderToolStripMenuItem,
            this.revertToOriginalToolStripMenuItem,
            this.toolStripMenuItem2,
            this.autorenderWhenChangingColorsToolStripMenuItem});
            this.renderToolStripMenuItem.Name = "renderToolStripMenuItem";
            // 
            // stepbystepRenderToolStripMenuItem
            // 
            resources.ApplyResources(this.stepbystepRenderToolStripMenuItem, "stepbystepRenderToolStripMenuItem");
            this.stepbystepRenderToolStripMenuItem.Name = "stepbystepRenderToolStripMenuItem";
            this.stepbystepRenderToolStripMenuItem.Click += new System.EventHandler(this.stepbystepRenderToolStripMenuItem_Click);
            // 
            // beginRenderToolStripMenuItem
            // 
            resources.ApplyResources(this.beginRenderToolStripMenuItem, "beginRenderToolStripMenuItem");
            this.beginRenderToolStripMenuItem.Name = "beginRenderToolStripMenuItem";
            this.beginRenderToolStripMenuItem.Click += new System.EventHandler(this.beginRenderToolStripMenuItem_Click);
            // 
            // revertToOriginalToolStripMenuItem
            // 
            resources.ApplyResources(this.revertToOriginalToolStripMenuItem, "revertToOriginalToolStripMenuItem");
            this.revertToOriginalToolStripMenuItem.Name = "revertToOriginalToolStripMenuItem";
            this.revertToOriginalToolStripMenuItem.Click += new System.EventHandler(this.revertToOriginalToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            resources.ApplyResources(this.toolStripMenuItem2, "toolStripMenuItem2");
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            // 
            // autorenderWhenChangingColorsToolStripMenuItem
            // 
            resources.ApplyResources(this.autorenderWhenChangingColorsToolStripMenuItem, "autorenderWhenChangingColorsToolStripMenuItem");
            this.autorenderWhenChangingColorsToolStripMenuItem.Checked = true;
            this.autorenderWhenChangingColorsToolStripMenuItem.CheckOnClick = true;
            this.autorenderWhenChangingColorsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autorenderWhenChangingColorsToolStripMenuItem.Name = "autorenderWhenChangingColorsToolStripMenuItem";
            // 
            // panel1
            // 
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.lblLicData);
            this.panel1.Controls.Add(this.picShadeImg);
            this.panel1.Name = "panel1";
            // 
            // lblLicData
            // 
            resources.ApplyResources(this.lblLicData, "lblLicData");
            this.lblLicData.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblLicData.Name = "lblLicData";
            // 
            // picShadeImg
            // 
            resources.ApplyResources(this.picShadeImg, "picShadeImg");
            this.picShadeImg.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picShadeImg.Cursor = System.Windows.Forms.Cursors.Cross;
            this.picShadeImg.Name = "picShadeImg";
            this.picShadeImg.TabStop = false;
            this.picShadeImg.Paint += new System.Windows.Forms.PaintEventHandler(this.picShadeImg_Paint);
            this.picShadeImg.MouseDown += new System.Windows.Forms.MouseEventHandler(this.picShadeImg_MouseDown);
            this.picShadeImg.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picShadeImg_MouseMove);
            this.picShadeImg.MouseUp += new System.Windows.Forms.MouseEventHandler(this.picShadeImg_MouseUp);
            // 
            // statusStrip1
            // 
            resources.ApplyResources(this.statusStrip1, "statusStrip1");
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus});
            this.statusStrip1.Name = "statusStrip1";
            // 
            // lblStatus
            // 
            resources.ApplyResources(this.lblStatus, "lblStatus");
            this.lblStatus.Name = "lblStatus";
            // 
            // ctxStroke
            // 
            resources.ApplyResources(this.ctxStroke, "ctxStroke");
            this.ctxStroke.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteToolStripMenuItem,
            this.setToPaletteColorToolStripMenuItem,
            this.redefineColorToolStripMenuItem,
            this.toolStripMenuItem1,
            this.changeRegionColorToolStripMenuItem});
            this.ctxStroke.Name = "ctxStroke";
            // 
            // deleteToolStripMenuItem
            // 
            resources.ApplyResources(this.deleteToolStripMenuItem, "deleteToolStripMenuItem");
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // setToPaletteColorToolStripMenuItem
            // 
            resources.ApplyResources(this.setToPaletteColorToolStripMenuItem, "setToPaletteColorToolStripMenuItem");
            this.setToPaletteColorToolStripMenuItem.Name = "setToPaletteColorToolStripMenuItem";
            this.setToPaletteColorToolStripMenuItem.Click += new System.EventHandler(this.setToPaletteColorToolStripMenuItem_Click);
            // 
            // redefineColorToolStripMenuItem
            // 
            resources.ApplyResources(this.redefineColorToolStripMenuItem, "redefineColorToolStripMenuItem");
            this.redefineColorToolStripMenuItem.Name = "redefineColorToolStripMenuItem";
            this.redefineColorToolStripMenuItem.Click += new System.EventHandler(this.redefineColorToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            // 
            // changeRegionColorToolStripMenuItem
            // 
            resources.ApplyResources(this.changeRegionColorToolStripMenuItem, "changeRegionColorToolStripMenuItem");
            this.changeRegionColorToolStripMenuItem.Name = "changeRegionColorToolStripMenuItem";
            this.changeRegionColorToolStripMenuItem.Click += new System.EventHandler(this.changeRegionColorToolStripMenuItem_Click);
            // 
            // frmDynShading
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "frmDynShading";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.frmDynShading_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picShadeImg)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ctxStroke.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.PictureBox picShadeImg;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripMenuItem renderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem beginRenderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveRenderAndStrokesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem revertToOriginalToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip ctxStroke;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem redefineColorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setToPaletteColorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem stepbystepRenderToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem changeRegionColorToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem autorenderWhenChangingColorsToolStripMenuItem;
        private System.Windows.Forms.Label lblLicData;
        private System.Windows.Forms.ToolStripMenuItem importOutlineAndStrokesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openwithoutColorsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
    }
}