using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Tesseract;

namespace ScreenReader
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Form form = new Form
            {
                Text = "ScreenReader — OCR тест",
                StartPosition = FormStartPosition.CenterScreen,
                Width = 1200,
                Height = 800
            };

            Label title = new Label
            {
                Text = "OCR — выбор строки",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            Button selectButton = new Button
            {
                Text = "Выбрать область экрана",
                Font = new Font("Segoe UI", 11),
                Width = 250,
                Height = 45,
                Location = new Point(20, 65)
            };

            Button ocrButton = new Button
            {
                Text = "Распознать выбранную область",
                Font = new Font("Segoe UI", 11),
                Width = 280,
                Height = 45,
                Location = new Point(285, 65),
                Enabled = false
            };

            PictureBox preview = new PictureBox
            {
                Location = new Point(20, 125),
                Width = 1140,
                Height = 300,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom
            };

            TextBox result = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 11),
                Location = new Point(20, 445),
                Width = 1140,
                Height = 270
            };

            form.Controls.Add(title);
            form.Controls.Add(selectButton);
            form.Controls.Add(ocrButton);
            form.Controls.Add(preview);
            form.Controls.Add(result);

            Bitmap? selectedImage = null;

            selectButton.Click += (sender, e) =>
            {
                try
                {
                    form.Hide();

                    Application.DoEvents();
                    System.Threading.Thread.Sleep(300);

                    Rectangle screenBounds =
                        Screen.PrimaryScreen.Bounds;

                    using Bitmap screenshot =
                        new Bitmap(
                            screenBounds.Width,
                            screenBounds.Height,
                            PixelFormat.Format32bppArgb);

                    using (Graphics graphics =
                        Graphics.FromImage(screenshot))
                    {
                        graphics.CopyFromScreen(
                            screenBounds.Left,
                            screenBounds.Top,
                            0,
                            0,
                            screenBounds.Size);
                    }

                    using SelectionForm selectionForm =
                        new SelectionForm(screenshot);

                    if (selectionForm.ShowDialog() == DialogResult.OK)
                    {
                        Rectangle selectedRectangle =
                            selectionForm.SelectedRectangle;

                        if (selectedRectangle.Width > 5 &&
                            selectedRectangle.Height > 5)
                        {
                            selectedImage?.Dispose();

                            selectedImage =
                                screenshot.Clone(
                                    selectedRectangle,
                                    PixelFormat.Format32bppArgb);

                            preview.Image = selectedImage;

                            ocrButton.Enabled = true;

                            result.Text =
                                "Область выбрана.\r\n\r\n" +
                                $"X: {selectedRectangle.X}\r\n" +
                                $"Y: {selectedRectangle.Y}\r\n" +
                                $"Ширина: {selectedRectangle.Width}\r\n" +
                                $"Высота: {selectedRectangle.Height}\r\n\r\n" +
                                "Нажми «Распознать выбранную область».";
                        }
                    }

                    form.Show();
                    form.Activate();
                }
                catch (Exception ex)
                {
                    form.Show();

                    result.Text =
                        "ОШИБКА:\r\n\r\n" +
                        ex.Message;
                }
            };

            ocrButton.Click += (sender, e) =>
            {
                if (selectedImage == null)
                {
                    return;
                }

                try
                {
                    result.Text = "Обрабатываю изображение...";

                    using Bitmap processedImage =
                        PrepareImage(selectedImage);

                    string tessdataPath =
                        Path.Combine(
                            AppContext.BaseDirectory,
                            "tessdata");

                    using TesseractEngine engine =
                        new TesseractEngine(
                            tessdataPath,
                            "rus",
                            EngineMode.Default);

                    engine.SetVariable(
                        "preserve_interword_spaces",
                        "1");

                    using MemoryStream stream =
                        new MemoryStream();

                    processedImage.Save(
                        stream,
                        System.Drawing.Imaging.ImageFormat.Png);

                    byte[] imageBytes =
                        stream.ToArray();

                    using Pix image =
                        Pix.LoadFromMemory(imageBytes);

                    using Page page =
                        engine.Process(
                            image,
                            PageSegMode.SingleLine);

                    string text =
                        page.GetText();

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        result.Text =
                            "OCR ничего не распознал.";
                    }
                    else
                    {
                        result.Text =
                            "РЕЗУЛЬТАТ OCR:\r\n\r\n" +
                            text.Trim();
                    }
                }
                catch (Exception ex)
                {
                    result.Text =
                        "ОШИБКА OCR:\r\n\r\n" +
                        ex.Message +
                        "\r\n\r\n" +
                        ex.StackTrace;
                }
            };

            form.FormClosed += (sender, e) =>
            {
                selectedImage?.Dispose();
            };

            Application.Run(form);
        }

        private static Bitmap PrepareImage(Bitmap source)
        {
            int scale = 3;

            Bitmap enlarged =
                new Bitmap(
                    source.Width * scale,
                    source.Height * scale,
                    PixelFormat.Format24bppRgb);

            using (Graphics graphics =
                Graphics.FromImage(enlarged))
            {
                graphics.InterpolationMode =
                    InterpolationMode.HighQualityBicubic;

                graphics.SmoothingMode =
                    SmoothingMode.HighQuality;

                graphics.PixelOffsetMode =
                    PixelOffsetMode.HighQuality;

                graphics.DrawImage(
                    source,
                    new Rectangle(
                        0,
                        0,
                        enlarged.Width,
                        enlarged.Height));
            }

            Bitmap gray =
                new Bitmap(
                    enlarged.Width,
                    enlarged.Height,
                    PixelFormat.Format24bppRgb);

            for (int y = 0; y < enlarged.Height; y++)
            {
                for (int x = 0; x < enlarged.Width; x++)
                {
                    Color pixel =
                        enlarged.GetPixel(x, y);

                    int value =
                        (int)(
                            pixel.R * 0.299 +
                            pixel.G * 0.587 +
                            pixel.B * 0.114);

                    Color grayColor =
                        Color.FromArgb(
                            value,
                            value,
                            value);

                    gray.SetPixel(
                        x,
                        y,
                        grayColor);
                }
            }

            enlarged.Dispose();

            Bitmap contrast =
                new Bitmap(
                    gray.Width,
                    gray.Height,
                    PixelFormat.Format24bppRgb);

            for (int y = 0; y < gray.Height; y++)
            {
                for (int x = 0; x < gray.Width; x++)
                {
                    Color pixel =
                        gray.GetPixel(x, y);

                    int value =
                        pixel.R;

                    if (value < 90)
                    {
                        value = 0;
                    }
                    else if (value > 180)
                    {
                        value = 255;
                    }

                    Color newColor =
                        Color.FromArgb(
                            value,
                            value,
                            value);

                    contrast.SetPixel(
                        x,
                        y,
                        newColor);
                }
            }

            gray.Dispose();

            return contrast;
        }
    }

    public class SelectionForm : Form
    {
        private readonly Bitmap screenshot;

        private Point startPoint;
        private Point currentPoint;

        private bool selecting;

        public Rectangle SelectedRectangle { get; private set; }

        public SelectionForm(Bitmap screenshot)
        {
            this.screenshot = screenshot;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = Screen.PrimaryScreen.Bounds;
            TopMost = true;
            Cursor = Cursors.Cross;
            DoubleBuffered = true;
            KeyPreview = true;

            MouseDown += SelectionForm_MouseDown;
            MouseMove += SelectionForm_MouseMove;
            MouseUp += SelectionForm_MouseUp;

            KeyDown += SelectionForm_KeyDown;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.DrawImage(
                screenshot,
                ClientRectangle);

            if (selecting)
            {
                Rectangle rectangle =
                    GetRectangle(
                        startPoint,
                        currentPoint);

                using Pen pen =
                    new Pen(Color.Red, 3);

                e.Graphics.DrawRectangle(
                    pen,
                    rectangle);

                using SolidBrush brush =
                    new SolidBrush(
                        Color.FromArgb(
                            60,
                            Color.Red));

                e.Graphics.FillRectangle(
                    brush,
                    rectangle);
            }
        }

        private void SelectionForm_MouseDown(
            object? sender,
            MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            selecting = true;

            startPoint = e.Location;
            currentPoint = e.Location;

            Invalidate();
        }

        private void SelectionForm_MouseMove(
            object? sender,
            MouseEventArgs e)
        {
            if (!selecting)
            {
                return;
            }

            currentPoint = e.Location;

            Invalidate();
        }

        private void SelectionForm_MouseUp(
            object? sender,
            MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            selecting = false;

            currentPoint = e.Location;

            SelectedRectangle =
                GetRectangle(
                    startPoint,
                    currentPoint);

            if (SelectedRectangle.Width > 5 &&
                SelectedRectangle.Height > 5)
            {
                DialogResult =
                    DialogResult.OK;

                Close();
            }

            Invalidate();
        }

        private void SelectionForm_KeyDown(
            object? sender,
            KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult =
                    DialogResult.Cancel;

                Close();
            }
        }

        private static Rectangle GetRectangle(
            Point p1,
            Point p2)
        {
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);

            int width =
                Math.Abs(p1.X - p2.X);

            int height =
                Math.Abs(p1.Y - p2.Y);

            return new Rectangle(
                x,
                y,
                width,
                height);
        }
    }
}
