using Plugin.Geolocator;
using Plugin.Geolocator.Abstractions;
using Plugin.Permissions.Abstractions;
using System;
using System.Collections.ObjectModel;
using System.Linq;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace GeolocatorSample
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HomePage : TabbedPage
    {
        #region Private Fields

        private int _count;
        private Position _savedPosition;
        private bool _tracking;

        #endregion Private Fields

        #region Public Constructors

        public HomePage()
        {
            // Inizializza il component HomePage
            InitializeComponent();
            ListViewPositions.ItemsSource = Positions;
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<Position> Positions { get; } = new ObservableCollection<Position>();

        #endregion Public Properties

        #region Private Methods

        /// <summary>
        /// Prende la posizione 
        /// </summary>
        private async void ButtonAddressForPosition_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Se non c'è una posizione ritorna, ne serve sempre una, altrimenti non c'è connessione
                // TODO -oFBE: Implementa avviso di mancata connessione se necessario
                if (_savedPosition == null)
                {
                    return;
                }

                // Controlla i permessi, se non presenti li richiede
                var hasPermission = await Utils.CheckPermissions(Permission.Location).ConfigureAwait(false);
                if (!hasPermission)
                {
                    return;
                }

                // Disabilita il bottone per l'indirizzo mentre computo il necessario
                ButtonAddressForPosition.IsEnabled = false;

                // Per chiarezza metto il plugin in una variabile
                var locator = CrossGeolocator.Current;
                var address = await locator.GetAddressesForPositionAsync(_savedPosition, 
                    "RJHqIE53Onrqons5CNOx~FrDr3XhjDTyEXEjng-CRoA~Aj69MhNManYUKxo6QcwZ0wmXBtyva0zwuHB04rFYAPf7qqGJ5cHb03RCDw1jIW8l")
                    .ConfigureAwait(false);

                // Se non ho ottenuto alcun indirizzo
                if (address?.Any() != true)
                {
                    LabelAddress.Text = "Unable to find address";
                }

                // Prendo solo il primo, il più significativo
                var a = address.FirstOrDefault();

                // Per ora lo scrivo, poi lo aggiungerò alla mail insieme alla foto
                LabelAddress.Text = $"Address: Thoroughfare = {a.Thoroughfare}\nLocality = {a.Locality}\nCountryCode = {a.CountryCode}\nCountryName = {a.CountryName}\nPostalCode = {a.PostalCode}\nSubThoroughfare = {a.SubThoroughfare}";
            }
            catch (Exception ex)
            {
                await Utils.DisplayAlertException(this, ex).ConfigureAwait(false);
            }
            finally
            {
                // Infine lo riattivo
                ButtonAddressForPosition.IsEnabled = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private async void ButtonGetCurrentLocation_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Check dei permessi
                var hasPermission = await Utils.CheckPermissions(Permission.Location).ConfigureAwait(false);
                if (!hasPermission)
                {
                    return;
                }

                // Distattivo temporaneamente il pulstante mentre computo
                ButtonGetCurrentLocation.IsEnabled = false;
                
                // Prendo la location
                var locator = CrossGeolocator.Current;
                locator.DesiredAccuracy = 0;
                labelGPS.Text = "Getting gps...";

                var position = await locator.GetPositionAsync(timeout: TimeSpan.FromSeconds(10000), token: null, includeHeading: true)
                    .ConfigureAwait(false);

                if (position == null)
                {
                    labelGPS.Text = "null gps :(";
                    return;
                }
                _savedPosition = position;
                ButtonAddressForPosition.IsEnabled = true;
                labelGPS.Text = string.Format("Time: {0} \nLat: {1} \nLong: {2} \nAltitude: {3} \nAltitude Accuracy: {4} \nAccuracy: {5} \nHeading: {6} \nSpeed: {7}",
                    position.Timestamp, position.Latitude, position.Longitude,
                    position.Altitude, position.AltitudeAccuracy, position.Accuracy, position.Heading, position.Speed);
            }
            catch (Exception ex)
            {
                await Utils.DisplayAlertException(this, ex).ConfigureAwait(false);
            }
            finally
            {
                ButtonGetCurrentLocation.IsEnabled = true;
            }
        }

        private async void ButtonTrack_Clicked(object sender, EventArgs e)
        {
            try
            {
                var hasPermission = await Utils.CheckPermissions(Permission.Location).ConfigureAwait(false);
                if (!hasPermission)
                {
                    return;
                }

                if (_tracking)
                {
                    CrossGeolocator.Current.PositionChanged -= CrossGeolocator_Current_PositionChanged;
                    CrossGeolocator.Current.PositionError -= CrossGeolocator_Current_PositionError;
                }
                else
                {
                    CrossGeolocator.Current.PositionChanged += CrossGeolocator_Current_PositionChanged;
                    CrossGeolocator.Current.PositionError += CrossGeolocator_Current_PositionError;
                }

                if (CrossGeolocator.Current.IsListening)
                {
                    await CrossGeolocator.Current.StopListeningAsync().ConfigureAwait(false);
                    labelGPSTrack.Text = "Stopped tracking";
                    ButtonTrack.Text = "Start Tracking";
                    _tracking = false;
                    _count = 0;
                }
                else
                {
                    Positions.Clear();
                    if (await CrossGeolocator.Current.StartListeningAsync(TimeSpan.FromSeconds(TrackTimeout.Value), TrackDistance.Value,
                        TrackIncludeHeading.IsToggled, new ListenerSettings
                        {
                            ActivityType = (ActivityType)ActivityTypePicker.SelectedIndex,
                            AllowBackgroundUpdates = AllowBackgroundUpdates.IsToggled,
                            DeferLocationUpdates = DeferUpdates.IsToggled,
                            DeferralDistanceMeters = DeferalDistance.Value,
                            DeferralTime = TimeSpan.FromSeconds(DeferalTIme.Value),
                            ListenForSignificantChanges = ListenForSig.IsToggled,
                            PauseLocationUpdatesAutomatically = PauseLocation.IsToggled
                        }).ConfigureAwait(false))
                    {
                        labelGPSTrack.Text = "Started tracking";
                        ButtonTrack.Text = "Stop Tracking";
                        _tracking = true;
                    }
                }
            }
            catch (Exception ex)
            {
                await Utils.DisplayAlertException(this, ex).ConfigureAwait(false);
            }
        }

        private void CrossGeolocator_Current_PositionChanged(object sender, PositionEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var position = e.Position;
                Positions.Add(position);
                _count++;
                LabelCount.Text = $"{_count} updates";
                labelGPSTrack.Text = string.Format("Time: {0} \nLat: {1} \nLong: {2} \nAltitude: {3} \nAltitude Accuracy: {4} \nAccuracy: {5} \nHeading: {6} \nSpeed: {7}",
                    position.Timestamp, position.Latitude, position.Longitude,
                    position.Altitude, position.AltitudeAccuracy, position.Accuracy, position.Heading, position.Speed);
            });
        }

        private void CrossGeolocator_Current_PositionError(object sender, PositionErrorEventArgs e)
        {
            labelGPSTrack.Text = "Location error: " + e.Error.ToString();
        }

        #endregion Private Methods
    }
}