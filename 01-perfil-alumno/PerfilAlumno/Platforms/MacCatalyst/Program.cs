using ObjCRuntime;
using UIKit;

namespace PerfilAlumno;

public class Program
{
    // Punto de entrada de la app en Mac Catalyst.
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
