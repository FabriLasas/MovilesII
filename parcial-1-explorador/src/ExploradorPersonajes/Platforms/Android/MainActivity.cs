using Android.App;
using Android.Content.PM;
using Android.OS;

namespace ExploradorPersonajes;

// ScreenOrientation = SensorPortrait: la app se usa en vertical (en cualquiera
// de los dos sentidos). En horizontal, los controles fijos de la pantalla
// principal (botones, búsqueda y filtros) ocupaban toda la altura y la lista
// de personajes quedaba sin espacio para mostrarse.
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ScreenOrientation = ScreenOrientation.SensorPortrait, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
