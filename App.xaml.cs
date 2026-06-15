namespace glove_e
{
    public partial class App : Application
    {
        public App(AppShell shell)
        {
            InitializeComponent();

            // El Shell llega por inyección de dependencias (ver MauiProgram.cs)
            MainPage = shell;
        }
    }
}
