//#define DEBUG_AGENT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Scheduler;
using Windows.Phone.System.UserProfile;
using Commons;
using System.IO.IsolatedStorage;

namespace Grow_Up
{
    public partial class SettingsPage : PhoneApplicationPage
    {
        bool shouldChange = false;

        public SettingsPage()
        {
            InitializeComponent();

            LayoutRoot.DataContext = App.ViewModelData;

            Loaded += SettingsPage_Loaded;
        }

        void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            // do nothin'
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (IsolatedStorageSettings.ApplicationSettings.Contains("shouldChange"))
            {
                shouldChange = (bool)IsolatedStorageSettings.ApplicationSettings["shouldChange"];
            }

            if (LockScreenManager.IsProvidedByCurrentApplication && shouldChange)
            {
                //Btn_SetLockscreen.IsEnabled = false;
                LockscreenProvider_Toggle.IsChecked = true;
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            App.ViewModelData.SaveSettings();
            Loaded -= SettingsPage_Loaded;
            base.OnNavigatedFrom(e);
        }

        //private async void SetLockscreen(object sender, RoutedEventArgs e)
        //{
        //    if (!LockScreenManager.IsProvidedByCurrentApplication)
        //    {
        //        var result = await LockScreenManager.RequestAccessAsync();
        //    }

        //    // start the background agent to dynamically change the lockscreen
        //    if (LockScreenManager.IsProvidedByCurrentApplication)
        //    {
        //        //Btn_SetLockscreen.IsEnabled = false;
        //        StartAgent();
        //    }
        //}

        private void LockscreenProvider_Toggle_Unchecked(object sender, RoutedEventArgs e)
        {
            PeriodicTask _lockscreenTask;
            string periodicTaskName = "PeriodicAgent";
            _lockscreenTask = ScheduledActionService.Find(periodicTaskName) as PeriodicTask;

            if (_lockscreenTask != null)
            {
                try
                {
                    ScheduledActionService.Remove(periodicTaskName);
                }
                catch (Exception)
                {
                }
            }

            HyperlinkBtn_Settings.Visibility = Visibility.Visible;
            IsolatedStorageSettings.ApplicationSettings["shouldChange"] = false;
        }

        private async void LockscreenProvider_Toggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!LockScreenManager.IsProvidedByCurrentApplication)
            {
                var result = await LockScreenManager.RequestAccessAsync();
            }

            if (LockScreenManager.IsProvidedByCurrentApplication)
            {
                IsolatedStorageSettings.ApplicationSettings["shouldChange"] = true;
                StartAgent();
            }
            else
            {
                LockscreenProvider_Toggle.IsChecked = false;
            }
        }

        public static void StartAgent()
        {
            PeriodicTask _lockscreenTask;
            string periodicTaskName = "PeriodicAgent";

            _lockscreenTask = ScheduledActionService.Find(periodicTaskName) as PeriodicTask;

            if (_lockscreenTask != null)
            {
                try
                {
                    ScheduledActionService.Remove(periodicTaskName);
                }
                catch (Exception)
                {
                }
            }

            _lockscreenTask = new PeriodicTask(periodicTaskName)
            {
                ExpirationTime = DateTime.Now.AddDays(14),
                Description = "This demonstrates a periodic task."
            };

            // Place the call to Add in a try block in case the user has disabled agents.
            try
            {
                ScheduledActionService.Add(_lockscreenTask);

                // If debugging is enabled, use LaunchForTest to launch the agent in one minute.
#if(DEBUG_AGENT)
                    ScheduledActionService.LaunchForTest(periodicTaskName, TimeSpan.FromSeconds(15));
#endif
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    MessageBox.Show("Background agents for this application have been disabled by the user.");
                }

                if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.
                }

            }
            catch (SchedulerServiceException)
            {
                // No user action required.  
            }
        }
    }
}