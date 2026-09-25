# Perfil del Alumno — App .NET MAUI con patrón MVVM

Trabajo práctico: aplicación que muestra un perfil editable implementando MVVM
completo, con separación clara entre Model, ViewModel y View.

## Cómo abrirlo

1. Abrir `PerfilAlumno.sln` con Visual Studio 2026 (carga de trabajo
   **Desarrollo de .NET Multi-platform App UI**).
2. Restaurar paquetes NuGet (Visual Studio lo hace solo al abrir).
3. Elegir el destino (Windows Machine, emulador de Android, etc.) y ejecutar con F5.

> Requiere .NET 10. Para compilar iOS / Mac Catalyst hace falta una Mac.

## Estructura

```
PerfilAlumno/
├── Models/
│   └── UserProfile.cs          → datos puros del perfil
├── ViewModels/
│   └── ProfileViewModel.cs     → INotifyPropertyChanged, Command y validaciones
├── Views/
│   ├── MainPage.xaml           → UI con bindings TwoWay
│   └── MainPage.xaml.cs        → solo asigna el BindingContext
├── Resources/                  → imágenes, íconos y estilos
└── Platforms/                  → código específico de Android, iOS, Mac y Windows
```

## Cómo funciona

- Los campos de edición (`Entry` de nombre, `Entry` de edad y `Editor` de
  descripción) usan **binding TwoWay** contra propiedades del ViewModel.
- El botón **Guardar** ejecuta un `Command` que copia los datos editados al
  Model y notifica a la vista, por lo que el bloque superior del perfil se
  actualiza al instante.
- El `CanExecute` del Command deshabilita el botón mientras los datos no sean
  válidos, y además se muestra un mensaje de error explicando el problema.

## Requisitos técnicos cubiertos

| Requisito | Dónde está resuelto |
|---|---|
| MVVM completo | carpetas `Models` / `ViewModels` / `Views` |
| INotifyPropertyChanged | `ProfileViewModel` + helper genérico `SetProperty` |
| Command | `GuardarCommand` con `CanExecute` y `ChangeCanExecute` |
| Binding TwoWay | los dos `Entry` y el `Editor` de `MainPage.xaml` |
| Validación básica | nombre no vacío + edad numérica entre 1 y 119 |
| Diseño responsive | `ScrollView` + `VerticalStackLayout` + `Grid` |

## Cambiar la foto de perfil

En `Models/UserProfile.cs`:

- Imagen local: dejar un archivo en `Resources/Images` y poner su nombre
  (los `.svg` se referencian con extensión `.png`).
- Imagen remota: `ImagenUrl = "https://misitio.com/foto.png";`
