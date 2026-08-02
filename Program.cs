using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ScreenReader
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Form form = new Form();

            form.Text = "ScreenReader — захват экрана";
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Width = 1200;
            form.Height = 800;

            Label title = new Label();

            title.Text = "Тест захвата экрана";
            title.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = new Point(20, 20);

            Button captureButton = new Button();

            captureButton.Text = "Сделать снимок экрана";
            captureButton.Font = new Font("Segoe UI", 11);
            captureButton.Width = 240;
            captureButton.Height = 45;
            captureButton.Location = new Point(20, 65);

            PictureBox picture = new PictureBox();

            picture.Location = new Point(20, 125);
            picture.Width = 1140;
            picture.Height = 600;
            picture.BorderStyle = BorderStyle.FixedSingle;
            picture.SizeMode = PictureBoxSizeMode.Zoom;

            form.Controls.Add(title);
            form.Controls.Add(captureButton);
            form.Controls.Add(picture);

            captureButton.Click += (sender, e) =>
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;

                Bitmap screenshot = new Bitmap(
                    bounds.Width,
                    bounds.Height,
                    PixelFormat.Format32bppArgb);

                using (Graphics graphics = Graphics.FromImage(screenshot))
                {
                    graphics.CopyFromScreen(
                        bounds.Left,
                        bounds.Top,
                        0,
                        0,
                        bounds.Size);
                }

                picture.Image?.Dispose();
                picture.Image = screenshot;
            };

            Application.Run(form);
        }
    }
}
