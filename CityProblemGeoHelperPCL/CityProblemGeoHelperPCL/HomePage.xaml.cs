using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Plugin.Messaging;
using Plugin.Permissions.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CityProblemGeoHelperPCL
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : TabbedPage
    {
        #region Private Fields

        /// <summary>
        /// Map-Key necessaria alle app UWP per Bing.
        /// </summary>
        private const string MY_MAP_KEY = "RJHqIE53Onrqons5CNOx~FrDr3XhjDTyEXEjng-CRoA~Aj69MhNManYUKxo6QcwZ0wmXBtyva0zwuHB04rFYAPf7qqGJ5cHb03RCDw1jIW8l";

        /// <summary>
        /// Dizionario che stabilisce, attraverso il cap,
        /// </summary>
        private readonly Dictionary<int, string> _dizionarioComuni =
            new Dictionary<int, string>
            {
                // { 47841, "info@cattolica.net" }
            };

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
        /// Tengo in memoria l'address salvato
        /// </summary>
        private Address _currentAddress;

        /// <summary>
        /// Tengo in memoria la posizione comprensiva di heading(direzione) quando la funzione è supportata
        /// </summary>
        private Position _currentPosition;

        private string _savedImageFilePath;

        #endregion Private Fields

        #region Public Constructors

        public HomePage()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Private Methods

        /// <summary>
        /// Click sul pulsante che permette di prendere la foto geolocalizzata
        /// </summary>
        /// <param name="sender">
        /// </param>
        /// <param name="e">
        /// </param>
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

                // Inizializzo ora la fotocamera e controllo che tutto sia ok: se non esiste o non è
                // disponibile sul device la fotocamera, il gps o il permesso di mandare email con
                // allegati, esco.
                if (!await CrossMedia.Current.Initialize().ConfigureAwait(false)
                    || !CrossMedia.Current.IsCameraAvailable
                    || !CrossMedia.Current.IsTakePhotoSupported
                    || !CrossGeolocator.IsSupported
                    || !CrossMessaging.Current.EmailMessenger.CanSendEmailAttachments)
                {
                    this.DisplayAlertOnMain("Error", "No camera or GPS available :(", "Ok");
                    return;
                }

                // Disabilito mentre faccio il necessario
                Device.BeginInvokeOnMainThread(() =>
                {
                    ButtonGetPhoto.IsEnabled = false;
                });

                // Provo quindi a prendere la posizione corrente sicuramente ho i permessi perchè ho
                // già controllato
                var locator = CrossGeolocator.Current;

                // Chiedo di essere il più accurato possibile
                locator.DesiredAccuracy = 50;

                // Prendo la posizione attuale, comprensiva di l'heading Che è la direzione verso la
                // quale l'utente è orientato, grazie all'accelerometro
                _currentPosition = Task.Run(() =>
                {
                    return CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(4), null, true);
                }).Result;

                if (_currentPosition == null)
                {
                    this.DisplayAlertOnMain("Error", "Found null gps :(, please try again when available", "Ok");
                    return;
                }

                // Presa la posizione, ottengo anche l'indirizzo
                IEnumerable<Address> possibleAddressesList = Task.Run(() =>
                {
                    return locator.GetAddressesForPositionAsync(_currentPosition, MY_MAP_KEY);
                }).Result;

                // Se non ho ottenuto alcun indirizzo
                if (possibleAddressesList?.Any() != true)
                {
                    this.DisplayAlertOnMain("Error", "Unable to find address", "Ok");
                    return;
                }

                // Prendo solo il primo, il più significativo
                _currentAddress = possibleAddressesList.FirstOrDefault();

                // Altrimenti Faccio fare una foto all'utente
                if (Device.RuntimePlatform == Device.UWP)
                {
                    // Se mi trovo su UWP devo inizializzare un nuovo thread dal main device,
                    // altrimenti non mi permette di continuare
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        await this.SaveAndRenderPhotoAsync();
                    });
                }
                else
                {
                    await this.SaveAndRenderPhotoAsync();
                }
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
        /// <param name="sender">
        /// </param>
        /// <param name="e">
        /// </param>
        private void ButtonSendEmail_Clicked(object sender, EventArgs e)
        {
            // Se possibile calcolo quale sia la mail prendendola da quelle salvate, Al momento ho
            // solo quella di cattolica, se non trovo quella da usare la chiedo all'utente
            if (String.IsNullOrWhiteSpace(EntryEmail.Text) || !Utils.CheckValidEmail(EntryEmail.Text))
            {
                this.DisplayAlertOnMain("Town hall email not found or invalid", "Please enter it and send again", "Ok");
            }
            else
            {
                try
                {
                    // Genero il testo
                    var sb = new StringBuilder().AppendLine("Hello there!")
                        .AppendLine("I found a problem, it's here:")
                        .AppendLine(LabelAddress.Text)
                        .AppendLine()
                        .AppendLine("Latitude:")
                        .AppendLine(_currentPosition.Latitude.ToString())
                        .AppendLine("Longitude:")
                        .AppendLine(_currentPosition.Longitude.ToString())
                        .AppendLine()
                        .AppendLine("Google Maps Link:")
                        .Append("https://www.google.com/maps/search/?api=1&query=")
                        .Append(Utils.FormatCoordinate(_currentPosition.Latitude))
                        .Append(",")
                        .AppendLine(Utils.FormatCoordinate(_currentPosition.Longitude));

                    // Se non sono nulli o non si è lasciato il placeholder, inserisco i commenti
                    if (!String.IsNullOrWhiteSpace(EditorComments.Text) && !EditorComments.Text.Equals("Write here your comments..."))
                    {
                        sb.AppendLine()
                            .AppendLine("Comments:")
                            .AppendLine(EditorComments.Text);
                    }

                    // Prendo il messenger, ho già controllato che l'utente abbia i permessi se siamo qui
                    var emailMessenger = CrossMessaging.Current.EmailMessenger;

                    // Creo la mail con l'allegato
                    var email = new EmailMessageBuilder()
                      .To(EntryEmail.Text)
                      .Subject($"A problem has been found! {Guid.NewGuid()}")
                      .Body(sb.ToString())
                      .WithAttachment(_savedImageFilePath, "image/jpeg")
                      .Build();

                    emailMessenger.SendEmail(email);
                }
                catch (Exception ex)
                {
                    this.DisplayAlertExceptionOnMain(ex);
                }
            }
        }

        /// <summary>
        /// Permette di salvare il file geolocalizzato e di mostrare la foto all'utente
        /// </summary>
        /// <returns>
        /// </returns>
        private async Task SaveAndRenderPhotoAsync()
        {
            // Su android continuo normalmente
            MediaFile file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                PhotoSize = PhotoSize.Medium,
                Directory = "CityProblemGeoHelperPhotos",
                SaveToAlbum = true,
                CompressionQuality = 70,
                Location = new Location()
                {
                    Latitude = _currentPosition.Latitude,
                    Direction = _currentPosition.Heading,
                    Altitude = _currentPosition.Altitude,
                    HorizontalAccuracy = _currentPosition.AltitudeAccuracy,
                    Longitude = _currentPosition.Longitude,
                    Speed = _currentPosition.Speed,
                    Timestamp = new DateTime(_currentPosition.Timestamp.UtcTicks, DateTimeKind.Utc),
                },
                AllowCropping = false,
                DefaultCamera = CameraDevice.Rear,
                Name = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-dd"),
                SaveMetaData = true
            });

            // Se non ci riesce esco
            if (file == null)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ButtonGetPhoto.IsEnabled = true;
                });
                return;
            }

            // Dico all'utente dove ho salvato l'immagine per chiarezza
            this.DisplayAlertOnMain("File Location", file.Path, "OK");

            // Imposta il path globale che ci servirà per aggiungere l'attachment
            _savedImageFilePath = file.Path;

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

                // Attivo il bottone per poi mandare la mail
                ButtonSendEmail.IsEnabled = true;
                ButtonSendEmail.IsVisible = true;

                // Se possibile calcolo quale sia la mail prendendola da quelle salvate Al momento ho
                // solo quella di cattolica, se non trovo quella da usare la chiedo all'utente
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
            return;
        }

        #endregion Private Methods
    }
}