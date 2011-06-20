using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Windows.Threading;

namespace Sparpotify7.ViewModel
{
    /// <summary>
    /// ViewModel Base
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// navigation Service
        /// </summary>
        private NavigationService navigationService;

        /// <summary>
        /// Gets the navigation service.
        /// </summary>
        public NavigationService NavigationService
        {
            get { return this.navigationService; }
        }

        public Dispatcher CurrentDispatcher { get { return Deployment.Current.Dispatcher; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelBase"/> class.
        /// </summary>
        /// <param name="view">The view.</param>
        public ViewModelBase(Page view) : this(view.NavigationService)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelBase"/> class.
        /// </summary>
        /// <param name="navigationService">The navigation service.</param>
        public ViewModelBase(NavigationService navigationService)
        {
            this.navigationService = navigationService;
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void OnPropertyChanged(String propertyName)
        {
            this.CurrentDispatcher.BeginInvoke(() =>
            {
                PropertyChangedEventHandler handler = PropertyChanged;
                if (null != handler)
                {
                    handler(this, new PropertyChangedEventArgs(propertyName));
                }
            });
        }
    }
}
