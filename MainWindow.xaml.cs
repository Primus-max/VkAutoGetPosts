using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using static TdLib.TdApi;
using static TdLib.TdApi.MessageContent;
using File = System.IO.File;


using System.Drawing;
using System.IO;
using TdLib;
using Newtonsoft.Json;
using System.Drawing.Imaging;
using System.Threading;

namespace GetPoster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }


        #region СТАРТ ПРОГРАММЫ
        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            string filePath = "authdata.json";
            string jsonString = File.ReadAllText(filePath);

            AuthData? authData = JsonConvert.DeserializeObject<AuthData>(jsonString);

            if (authData?.PhoneNumber != PhoneNumberText.Text)
            {
                authData.PhoneNumber = PhoneNumberText.Text;
            }
            if (authData?.ApiHash != APIHashText.Text)
            {
                authData.ApiHash = APIHashText.Text;
            }
            if (authData?.ApiId != APIIDText.Text)
            {
                authData.ApiId = APIIDText.Text;
            }

            if (authData != null)
            {
                string updatedJsonString = JsonConvert.SerializeObject(authData);
                File.WriteAllText(filePath, updatedJsonString);
            }

            TGClient client = new();
            string phoneNumber = PhoneNumberText.Text;
            string codeAuth = String.Empty;
            string apiHash = APIHashText.Text;
            int apiId = Convert.ToInt32(APIIDText.Text);


            await client.ConnectAsync(phoneNumber, codeAuth, apiHash, apiId);

            await Task.Delay(3000);

            while (true)
            {
                var chats = await client.GetChatsAsync();
                foreach (var chat in chats)
                {
                    await client.GetAllChatMessagesAsync(chat.Id);
                }
                await Task.Delay(TimeSpan.FromDays(2));
            }
        }
        #endregion


        private void Win_Loaded(object sender, RoutedEventArgs e)
        {
            string filePath = "authdata.json";
            string jsonString = File.ReadAllText(filePath);

            AuthData? authData = JsonConvert.DeserializeObject<AuthData>(jsonString);

            PhoneNumberText.Text = authData?.PhoneNumber;
            APIHashText.Text = authData?.ApiHash;
            APIIDText.Text = authData?.ApiId;
        }
    }
}
