using System;
using System.Drawing;
using System.Windows.Forms;

namespace SnipChatGpt
{
    public partial class CropForm : Form
    {
        private PictureBox pictureBox;
        private Rectangle cropRect;
        private Point startPoint;
        private bool isDragging;
        public Bitmap CroppedImage { get; private set; }

        public CropForm(Bitmap image)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Normal;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = Screen.PrimaryScreen.Bounds; // Covers entire screen, including taskbar
            this.TopMost = true;

            this.KeyDown += CropForm_KeyDown;
            this.Text = "Crop Screenshot";
            pictureBox = new PictureBox { Image = image, Dock = DockStyle.Fill, Cursor = Cursors.Cross };
            pictureBox.MouseDown += OnMouseDown;
            pictureBox.MouseMove += OnMouseMove;
            pictureBox.MouseUp += OnMouseUp;
            pictureBox.Paint += PictureBox_Paint;

            Controls.Add(pictureBox);
            Width = image.Width;
            Height = image.Height;
        }
        private void CropForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close(); // Cancel cropping and close form
            }
        }
        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            isDragging = true;
            startPoint = e.Location;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                cropRect = new Rectangle(
                    Math.Min(startPoint.X, e.X),
                    Math.Min(startPoint.Y, e.Y),
                    Math.Abs(startPoint.X - e.X),
                    Math.Abs(startPoint.Y - e.Y)
                );
                pictureBox.Invalidate();
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            if (cropRect.Width > 0 && cropRect.Height > 0)
            {
                var bmp = new Bitmap(cropRect.Width, cropRect.Height);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(pictureBox.Image, new Rectangle(0, 0, bmp.Width, bmp.Height), cropRect, GraphicsUnit.Pixel);
                }
                CroppedImage = bmp;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
        private void PictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (isDragging && cropRect != Rectangle.Empty)
            {
                using (Pen pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, cropRect);
                }
            }
        }
    }
}