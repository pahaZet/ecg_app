using ecg_app.ViewModels;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace ecg_app.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EcgPage : ContentPage
    {
        EcgViewModel model = null;

        public EcgPage()
        {
            InitializeComponent();
            model = new EcgViewModel();
            BindingContext = model;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            model.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            model.OnDisappearing();
        }

    }
}