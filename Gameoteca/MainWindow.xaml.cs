using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        // ✅ Correção: garante que a linha seja selecionada ao clicar com botão direito.
        private void MappingsGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid grid) return;

            // Descobre qual linha/célula foi clicada
            var dep = e.OriginalSource as DependencyObject;
            var row = FindVisualParent<DataGridRow>(dep);
            if (row == null)
                return;

            row.IsSelected = true;
            grid.SelectedItem = row.Item;
            row.Focus();
        }

        private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T typed) return typed;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void MappingsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
