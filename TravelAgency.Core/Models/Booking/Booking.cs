using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.Users;

namespace TravelAgency.Core.Models.Booking
{
    public class Booking : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.Now;

        public Client? Client { get; set; }

        public TripPackage? TripPackage { get; set; }

        public BookingStatus? Status { get; private set; }

        public string StatusName => Status?.Name ?? "";

        public List<string> SelectedExtras { get; set; } = new();

        public double BasePrice { get; set; }

        public double TotalPrice { get; set; }

        private bool _isBeingRemoved;
        public bool IsBeingRemoved
        {
            get => _isBeingRemoved;
            set
            {
                if (_isBeingRemoved != value)
                {
                    _isBeingRemoved = value;
                    OnPropertyChanged();
                }
            }
        }

        public void SubmitRequest()
        {
            Status = new BookingStatus { Name = "Pending" };
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusName));
        }

        public void ConfirmBooking()
        {
            Status = new BookingStatus { Name = "Confirmed" };
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusName));
        }

        public void RejectBooking()
        {
            Status = new BookingStatus { Name = "Rejected" };
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusName));
        }

        public void CancelBooking()
        {
            Status = new BookingStatus { Name = "Cancelled" };
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusName));
        }

        public void ChangeStatus(BookingStatus status)
        {
            Status = status;
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusName));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}