using System;
using System.Collections.Generic;
using System.Printing;
using System.Windows;
using EbayWpfUploader;


namespace EbayWpfUploader
{
    public partial class MainWindow : Window
    {
        private EbayListingManager manager = new EbayListingManager();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            UploadButton.IsEnabled = false;
            StatusText.Text = "Getting access token...";

            try
            {
                await manager.InitializeAsync();

                var lines = InputBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var items = new List<(string, string, string)>();

                foreach (var line in lines)
                {
                    var parts = line.Split('|');
                    if (parts.Length == 3)
                        items.Add((parts[0].Trim(), parts[1].Trim(), parts[2].Trim()));
                }

                await manager.CreateListingsAsync(items,
                    "YOUR_PAYMENT_POLICY_ID",
                    "YOUR_RETURN_POLICY_ID",
                    "YOUR_FULFILLMENT_POLICY_ID");

                StatusText.Text = "✅ Listings uploaded successfully.";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"❌ Error: {ex.Message}";
            }

            UploadButton.IsEnabled = true;
        }
    }
}
