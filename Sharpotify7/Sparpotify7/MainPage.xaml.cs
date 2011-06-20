using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.IO;
using System.IO.IsolatedStorage;
using Sharpotify;
using Sharpotify.Enums;


namespace Sparpotify7
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            Facade.Init(this.Dispatcher);
        }

        public void SpotifyStartted(bool loggedin)
        {
            if (loggedin)
            {
                NavigationService.Navigate(new Uri("/View/MainPanorama.xaml", UriKind.Relative));
            }
            else
            {
                loginPanel.Visibility = Visibility.Visible;
                progress.Visibility = Visibility.Collapsed;
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            Facade.Login(txtUser.Text, txtPassword.Password, SpotifyStartted);
            loginPanel.Visibility = Visibility.Collapsed;
            progress.Visibility = Visibility.Visible;
        }
    }
}