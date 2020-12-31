namespace DynamicShading
{
    partial class frmPalette
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmPalette));
            this.picCurColor = new System.Windows.Forms.PictureBox();
            this.picPalette = new System.Windows.Forms.PictureBox();
            this.btnOpenImg = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.picCurColor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picPalette)).BeginInit();
            this.SuspendLayout();
            // 
            // picCurColor
            // 
            this.picCurColor.BackColor = System.Drawing.Color.RoyalBlue;
            this.picCurColor.Location = new System.Drawing.Point(0, 0);
            this.picCurColor.Name = "picCurColor";
            this.picCurColor.Size = new System.Drawing.Size(40, 40);
            this.picCurColor.TabIndex = 1;
            this.picCurColor.TabStop = false;
            this.picCurColor.Click += new System.EventHandler(this.picCurColor_Click);
            // 
            // picPalette
            // 
            this.picPalette.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.picPalette.Image = global::DynamicShading.Properties.Resources.RGB_wheel1;
            this.picPalette.Location = new System.Drawing.Point(0, 40);
            this.picPalette.Name = "picPalette";
            this.picPalette.Size = new System.Drawing.Size(147, 133);
            this.picPalette.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.picPalette.TabIndex = 0;
            this.picPalette.TabStop = false;
            this.picPalette.MouseDown += new System.Windows.Forms.MouseEventHandler(this.picPalette_MouseDown);
            this.picPalette.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picPalette_MouseMove);
            this.picPalette.MouseUp += new System.Windows.Forms.MouseEventHandler(this.picPalette_MouseUp);
            // 
            // btnOpenImg
            // 
            this.btnOpenImg.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenImg.Image = ((System.Drawing.Image)(resources.GetObject("btnOpenImg.Image")));
            this.btnOpenImg.Location = new System.Drawing.Point(105, -1);
            this.btnOpenImg.Name = "btnOpenImg";
            this.btnOpenImg.Size = new System.Drawing.Size(42, 42);
            this.btnOpenImg.TabIndex = 2;
            this.btnOpenImg.UseVisualStyleBackColor = true;
            this.btnOpenImg.Click += new System.EventHandler(this.btnOpenImg_Click);
            // 
            // frmPalette
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(146, 174);
            this.ControlBox = false;
            this.Controls.Add(this.btnOpenImg);
            this.Controls.Add(this.picCurColor);
            this.Controls.Add(this.picPalette);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmPalette";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Palette";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.frmPalette_Load);
            ((System.ComponentModel.ISupportInitialize)(this.picCurColor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picPalette)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox picPalette;
        private System.Windows.Forms.Button btnOpenImg;
        public System.Windows.Forms.PictureBox picCurColor;
    }
}