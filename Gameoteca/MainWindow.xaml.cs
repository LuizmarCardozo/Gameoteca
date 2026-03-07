using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Gameoteca.ViewModels;

namespace Gameoteca
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.OpenContextMenuRequested += Vm_OpenContextMenuRequested;
            }

            // Foca a aba principal logo ao abrir para não precisar de mouse!
            MainTabControl.Focus();
        }

        private void Vm_OpenContextMenuRequested(object? sender, EventArgs e)
        {
            if (MainTabControl.SelectedIndex == 0 && GamesList.SelectedItem != null)
                OpenMenu(GamesList, GamesList.SelectedItem);
            else if (MainTabControl.SelectedIndex == 1 && EmulatorsList.SelectedItem != null)
                OpenMenu(EmulatorsList, EmulatorsList.SelectedItem);
            else if (MainTabControl.SelectedIndex == 2 && MappingsGrid.SelectedItem != null)
                OpenMenu(MappingsGrid, MappingsGrid.SelectedItem);
        }

        private void OpenMenu(ItemsControl control, object item)
        {
            control.Dispatcher.InvokeAsync(() =>
            {
                if (control is ListBox listBox) listBox.ScrollIntoView(item);
                else if (control is DataGrid dataGrid) dataGrid.ScrollIntoView(item);

                control.Dispatcher.InvokeAsync(() =>
                {
                    var container = control.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                    if (container != null && container.ContextMenu != null)
                    {
                        container.Focus();
                        container.ContextMenu.PlacementTarget = container;
                        container.ContextMenu.Placement = PlacementMode.Center;

                        container.ContextMenu.Opened -= ContextMenu_Opened;
                        container.ContextMenu.Closed -= ContextMenu_Closed;
                        container.ContextMenu.Opened += ContextMenu_Opened;
                        container.ContextMenu.Closed += ContextMenu_Closed;

                        container.ContextMenu.IsOpen = true;
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
            });
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            MainViewModel.IsContextMenuOpen = true;
            Dispatcher.InvokeAsync(() =>
            {
                Gameoteca.ViewModels.KeyboardSimulator.PressKey(Gameoteca.ViewModels.KeyboardSimulator.VK_DOWN);
            }, System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        private void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            MainViewModel.IsContextMenuOpen = false;
        }

        private void MappingsGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is DataGridRow))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep is DataGridRow row)
            {
                row.IsSelected = true;
            }
        }

        private void MappingsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        // AS DUAS FUNÇÕES QUE FALTAVAM PARA OS ERROS SUMIREM:

        // 1. Pular dos botões de cima para as Abas apertando "BAIXO"
        private void HeaderButton_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                var item = MainTabControl.ItemContainerGenerator.ContainerFromIndex(MainTabControl.SelectedIndex) as UIElement;
                item?.Focus();
                e.Handled = true;
            }
        }

        // 2. Pular das Abas para os botões de cima apertando "CIMA"
        private void MainTabControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Up && e.OriginalSource is TabItem)
            {
                if (BtnMap != null)
                {
                    BtnMap.Focus();
                    e.Handled = true;
                }
            }
        }
    }
}