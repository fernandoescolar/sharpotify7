using System.Windows.Navigation;
using Microsoft.Phone.BackgroundAudio;
using Microsoft.Phone.Controls;
using System.Windows;
using System;
using Sparpotify7.Player;
using Sparpotify7.ViewModel;
using System.Windows.Media;

namespace Sparpotify7.View
{
    public partial class PlayerPage : PhoneApplicationPage
    {
        public PlayerPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Set the data context of the listbox control to the sample data
            DataContext = new PlayerViewModel(this, Play);
        }

        protected void Play(MediaStreamSource source)
        {
            
        }
    }
}