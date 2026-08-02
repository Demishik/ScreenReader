using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
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
                Text = "ScreenReader — настройка областей",
                StartPosition = FormStartPosition.CenterScreen,
                Width = 1200,
                Height = 800
            };

            Label title = new Label
            {
                Text = "Настройка областей строки",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            Label instruction = new Label
            {
                Text =
                    "Сначала нажми «Настроить области» и последовательно выдели 5 областей строки К-05.",
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
                Text = "Проверить OCR",
                Font = new Font("Segoe UI", 11),
                Width = 220,
                Height = 45,
                Location = new Point(255, 80),
                Enabled = false
            };

            TextBox result = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 11),
                Location = new Point(20, 145),
                Width = 1140,
                Height = 560
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

                        DialogResult selectionResult =
                            selectionForm.ShowDialog();

                        if (selectionResult != DialogResult.OK)
                        {
                            form.Show();
                            form.Activate();

                            result.Text =
                                "Настройка отменена.\r\n\r\n" +
                                "Ни одна область не была сохранена.";

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
                        "НАСТРОЙКА ЗАВЕРШЕНА\r\n\r\n";

                    foreach (string name in areaNames)
                    {
                        Rectangle r = areas[name];

                        result.AppendText(
                            name + "\r\n" +
                            $"X = {r.X}\r\n" +
                            $"Y = {r.Y}\r\n" +
                            $"Ширина = {r.Width}\r\n" +
                            $"Высота = {r.Height}\r\n\r\n");
                    }
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
                        "Сначала нужно выполнить настройку областей.";

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
                        "ПРОВЕРКА OCR\r\n\r\n";

                    foreach (string name in areaNames)
                    {
                        Rectangle area =
                            areas[name];

                        using Bitmap crop =
                            screenshot.Clone(
                                area,
                                PixelFormat.Format32bppArgb);

                        using Bitmap processed =
                            PrepareImage(crop);

                        string text =
                            RunOcr(processed);

                        result.AppendText(
                            "============================\r\n" +
                            name + "\r\n" +
                            "============================\r\n" +
                            text.Trim() +
                            "\r\n\r\n");
                    }
                }
                catch (Exception ex)
                {
                    form.Show();
                    form.Activate();

                    result.Text =
                        "ОШИБКА OCR:\r\n\r\n" +
                        ex.Message +
                        "\r\n\r\n" +
                        ex.StackTrace;
                }
            };

            Application.Run(form);
        }

        private static string RunOcr(Bitmap image)
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
                    PageSegMode.SingleLine);

            return page.GetText();
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

                    gray.SetPixel(
                        x,
                        y,
                        Color.FromArgb(
                            value,
                            value,
                            value));
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
                    int value =
                        gray.GetPixel(x, y).R;

                    if (value < 90)
                    {
                        value = 0;
                    }
                    else if (value > 180)
                    {
                        value = 255;
                    }

                    contrast.SetPixel(
                        x,
                        y,
                        Color.FromArgb(
                            value,
                            value,
                            value));
                }
            }

            gray.Dispose();

            return contrast;
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
                new SolidBrush(Color.White);

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
