using System.Windows;
using Gameoteca.ViewModels;

namespace Gameoteca
{
    public partial class MainWindow : Window
    {
        public MainViewModel VM { get; } = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = VM;

            Loaded += async (_, __) => await VM.InitAsync();
        }
    }
}
