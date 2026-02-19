using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Gameoteca.Models
{
    public partial class FolderMapping : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [ObservableProperty]
        private string _folderPath = "";

        [ObservableProperty]
        private string? _plataform;

        [ObservableProperty]
        private Guid? _emulatorId;

        // ✅ IMPORTANTE:
        // System.Text.Json pode trocar a instância da coleção ao desserializar.
        // Então precisamos "re-hook" do CollectionChanged sempre que Extensions for setado.
        private ObservableCollection<string> _extensions = new();

        public ObservableCollection<string> Extensions
        {
            get => _extensions;
            set
            {
                if (ReferenceEquals(_extensions, value)) return;

                if (_extensions != null)
                    _extensions.CollectionChanged -= Extensions_CollectionChanged;

                _extensions = value ?? new ObservableCollection<string>();
                _extensions.CollectionChanged += Extensions_CollectionChanged;

                OnPropertyChanged(nameof(Extensions));
                OnPropertyChanged(nameof(ExtensionsText));
            }
        }

        public FolderMapping()
        {
            _extensions.CollectionChanged += Extensions_CollectionChanged;
        }

        private void Extensions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ExtensionsText));
        }

        // Texto exibido/editável no grid
        public string ExtensionsText
        {
            get => string.Join("; ", Extensions);
            set
            {
                // evita loop de eventos
                Extensions.CollectionChanged -= Extensions_CollectionChanged;
                try
                {
                    Extensions.Clear();

                    var parts = (value ?? "")
                        .Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(NormalizeExt)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase);

                    foreach (var p in parts)
                        Extensions.Add(p);
                }
                finally
                {
                    Extensions.CollectionChanged += Extensions_CollectionChanged;
                    OnPropertyChanged(nameof(ExtensionsText));
                }
            }
        }

        public void AddExtension(string ext)
        {
            var n = NormalizeExt(ext);
            if (string.IsNullOrWhiteSpace(n)) return;

            if (!Extensions.Any(x => string.Equals(x, n, StringComparison.OrdinalIgnoreCase)))
                Extensions.Add(n);

            OnPropertyChanged(nameof(ExtensionsText));
        }

        private static string NormalizeExt(string ext)
        {
            ext = (ext ?? "").Trim();
            if (ext.Length == 0) return "";

            if (!ext.StartsWith("."))
                ext = "." + ext;

            return ext.ToLowerInvariant();
        }
    }
}
