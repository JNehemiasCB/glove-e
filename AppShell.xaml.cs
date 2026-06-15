using glove_e.Services;

namespace glove_e
{
    public partial class AppShell : Shell
    {
        private readonly ISettingsService _settings;
        private bool _checked;

        public AppShell(ISettingsService settings)
        {
            InitializeComponent();
            _settings = settings;
            Loaded += OnShellLoaded;
        }

        /// <summary>
        /// Si la app ya fue configurada, salta el onboarding
        /// y va directo a las pestañas principales.
        /// </summary>
        private async void OnShellLoaded(object? sender, EventArgs e)
        {
            if (_checked) return;
            _checked = true;

            if (_settings.IsConfigured)
                await GoToAsync("//inicio");
            // si no está configurada, se queda en "onboarding" (primer elemento del Shell)
        }
    }
}
