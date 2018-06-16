using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace CityProblemGeoHelperPCL
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
        internal static void DisplayAlertExceptionOnMain(this Page page, Exception ex)
        {
            Device.BeginInvokeOnMainThread(() => page.DisplayAlert("Uh oh, something went wrong", $"Please send a screen of this to the app admin. {ex}", "OK"));
        }

        /// <summary>
        /// Permette di mostrare all'utente i messaggi dato che se si inizializza un thread 
        /// da un thread secondario si rischia che il secondario finisca prima che l'utente abbia visto il messaggio
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <param name="button"></param>
        internal static void DisplayAlertOnMain(this Page page, string message, string title, string button)
        {
            Device.BeginInvokeOnMainThread(() => page.DisplayAlert(message, title, button));
        }

        /// <summary>
        /// Controlla se l'email inserita è valida
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        internal static bool CheckValidEmail(string text)
        {
            if (text == null)
            {
                return false;
            }

            Regex regex = new Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            Match match = regex.Match(text);

            return match.Success;
        }

        #endregion Public Methods
    }
}
