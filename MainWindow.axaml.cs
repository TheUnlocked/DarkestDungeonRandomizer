using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DarkestDungeonRandomizer
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
