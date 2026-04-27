using System;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using FluentValidation;
using TravelAgency.Core.Models.Users.Access;
using TravelAgency.Core.Services;
using TravelAgency.Core.Validators;
using TravelAgency.WPF.Commands;
using TravelAgency.WPF.Services;
using TravelAgency.WPF.Views;

namespace TravelAgency.WPF.ViewModels
{
    public class ForgotPasswordViewModel : ViewModelBase, IDisposable
    {
        private readonly Window _window;
        private readonly IPasswordResetFlow _passwordResetFlow;

        private string _emailOrUsername = "";
        private string _otpCode = "";
        private string _newPassword = "";
        private string _confirmPassword = "";
        private string _errorMessage = "";
        private bool _isCodeSent;

        public string EmailOrUsername
        {
            get => _emailOrUsername;
            set => Set(ref _emailOrUsername, value);
        }

        public string OtpCode
        {
            get => _otpCode;
            set => Set(ref _otpCode, value);
        }

        public string NewPassword
        {
            get => _newPassword;
            set => Set(ref _newPassword, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => Set(ref _confirmPassword, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => Set(ref _errorMessage, value);
        }

        public bool IsCodeSent
        {
            get => _isCodeSent;
            set => Set(ref _isCodeSent, value);
        }

        public ICommand SendCodeCommand { get; }
        public ICommand ConfirmResetCommand { get; }
        public ICommand CloseCommand { get; }

        public ForgotPasswordViewModel(Window window)
        {
            _window = window;
            _passwordResetFlow = PasswordResetFlowFactory.Create();

            SendCodeCommand = new RelayCommand(SendCode);
            ConfirmResetCommand = new RelayCommand(ConfirmReset);
            CloseCommand = new RelayCommand(() => _window.Close());
        }

        public void Dispose()
        {
            if (_passwordResetFlow is IDisposable d)
                d.Dispose();
        }

        private async void SendCode()
        {
            ErrorMessage = "";
            IsCodeSent = false;

            try
            {
                if (string.IsNullOrWhiteSpace(EmailOrUsername))
                {
                    ErrorMessage = "Email is required.";
                    return;
                }

                var key = (EmailOrUsername ?? "").Trim();
                var isEmail = key.Contains("@");
                if (!isEmail)
                {
                    ErrorMessage = "Please enter a valid email address.";
                    return;
                }
                var sent = await _passwordResetFlow.RequestResetAsync(key);

                var dialog = new MessageDialogWindow(
                    "Check your email",
                    sent
                        ? "A reset link was sent to your email. Click it to set a new password."
                        : "If an account exists for this email, a reset link was sent. If you do not see it, wait a minute before requesting again.")
                {
                    Owner = _window
                };
                dialog.ShowDialog();

                // For email-link flow, user resets password in browser.
                // Close this dialog and return to login window.
                if (sent)
                {
                    _window.Close();
                    return;
                }

                // SMS/OTP flow disabled in UI (email-only).
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (HttpRequestException)
            {
                ErrorMessage = "Could not reach the password reset service. Start the API or check PasswordReset.ApiBaseUrl.";
            }
            catch (Exception)
            {
                ErrorMessage = "Could not send reset link. Please try again.";
            }
        }

        private async void ConfirmReset()
        {
            ErrorMessage = "";

            var request = new ResetPasswordRequest
            {
                Username = EmailOrUsername?.Trim() ?? "",
                NewPassword = NewPassword ?? "",
                ConfirmPassword = ConfirmPassword ?? ""
            };

            try
            {
                var validator = new ResetPasswordRequestValidator();
                validator.ValidateAndThrow(request);

                if (string.IsNullOrWhiteSpace(OtpCode))
                {
                    ErrorMessage = "Verification code is required.";
                    return;
                }

                await _passwordResetFlow.ConfirmResetAsync(
                    EmailOrUsername ?? string.Empty,
                    OtpCode ?? string.Empty,
                    NewPassword ?? string.Empty);

                var dialog = new MessageDialogWindow(
                    "Reset password",
                    "Password updated successfully. You can now sign in with your new password.")
                {
                    Owner = _window
                };
                dialog.ShowDialog();

                _window.Close();
            }
            catch (ValidationException ex)
            {
                ErrorMessage = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input.";
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception)
            {
                ErrorMessage = "Invalid code or expired request.";
            }
        }
    }
}

