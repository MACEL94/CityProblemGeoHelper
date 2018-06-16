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
using System.Threading.Tasks;
using Xamarin.Essentials;
using System.Text;

namespace CityProblemGeoHelperPCL
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
                Permission.Location,
                Permission.Storage,
            };

        /// <summary>
        /// Dizionario che stabilisce, attraverso il cap, 
        /// </summary>
        private Dictionary<int, string> _dizionarioComuni =
            new Dictionary<int, string>
            {
                //    { 47841, "info@cattolica.net" }
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
        /// quando la funzione è supportata
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
                    this.DisplayAlertOnMain("Error", "No camera or GPS available :(", "Ok");
                    return;
                }

                // Disabilito mentre faccio il necessario
                // ButtonGetPhoto.IsEnabled = false;

                // Provo quindi a prendere la posizione corrente
                // sicuramente ho i permessi perchè ho già controllato
                var locator = CrossGeolocator.Current;

                // Chiedo di essere il più accurato possibile
                locator.DesiredAccuracy = 50;

                // Prendo la posizione attuale, comprensiva di l'heading 
                // Che è la direzione verso la quale l'utente è orientato, grazie all'accelerometro
                _currentPosition = Task.Run(() => CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(2), null, true)).Result;

                if (_currentPosition == null)
                {
                    this.DisplayAlertOnMain("Error", "Found null gps :(, please try again when available", "Ok");
                    return;
                }

                // Presa la posizione, ottengo anche l'indirizzo
                var possibleAddressesList = await locator.GetAddressesForPositionAsync(_currentPosition, MY_MAP_KEY)
                    .ConfigureAwait(false);

                // Se non ho ottenuto alcun indirizzo
                if (possibleAddressesList?.Any() != true)
                {
                    this.DisplayAlertOnMain("Error", "Unable to find address", "Ok");
                    return;
                }

                // Prendo solo il primo, il più significativo
                _currentAddress = possibleAddressesList.FirstOrDefault();

                // Altrimenti Faccio fare una foto all'utente
                var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
                {
                    Directory = "CityProblemGeoHelperPhotos",
                    SaveToAlbum = true,
                    CompressionQuality = 70,
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
                this.DisplayAlertOnMain("File Location", file.Path, "OK");

                // Prende lo stream dal file
                var stream = file.GetStream();

                // Prendo il main thread per impostare la nuova UI
                Device.BeginInvokeOnMainThread(() =>
                {
                    // Imposto l'immagine per mostrarla all'utente
                    ImageContent.Source = ImageSource.FromStream(() => stream);

                    // Mostro all'utente l'indirizzo
                    LabelAddress.Text = $"Address = { _currentAddress.Thoroughfare }{ Environment.NewLine }Locality = { _currentAddress.Locality }{ Environment.NewLine }CountryCode = { _currentAddress.CountryCode }{ Environment.NewLine }CountryName = { _currentAddress.CountryName }{ Environment.NewLine }PostalCode = { _currentAddress.PostalCode }{ Environment.NewLine }SubThoroughfare = { _currentAddress.SubThoroughfare }";

                    // Attivo finalmente l'editor per i commenti
                    EditorComments.IsEnabled = true;
                    EditorComments.IsVisible = true;

                    // Riattivo il bottone per prendere un'altra foto, in caso si sia sbagliato
                    ButtonGetPhoto.IsEnabled = true;

                    // Attivo l'Entry
                    EntryEmail.IsVisible = true;
                    EntryEmail.IsEnabled = true;

                    // Se possibile calcolo quale sia la mail prendendola da quelle salvate
                    // Al momento ho solo quella di cattolica, se non trovo quella da usare la chiedo all'utente
                    if (!_dizionarioComuni.TryGetValue(Int32.Parse(_currentAddress.PostalCode), out string email))
                    {
                        this.DisplayAlertOnMain("Town hall email not found", "Please enter it", "Ok");
                    }
                    else
                    {
                        // Se la trovo la inserisco
                        EntryEmail.Text = email;
                    }
                });
            }
            catch (Exception ex)
            {
                // Loggo qualsiasi errore accada
                this.DisplayAlertExceptionOnMain(ex);
            }
        }

        /// <summary>
        /// Controllo che tutto sia ok, in caso mando finalmente la mail
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ButtonSendEmail_Clicked(object sender, EventArgs e)
        {
            // Se possibile calcolo quale sia la mail prendendola da quelle salvate, 
            // Al momento ho solo quella di cattolica, se non trovo quella da usare la chiedo all'utente
            if (String.IsNullOrWhiteSpace(EntryEmail.Text) || !Utils.CheckValidEmail(EntryEmail.Text))
            {
                this.DisplayAlertOnMain("Town hall email not found or invalid", "Please enter it and send again", "Ok");
            }
            else
            {
                try
                {
                    // Genero il testo
                    var sb = new StringBuilder();

                    sb.AppendLine("Hello there!")
                        .AppendLine("I found a problem, it's here:")
                        .AppendLine(LabelAddress.Text);

                    var email = new EmailMessage
                    {
                        // Genero un numero identificativo così le email sono organizzate anche per il comune
                        Subject = $"A problem has been found! {Guid.NewGuid()}",
                        Body = sb.ToString(),
                        BodyFormat = EmailBodyFormat.PlainText,
                        To = new List<string> { EntryEmail.Text },
                    };

                    await Email.ComposeAsync(email);
                }
                catch (Exception ex)
                {
                    this.DisplayAlertExceptionOnMain(ex);
                }
            }
        }
    }
}
