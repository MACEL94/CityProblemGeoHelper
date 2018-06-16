using Xamarin.Forms;

namespace CityProblemGeoHelperPCL
{
    public class App : Application
    {
        #region Public Constructors

        public App()
        {
            // The root page of your application
            MainPage = new NavigationPage(new HomePage());
        }

        #endregion Public Constructors

        #region Protected Methods

        protected override void OnResume()
        {
            // Handle when your app resumes
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        #endregion Protected Methods
    }
}