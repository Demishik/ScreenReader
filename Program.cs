using System;
using System.Drawing;
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

            Form form = new Form();

            form.Text = "ScreenReader — OCR тест";
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Width = 1200;
            form.Height = 800;

            Label title = new Label
            {
                Text = "Тест распознавания текста с экрана",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            };

            Button captureButton = new Button
            {
                Text = "Считать экран",
                Font = new Font("Segoe UI", 11),
                Width = 220,
                Height = 45,
                Location = new Point(20, 65)
            };

            TextBox result = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 11),
                Location = new Point(20, 125),
                Width = 1140,
                Height = 580
            };

            form.Controls.Add(title);
            form.Controls.Add(captureButton);
            form.Controls.Add(result);

            captureButton.Click += (sender, e) =>
            {
                try
                {
                    Rectangle bounds = Screen.PrimaryScreen.Bounds;

                    using Bitmap screenshot = new Bitmap(
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

                    string tessdataPath =
                        Path.Combine(
                            AppContext.BaseDirectory,
                            "tessdata");

                    using TesseractEngine engine =
                        new TesseractEngine(
                            tessdataPath,
                            "rus",
                            EngineMode.Default);

                    using MemoryStream stream = new MemoryStream();

                    screenshot.Save(
                        stream,
                        System.Drawing.Imaging.ImageFormat.Png);

                    byte[] imageBytes = stream.ToArray();

                    using Pix image =
                        Pix.LoadFromMemory(imageBytes);

                    using Page page =
                        engine.Process(image);

                    string text = page.GetText();

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        result.Text =
                            "OCR не распознал текст.\r\n\r\n" +
                            "Попробуем изменить область или настройки распознавания.";
                    }
                    else
                    {
                        result.Text = text;
                    }
                }
                catch (Exception ex)
                {
                    result.Text =
                        "ОШИБКА:\r\n\r\n" +
                        ex.Message +
                        "\r\n\r\n" +
                        ex.StackTrace;
                }
            };

            Application.Run(form);
        }
    }
}
