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
using Sparpotify7.ViewModel;

namespace Sparpotify7.View
{
    public partial class MainPanorama : PhoneApplicationPage
    {
        public MainPanorama()
        {
            InitializeComponent();
            
        }
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            NavigationService.RemoveBackEntry();
            // Set the data context of the listbox control to the sample data
            DataContext = new MainPanoramaViewModel(this);
        }
    }
}