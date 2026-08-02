using System;
using System.Collections.Generic;
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
                Text = "ScreenReader — диагностика OCR",
                StartPosition = FormStartPosition.CenterScreen,
                Width = 1250,
                Height = 850
            };

            Label title = new Label
            {
                Text = "Диагностика OCR",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            Label instruction = new Label
            {
                Text = "Настрой пять областей одной строки, затем сравним несколько способов OCR.",
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Location = new Point(20, 50)
            };

            Button setupButton = new Button
            {
                Text = "Настроить области",
                Font = new Font("Segoe UI", 11),
                Width = 220,
                Height = 45,
                Location = new Point(20, 80)
            };

            Button testButton = new Button
            {
                Text = "Запустить диагностику",
                Font = new Font("Segoe UI", 11),
                Width = 240,
                Height = 45,
                Location = new Point(255, 80),
                Enabled = false
            };

            TextBox result = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 10),
                Location = new Point(20, 145),
                Width = 1190,
                Height = 640
            };

            form.Controls.Add(title);
            form.Controls.Add(instruction);
            form.Controls.Add(setupButton);
            form.Controls.Add(testButton);
            form.Controls.Add(result);

            Dictionary<string, Rectangle> areas =
                new Dictionary<string, Rectangle>();

            string[] areaNames =
            {
                "Название корпуса",
                "Корм за час",
                "Вода за час",
                "Корм с 00:00",
                "Вода с 00:00"
            };

            setupButton.Click += (sender, e) =>
            {
                try
                {
                    areas.Clear();

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

                    for (int i = 0; i < areaNames.Length; i++)
                    {
                        using SelectionForm selectionForm =
                            new SelectionForm(
                                screenshot,
                                areaNames[i]);

                        if (selectionForm.ShowDialog() !=
                            DialogResult.OK)
                        {
                            form.Show();
                            form.Activate();

                            result.Text =
                                "Настройка отменена.";

                            return;
                        }

                        Rectangle rectangle =
                            selectionForm.SelectedRectangle;

                        if (rectangle.Width <= 5 ||
                            rectangle.Height <= 5)
                        {
                            form.Show();
                            form.Activate();

                            result.Text =
                                "Выбрана слишком маленькая область.";

                            return;
                        }

                        areas[areaNames[i]] = rectangle;
                    }

                    form.Show();
                    form.Activate();

                    testButton.Enabled = true;

                    result.Text =
                        "Настройка завершена.\r\n\r\n" +
                        "Теперь нажми «Запустить диагностику».";
                }
                catch (Exception ex)
                {
                    form.Show();
                    form.Activate();

                    result.Text =
                        "ОШИБКА:\r\n\r\n" +
                        ex.Message;
                }
            };

            testButton.Click += (sender, e) =>
            {
                if (areas.Count != areaNames.Length)
                {
                    result.Text =
                        "Сначала выполни настройку областей.";

                    return;
                }

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

                    form.Show();
                    form.Activate();

                    result.Text =
                        "ДИАГНОСТИКА OCR\r\n" +
                        "==============================\r\n\r\n";

                    foreach (string name in areaNames)
                    {
                        Rectangle area = areas[name];

                        using Bitmap crop =
                            screenshot.Clone(
                                area,
                                PixelFormat.Format32bppArgb);

                        result.AppendText(
                            "\r\n========================================\r\n" +
                            name +
                            "\r\n========================================\r\n\r\n");

                        // Вариант 1 — оригинал
                        string original =
                            RunOcr(
                                crop,
                                PageSegMode.SingleLine);

                        result.AppendText(
                            "[1] ОРИГИНАЛ\r\n" +
                            original.Trim() +
                            "\r\n\r\n");

                        // Вариант 2 — увеличенный
                        using Bitmap enlarged =
                            ResizeImage(crop, 3);

                        string enlargedText =
                            RunOcr(
                                enlarged,
                                PageSegMode.SingleLine);

                        result.AppendText(
                            "[2] УВЕЛИЧЕНИЕ x3\r\n" +
                            enlargedText.Trim() +
                            "\r\n\r\n");

                        // Вариант 3 — серый
                        using Bitmap gray =
                            MakeGray(enlarged);

                        string grayText =
                            RunOcr(
                                gray,
                                PageSegMode.SingleLine);

                        result.AppendText(
                            "[3] СЕРЫЙ\r\n" +
                            grayText.Trim() +
                            "\r\n\r\n");

                        // Вариант 4 — бинарный
                        using Bitmap binary =
                            MakeBinary(gray);

                        string binaryText =
                            RunOcr(
                                binary,
                                PageSegMode.SingleLine);

                        result.AppendText(
                            "[4] ЧЁРНО-БЕЛЫЙ\r\n" +
                            binaryText.Trim() +
                            "\r\n\r\n");

                        // Вариант 5 — режим текста
                        string sparseText =
                            RunOcr(
                                enlarged,
                                PageSegMode.SparseText);

                        result.AppendText(
                            "[5] SPARSE TEXT\r\n" +
                            sparseText.Trim() +
                            "\r\n\r\n");
                    }

                    result.AppendText(
                        "\r\n========================================\r\n" +
                        "ДИАГНОСТИКА ЗАВЕРШЕНА\r\n" +
                        "========================================\r\n");
                }
                catch (Exception ex)
                {
                    form.Show();
                    form.Activate();

                    result.Text =
                        "ОШИБКА:\r\n\r\n" +
                        ex.Message +
                        "\r\n\r\n" +
                        ex.StackTrace;
                }
            };

            Application.Run(form);
        }

        private static string RunOcr(
            Bitmap image,
            PageSegMode mode)
        {
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

            image.Save(
                stream,
                System.Drawing.Imaging.ImageFormat.Png);

            byte[] bytes =
                stream.ToArray();

            using Pix pix =
                Pix.LoadFromMemory(bytes);

            using Page page =
                engine.Process(
                    pix,
                    mode);

            return page.GetText();
        }

        private static Bitmap ResizeImage(
            Bitmap source,
            int scale)
        {
            Bitmap result =
                new Bitmap(
                    source.Width * scale,
                    source.Height * scale,
                    PixelFormat.Format24bppRgb);

            using Graphics graphics =
                Graphics.FromImage(result);

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
                    result.Width,
                    result.Height));

            return result;
        }

        private static Bitmap MakeGray(
            Bitmap source)
        {
            Bitmap result =
                new Bitmap(
                    source.Width,
                    source.Height,
                    PixelFormat.Format24bppRgb);

            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    Color pixel =
                        source.GetPixel(x, y);

                    int value =
                        (int)(
                            pixel.R * 0.299 +
                            pixel.G * 0.587 +
                            pixel.B * 0.114);

                    result.SetPixel(
                        x,
                        y,
                        Color.FromArgb(
                            value,
                            value,
                            value));
                }
            }

            return result;
        }

        private static Bitmap MakeBinary(
            Bitmap source)
        {
            Bitmap result =
                new Bitmap(
                    source.Width,
                    source.Height,
                    PixelFormat.Format24bppRgb);

            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                {
                    int value =
                        source.GetPixel(x, y).R;

                    value =
                        value < 90
                            ? 0
                            : 255;

                    result.SetPixel(
                        x,
                        y,
                        Color.FromArgb(
                            value,
                            value,
                            value));
                }
            }

            return result;
        }
    }

    public class SelectionForm : Form
    {
        private readonly Bitmap screenshot;
        private readonly string areaName;

        private Point startPoint;
        private Point currentPoint;

        private bool selecting;

        public Rectangle SelectedRectangle { get; private set; }

        public SelectionForm(
            Bitmap screenshot,
            string areaName)
        {
            this.screenshot = screenshot;
            this.areaName = areaName;

            FormBorderStyle =
                FormBorderStyle.None;

            StartPosition =
                FormStartPosition.Manual;

            Bounds =
                Screen.PrimaryScreen.Bounds;

            TopMost = true;
            Cursor = Cursors.Cross;
            DoubleBuffered = true;
            KeyPreview = true;

            MouseDown += SelectionForm_MouseDown;
            MouseMove += SelectionForm_MouseMove;
            MouseUp += SelectionForm_MouseUp;
            KeyDown += SelectionForm_KeyDown;
        }

        protected override void OnPaint(
            PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.DrawImage(
                screenshot,
                ClientRectangle);

            using SolidBrush overlay =
                new SolidBrush(
                    Color.FromArgb(
                        100,
                        Color.Black));

            e.Graphics.FillRectangle(
                overlay,
                ClientRectangle);

            using SolidBrush textBrush =
                new SolidBrush(
                    Color.White);

            using Font font =
                new Font(
                    "Segoe UI",
                    18,
                    FontStyle.Bold);

            e.Graphics.DrawString(
                "Выберите: " + areaName,
                font,
                textBrush,
                20,
                20);

            if (selecting)
            {
                Rectangle rectangle =
                    GetRectangle(
                        startPoint,
                        currentPoint);

                using SolidBrush selectedBrush =
                    new SolidBrush(
                        Color.FromArgb(
                            80,
                            Color.White));

                using Pen pen =
                    new Pen(
                        Color.Red,
                        3);

                e.Graphics.FillRectangle(
                    selectedBrush,
                    rectangle);

                e.Graphics.DrawRectangle(
                    pen,
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
            int x =
                Math.Min(p1.X, p2.X);

            int y =
                Math.Min(p1.Y, p2.Y);

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
