using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Tesseract;


namespace SnipChatGpt
{
    public partial class MainForm : Form
    {
        /*This program is suitable to answer simple question using chatgpt
        Step/Function: 
        -User may need to click "Capture + Ask GPT" on the screen need to ask gpt
        -User need to drag the red box to select the area from the screenshot for converting the pic to text
         *User can click cancel or close the form when wrongly selected 
        -If user click ok other pop up message from chatgpt will be shown 

        button screenshot from screen and crop the part need to ask chatgpt, then the answer will be prompt if the user assume the ocr info is correct
        **For testing purpose, https://openrouter.ai/settings/keys for get api key and replace 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
        */
        public MainForm()
        {
            InitializeComponent();
            this.Text = "Screenshot OCR to ChatGPT";
            var btn = new Button() { Text = "Capture + Ask GPT", Dock = DockStyle.Fill };
            btn.Click += async (s, e) => await CaptureAndProcess();
            Controls.Add(btn);
        }


        private async Task CaptureAndProcess()
        {
            this.Hide(); // Hide main form

            Bitmap screenshot = CaptureScreen();
            var cropped = CropImageInteractively(screenshot);

            if (cropped == null)
            {
                this.Show();
                return; // Esc or invalid selection
            }

            string extractedText = ExtractTextFromImage(cropped);
            if (string.IsNullOrEmpty(extractedText))
            {
                MessageBox.Show("OCR Result:\nEmpty text, try again", "OCR");
                this.Show();
                return;
            }

            var result = MessageBox.Show($"OCR Result:\n{extractedText}", "OCR", MessageBoxButtons.OKCancel);

            if (result == DialogResult.OK)
            {
                string gptReply = await AskChatGPT(extractedText);
                MessageBox.Show(gptReply, "ChatGPT");
                this.Show(); // Show main form again after everything


            }
            else
            {
                this.Show(); // Show main form again after everything
                return;
            }
        }


        private Bitmap CaptureScreen()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }
            return bitmap;
        }

        private Bitmap CropImageInteractively(Bitmap image)
        {
            using (var form = new CropForm(image))
            {
                return form.ShowDialog() == DialogResult.OK ? form.CroppedImage : null;
            }
        }

        private string ExtractTextFromImage(Bitmap image)
        {
            using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            using var pix = Helper.BitmapToPix(image); // use our helper here
            using var page = engine.Process(pix);
            return page.GetText();
        }

        private async Task<string> AskChatGPT(string prompt)
        {

            var client = new HttpClient();
            var requestBody = new
            {
                model = "openai/gpt-3.5-turbo", // You can try mistralai/mixtral-8x7b too
                messages = new[]
                {
            new { role = "user", content = prompt }
        }
            };

            var requestJson = JsonConvert.SerializeObject(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            request.Headers.Add("Authorization", "Bearer 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"); // Replace this
            request.Headers.Add("HTTP-Referer", "http://localhost"); // Required by OpenRouter
            request.Headers.Add("X-Title", "SnipChatGptApp");

            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            try
            {
                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                //MessageBox.Show(responseBody); // Add this to debug

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"OpenRouter Error: {response.StatusCode}\n\n{responseBody}", "API Error");
                    return "OpenRouter API error.";
                }

                dynamic result = JsonConvert.DeserializeObject(responseBody);
                if (result?.choices == null || result.choices.Count == 0)
                {
                    MessageBox.Show("No response from OpenRouter.", "API Error");
                    return "No response from OpenRouter.";
                }

                return result.choices[0].message.content.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}\n\n{ex.InnerException?.Message}", "Exception");
                return $"Exception: {ex.Message}";
            }
        }


    }
}


