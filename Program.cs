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
        private static readonly string AreasFile =
            Path.Combine(AppContext.BaseDirectory, "areas.txt");

        private static readonly string[] AreaNames =
        {
            "Название корпуса",
            "Корм за час",
            "Вода за час",
            "Корм с 00:00",
            "Вода с 00:00"
        };

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Form form = new Form
            {
                Text = "ScreenReader — чтение данных",
                StartPosition = FormStartPosition.CenterScreen,
                Width = 1200,
                Height = 800
            };

            Label title = new Label
            {
                Text = "Чтение данных корпуса",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            Label instruction = new Label
            {
                Text =
                    "Сначала настрой области. После этого программа сможет читать строку автоматически.",
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

            Button readButton = new Button
            {
                Text = "Считать данные",
                Font = new Font("Segoe UI", 11),
                Width = 220,
                Height = 45,
                Location = new Point(255, 80)
            };

            TextBox result = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 12),
                Location = new Point(20, 145),
                Width = 1140,
                Height = 560
            };

            form.Controls.Add(title);
            form.Controls.Add(instruction);
            form.Controls.Add(setupButton);
            form.Controls.Add(readButton);
            form.Controls.Add(result);

            Dictionary<string, Rectangle> areas =
                LoadAreas();

            if (areas.Count == AreaNames.Length)
            {
                result.Text =
                    "Области загружены из areas.txt.\r\n\r\n" +
                    "Можно нажать «Считать данные».";
            }
            else
            {
                result.Text =
                    "Области ещё не настроены.\r\n\r\n" +
                    "Нажми «Настроить области».";
            }

            setupButton.Click += (sender, e) =>
            {
                try
                {
                    Dictionary<string, Rectangle> newAreas =
                        SelectAreas(form);

                    if (newAreas.Count != AreaNames.Length)
                    {
                        return;
                    }

                    areas = newAreas;

                    SaveAreas(areas);

                    result.Text =
                        "ОБЛАСТИ СОХРАНЕНЫ\r\n\r\n" +
                        "Файл:\r\n" +
                        AreasFile +
                        "\r\n\r\n" +
                        "Теперь можно нажать «Считать данные».";
                }
                catch (Exception ex)
                {
                    form.Show();
                    form.Activate();

                    result.Text =
                        "ОШИБКА НАСТРОЙКИ:\r\n\r\n" +
                        ex.Message;
                }
            };

            readButton.Click += (sender, e) =>
            {
                if (areas.Count != AreaNames.Length)
                {
                    result.Text =
                        "Сначала нужно настроить области.";

                    return;
                }

                try
                {
                    result.Text =
                        "Считываю данные...\r\n";

                    Application.DoEvents();

                    using Bitmap screenshot =
                        CaptureScreen();

                    string[] values =
                        new string[AreaNames.Length];

                    for (int i = 0; i < AreaNames.Length; i++)
                    {
                        string name =
                            AreaNames[i];

                        Rectangle area =
                            areas[name];

                        using Bitmap crop =
                            screenshot.Clone(
                                area,
                                PixelFormat.Format32bppArgb);

                        values[i] =
                            RunSparseOcr(crop).Trim();
                    }

                    string text =
                        "РЕЗУЛЬТАТ СЧИТЫВАНИЯ\r\n" +
                        "==============================\r\n\r\n" +

                        "Корпус:\r\n" +
                        values[0] +
                        "\r\n\r\n" +

                        "Корм за час:\r\n" +
                        values[1] +
                        "\r\n\r\n" +

                        "Вода за час:\r\n" +
                        values[2] +
                        "\r\n\r\n" +

                        "Корм с 00:00:\r\n" +
                        values[3] +
                        "\r\n\r\n" +

                        "Вода с 00:00:\r\n" +
                        values[4];

                    result.Text = text;
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

            Application.Run(form);
        }

        private static Dictionary<string, Rectangle> SelectAreas(
            Form form)
        {
            Dictionary<string, Rectangle> result =
                new Dictionary<string, Rectangle>();

            form.Hide();

            Application.DoEvents();

            System.Threading.Thread.Sleep(300);

            using Bitmap screenshot =
                CaptureScreen();

            for (int i = 0; i < AreaNames.Length; i++)
            {
                using SelectionForm selectionForm =
                    new SelectionForm(
                        screenshot,
                        AreaNames[i]);

                DialogResult dialogResult =
                    selectionForm.ShowDialog();

                if (dialogResult != DialogResult.OK)
                {
                    form.Show();
                    form.Activate();

                    return result;
                }

                Rectangle rectangle =
                    selectionForm.SelectedRectangle;

                if (rectangle.Width <= 5 ||
                    rectangle.Height <= 5)
                {
                    form.Show();
                    form.Activate();

                    return result;
                }

                result[AreaNames[i]] =
                    rectangle;
            }

            form.Show();
            form.Activate();

            return result;
        }

        private static Bitmap CaptureScreen()
        {
            Rectangle screenBounds =
                Screen.PrimaryScreen.Bounds;

            Bitmap screenshot =
                new Bitmap(
                    screenBounds.Width,
                    screenBounds.Height,
                    PixelFormat.Format32bppArgb);

            using Graphics graphics =
                Graphics.FromImage(screenshot);

            graphics.CopyFromScreen(
                screenBounds.Left,
                screenBounds.Top,
                0,
                0,
                screenBounds.Size);

            return screenshot;
        }

        private static string RunSparseOcr(
            Bitmap source)
        {
            using Bitmap enlarged =
                ResizeImage(source, 3);

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

            enlarged.Save(
                stream,
                System.Drawing.Imaging.ImageFormat.Png);

            byte[] bytes =
                stream.ToArray();

            using Pix pix =
                Pix.LoadFromMemory(bytes);

            using Page page =
                engine.Process(
                    pix,
                    PageSegMode.SparseText);

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

        private static void SaveAreas(
            Dictionary<string, Rectangle> areas)
        {
            using StreamWriter writer =
                new StreamWriter(
                    AreasFile,
                    false);

            foreach (string name in AreaNames)
            {
                Rectangle r =
                    areas[name];

                writer.WriteLine(
                    $"{name}|{r.X}|{r.Y}|{r.Width}|{r.Height}");
            }
        }

        private static Dictionary<string, Rectangle> LoadAreas()
        {
            Dictionary<string, Rectangle> areas =
                new Dictionary<string, Rectangle>();

            if (!File.Exists(AreasFile))
            {
                return areas;
            }

            try
            {
                string[] lines =
                    File.ReadAllLines(AreasFile);

                foreach (string line in lines)
                {
                    string[] parts =
                        line.Split('|');

                    if (parts.Length != 5)
                    {
                        continue;
                    }

                    string name =
                        parts[0];

                    if (!int.TryParse(parts[1], out int x))
                    {
                        continue;
                    }

                    if (!int.TryParse(parts[2], out int y))
                    {
                        continue;
                    }

                    if (!int.TryParse(parts[3], out int width))
                    {
                        continue;
                    }

                    if (!int.TryParse(parts[4], out int height))
                    {
                        continue;
                    }

                    areas[name] =
                        new Rectangle(
                            x,
                            y,
                            width,
                            height);
                }
            }
            catch
            {
                areas.Clear();
            }

            return areas;
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
