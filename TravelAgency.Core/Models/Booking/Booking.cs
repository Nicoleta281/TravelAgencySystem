using System;
using System.Collections.Generic;
using System.ComponentModel;
using TravelAgency.Core.Models.TripPkg.Package;
using TravelAgency.Core.Models.Users;
using TravelAgency.Core.Patterns.State;

namespace TravelAgency.Core.Models.Booking
{
    public class Booking : INotifyPropertyChanged
    {
        private IBookingState _state = new PendingBookingState();

        public int Id { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.Now;

        public Client? Client { get; set; }

        public TripPackage? TripPackage { get; set; }

        public BookingStatus? Status { get; private set; } = new BookingStatus { Name = "Pending" };

        public string StatusName => _state.Name;

        public List<string> SelectedExtras { get; set; } = new();

        public double BasePrice { get; set; }

        public double TotalPrice { get; set; }

        public bool IsBeingRemoved { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void SetState(IBookingState state)
        {
            _state = state;
            Status = new BookingStatus { Name = state.Name };
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StatusName));
        }

        public void SubmitRequest()
        {
            SetState(new PendingBookingState());
        }

        public void ConfirmBooking()
        {
            _state.Confirm(this);
        }

        public void RejectBooking()
        {
            _state.Reject(this);
        }

        public void CancelBooking()
        {
            _state.Cancel(this);
        }

        public void RestoreStateFromStatusName(string? statusName)
        {
            switch (statusName)
            {
                case "Confirmed":
                    SetState(new ConfirmedBookingState());
                    break;
                case "Rejected":
                    SetState(new RejectedBookingState());
                    break;
                case "Cancelled":
                    SetState(new CancelledBookingState());
                    break;
                default:
                    SetState(new PendingBookingState());
                    break;
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}