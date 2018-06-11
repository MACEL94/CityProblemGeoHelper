﻿using Android.App;
using Android.Content.PM;
using Android.OS;
using Plugin.Permissions;
using Xamarin.Forms.Platform.Android;

namespace GeolocatorSample.Droid
{
    [Activity(Label = "GeolocatorSample", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        #region Public Methods

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        #endregion Public Methods

        #region Protected Methods

        protected override void OnCreate(Bundle bundle)
        {
            FormsAppCompatActivity.ToolbarResource = Resource.Layout.Toolbar;
            FormsAppCompatActivity.TabLayoutResource = Resource.Layout.Tabbar;

            base.OnCreate(bundle);

            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, bundle);
            global::Xamarin.Forms.Forms.Init(this, bundle);
            LoadApplication(new App());
        }

        #endregion Protected Methods
    }
}