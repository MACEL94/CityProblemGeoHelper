using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace GeolocatorSample
{
    public static class Utils
    {
        #region Public Methods

        public static async Task<bool> CheckPermissions(Permission permission)
        {
            var permissionStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(permission).ConfigureAwait(false);
            bool shouldRequest = false;

            if (permissionStatus == PermissionStatus.Denied)
            {
                if (Device.RuntimePlatform == Device.iOS)
                {
                    var title = $"{permission} Permission";
                    var question = $"To use this plugin the {permission} permission is required. Please go into Settings and turn on {permission} for the app.";
                    var positive = "Settings";
                    var negative = "Maybe Later";

                    // Se possibile, crea un dialog che mostra all'utente IOS la richiesta del permesso
                    var task = Application.Current?.MainPage?.DisplayAlert(title, question, positive, negative);

                    // Se non riesce a creare il task la risposta sarà sempre negativa
                    if (task == null)
                    {
                        return false;
                    }

                    // Se l'utente clicca "Settings" allora apro le opzioni
                    var result = await task;
                    if (result)
                    {
                        CrossPermissions.Current.OpenAppSettings();
                    }

                    // Ritorna il risultato che al momento è false
                    return false;
                }

                // Imposto la richiesta a true perchè la devo fare
                shouldRequest = true;
            }

            // Se la richiesta sa che va eseguita o se  lo stato è diverso da concesso
            if (shouldRequest || permissionStatus != PermissionStatus.Granted)
            {
                // Lo chiede
                var newStatus = await CrossPermissions.Current.RequestPermissionsAsync(permission).ConfigureAwait(false);

                // Mostra l'errore se di nuovo l'utente non da il permesso di utilizzo
                if (newStatus.ContainsKey(permission) && newStatus[permission] != PermissionStatus.Granted)
                {
                    var title = $"{permission} Permission";
                    var question = $"To use the plugin the {permission} permission is required.";
                    var positive = "Settings";
                    var negative = "Maybe Later";

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
        internal static async Task DisplayAlertException(this Page page, Exception ex)
        {
            Console.WriteLine();
            await page.DisplayAlert("Uh oh, something went wrong", $"Please send a screen of this to the app admin. {ex}", "OK").ConfigureAwait(false);
        }

        #endregion Public Methods
    }
}