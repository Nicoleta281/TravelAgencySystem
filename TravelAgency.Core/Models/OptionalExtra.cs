using System.ComponentModel;

namespace TravelAgency.Core.Models
{
    public class OptionalExtra : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Name { get; set; } = "";
        public double Price { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}