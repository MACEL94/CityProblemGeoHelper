using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Collections.Generic;
using Plugin.Media;
using Plugin.Permissions.Abstractions;
using Plugin.Media.Abstractions;
using Plugin.Geolocator.Abstractions;
using Plugin.Geolocator;
using System.Linq;

namespace CityProblemGeoHelper
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : TabbedPage
    {
        /// <summary>
        /// Permessi necessari da controllare e in caso richiedere ad ogni avvio se necessario
        /// </summary>
        private readonly List<Permission> _neededPermissions =
            new List<Permission>
            {
                Permission.Camera,
                Permission.Location
            };

        /// <summary>
        /// Map-Key necessaria alle app UWP per Bing.
        /// </summary>
        private const string MY_MAP_KEY = "AiyBWk-XD4TvWYsi9WEXiZvpi5YyGTXDPIsdsd9u3r76T63OmZB3ADGCj2Zpklny";

        /// <summary>
        /// Tengo in memoria l'address salvato
        /// </summary>
        private Address _currentAddress;

        /// <summary>
        /// Tengo in memoria la posizione comprensiva di heading(direzione)
        /// </summary>
        private Position _currentPosition;

        public HomePage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Click sul pulsante che permette di prendere la foto geolocalizzata
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonGetPhoto_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Controlla i permessi, se non presenti li richiede
                foreach (var permission in _neededPermissions)
                {
                    var hasPermission = await Utils.CheckPermissions(permission).ConfigureAwait(false);
                    if (!hasPermission)
                    {
                        return;
                    }
                }

                // Inizializzo ora la fotocamera e controllo che tutto sia ok
                // Se non esiste o non è disponibile sul device la fotocamera o il gps, esco.
                if (!await CrossMedia.Current.Initialize().ConfigureAwait(false)
                    || !CrossMedia.Current.IsCameraAvailable
                    || !CrossMedia.Current.IsTakePhotoSupported
                    || !CrossGeolocator.IsSupported)
                {
                    await this.DisplayAlert("Error", "No camera or GPS available :(", "Ok").ConfigureAwait(false);
                    return;
                }

                // Disabilito mentre faccio il necessario
                // ButtonGetPhoto.IsEnabled = false;

                // Provo quindi a prendere la posizione corrente
                // sicuramente ho i permessi perchè ho già controllato
                var locator = CrossGeolocator.Current;

                // Chiedo di essere il più accurato possibile
                locator.DesiredAccuracy = 1;

                // Prendo la posizione attuale, comprensiva di l'heading 
                // Che è la direzione verso la quale l'utente è orientato, grazie all'accelerometro
                _currentPosition = await locator.GetPositionAsync(timeout: TimeSpan.FromSeconds(10000), token: null, includeHeading: true)
                    .ConfigureAwait(false);

                if (_currentPosition == null)
                {
                    await this.DisplayAlert("Error", "Found null gps :(, please try again when available", "Ok").ConfigureAwait(false);
                    return;
                }

                // Presa la posizione, ottengo anche l'indirizzo
                var possibleAddressesList = await locator.GetAddressesForPositionAsync(_currentPosition, MY_MAP_KEY)
                    .ConfigureAwait(false);

                // Se non ho ottenuto alcun indirizzo
                if (possibleAddressesList?.Any() != true)
                {
                    await this.DisplayAlert("Error", "Unable to find address", "Ok").ConfigureAwait(false);
                    return;
                }

                // Prendo solo il primo, il più significativo
                _currentAddress = possibleAddressesList.FirstOrDefault();

                // Altrimenti Faccio fare una foto all'utente
                var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                {
                    Directory = "CityProblemGeoHelperPhotos",
                    SaveToAlbum = true,
                    CompressionQuality = 50,
                    PhotoSize = PhotoSize.MaxWidthHeight,
                    // La fotocamera da utilizzare è chiaramente quella dietro
                    DefaultCamera = CameraDevice.Rear,
                    Location = new Plugin.Media.Abstractions.Location()
                    {
                        Latitude = _currentPosition.Latitude,
                        Direction = _currentPosition.Heading,
                        Altitude = _currentPosition.Altitude,
                        HorizontalAccuracy = _currentPosition.AltitudeAccuracy,
                        Longitude = _currentPosition.Longitude,
                        Speed = _currentPosition.Speed,
                        Timestamp = new DateTime(_currentPosition.Timestamp.UtcTicks, DateTimeKind.Utc),
                    }
                }).ConfigureAwait(false);

                // Se non ci riesce esco
                if (file == null)
                {
                    ButtonGetPhoto.IsEnabled = true;
                    return;
                }

                // Dico all'utente dove ho salvato l'immagine per chiarezza
                await DisplayAlert("File Location", file.Path, "OK").ConfigureAwait(false);

                var stream = file.GetStream();

                // Imposto l'immagine per mostrarla all'utente
                image.Source = ImageSource.FromStream(() => stream);

                // Mostro all'utente l'indirizzo
                LabelAddress.Text = $"Address: Thoroughfare = {_currentAddress.Thoroughfare}\nLocality = {_currentAddress.Locality}\nCountryCode = {_currentAddress.CountryCode}\nCountryName = {_currentAddress.CountryName}\nPostalCode = {_currentAddress.PostalCode}\nSubThoroughfare = {_currentAddress.SubThoroughfare}";

                // Riattivo finalmente il bottone
                ButtonGetPhoto.IsEnabled = true;
            }
            catch (Exception ex)
            {
                // Loggo qualsiasi errore accada
                await this.DisplayAlertException(ex).ConfigureAwait(false);
            }

            // TODO -oFBE: Crea textbox per commenti(max 500 caratteri), tasto per invio email, scrivi testo per email con alla fine Commenti: e i commenti se presenti, e calcola se è possibile mandare direttamente la mail.

            // Se è possibile mandarla in automatico appare direttamente invia al comune di "nomeComune" nel bottone, altrimenti solo "invia" e viene creata una mail senza mittente
        }
    }
}
