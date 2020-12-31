using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DynamicShading
{
    public partial class frmPalette : Form
    {
        public frmPalette()
        {
            InitializeComponent();
        }

        private void btnOpenImg_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Images|*.jpg;*.bmp;*.png;*.jpeg";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Bitmap bmpSrc = new Bitmap(ofd.FileName);

                picPalette.Image = bmpSrc;
            }
        }

        private void frmPalette_Load(object sender, EventArgs e)
        {
        }

        private void picCurColor_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            cd.Color = picCurColor.BackColor;
            cd.FullOpen = true;
            if (cd.ShowDialog() == DialogResult.OK)
            {
                picCurColor.BackColor = cd.Color;
            }
        }

        #region Color picking from image
        bool clicked = false;
        private void picPalette_MouseDown(object sender, MouseEventArgs e)
        {
            clicked = true;
            picCurColor.BackColor = getImgColor(e.X, e.Y);
        }

        private void picPalette_MouseUp(object sender, MouseEventArgs e)
        {
            clicked = false;
        }

        private void picPalette_MouseMove(object sender, MouseEventArgs e)
        {
            if (clicked) picCurColor.BackColor = getImgColor(e.X, e.Y);
        }

        private Color getImgColor(int x, int y)
        {
            if (x < 0 || y < 0 || x >= picPalette.Width || y >= picPalette.Height) return picCurColor.BackColor;

            Bitmap bmp = (Bitmap)picPalette.Image;
            int xImg = bmp.Width * x / picPalette.Width;
            int yImg = bmp.Height * y / picPalette.Height;

            return bmp.GetPixel(xImg, yImg);
        }
        #endregion

    }
}
