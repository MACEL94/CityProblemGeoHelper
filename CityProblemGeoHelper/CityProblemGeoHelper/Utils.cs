using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CityProblemGeoHelper
{
    public static class Utils
    {
        #region Public Methods

        public static async Task<bool> CheckPermissions(Permission permission)
        {
            var permissionStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(permission).ConfigureAwait(false);

            // Se non ha i permessi li chiede
            if (permissionStatus != PermissionStatus.Granted)
            {
                // Richiesta
                var newStatus = await CrossPermissions.Current.RequestPermissionsAsync(permission).ConfigureAwait(false);

                // Mostra l'errore se l'utente non da il permesso di utilizzo
                if (newStatus.ContainsKey(permission) && newStatus[permission] != PermissionStatus.Granted)
                {
                    var title = $"{permission} Permission";
                    var question = $"To use the plugin the {permission} permission is required.";
                    const string positive = "Settings";
                    const string negative = "Maybe Later";

                    var task = Application.Current?.MainPage?.DisplayAlert(title, question, positive, negative);

                    // Se non riesce a creare il task la risposta sarà sempre negativa
                    if (task == null)
                    {
                        return false;
                    }

                    var result = await task;
                    if (result)
                    {
                        CrossPermissions.Current.OpenAppSettings();
                    }
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Loggo sempre seguendo lo stesso standard
        /// </summary>
        /// <param name="page"></param>
        /// <param name="ex"></param>
        internal static async Task DisplayAlertException(this Page page, Exception ex)
        {
            await page.DisplayAlert("Uh oh, something went wrong", $"Please send a screen of this to the app admin. {ex}", "OK").ConfigureAwait(false);
        }

        #endregion Public Methods
    }
}
