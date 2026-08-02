using System;
using System.Drawing;
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

            form.Text = "ScreenReader — тест считывания";
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Width = 900;
            form.Height = 600;

            Label title = new Label();

            title.Text = "Прототип считывания данных с экрана";
            title.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            title.AutoSize = true;
            title.Location = new Point(25, 25);

            Button captureButton = new Button();

            captureButton.Text = "Считать область экрана";
            captureButton.Font = new Font("Segoe UI", 11);
            captureButton.Width = 220;
            captureButton.Height = 45;
            captureButton.Location = new Point(25, 80);

            TextBox result = new TextBox();

            result.Multiline = true;
            result.ReadOnly = true;
            result.ScrollBars = ScrollBars.Vertical;
            result.Font = new Font("Consolas", 12);
            result.Location = new Point(25, 150);
            result.Width = 830;
            result.Height = 350;

            form.Controls.Add(title);
            form.Controls.Add(captureButton);
            form.Controls.Add(result);

            captureButton.Click += (sender, e) =>
            {
                result.Text =
                    "Кнопка работает.\r\n\r\n" +
                    "Следующим этапом добавим захват области экрана.";
            };

            Application.Run(form);
        }
    }
}
