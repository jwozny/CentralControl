using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Unified_Systems.Settings
{
    /// <summary>
    /// Interaction logic for Credentials.xaml
    /// </summary>
    public partial class Credentials : Page
    {
        public Credentials()
        {
            InitializeComponent();
            InitializeSavedInfo();
        }

        public void InitializeSavedInfo()
        {
            ADdomain_TextBox.Text = ActiveDirectory.DomainName;
            ADuser_TextBox.Text = ActiveDirectory.AuthenticatingUsername;
            if (ActiveDirectory.AuthenticatingPasswordExists()) ADpass_TextBox.Text = "********";
        }

        char _passwordChar = '●';
        private char PasswordChar
        {
            get { return _passwordChar; }
            set { _passwordChar = value; }
        }
        private SecureString ADpass_secureString = new SecureString();
        private SecureString ADpass_SecureString
        {
            get { return ADpass_secureString; }
        }
        private void ADpass_TextBox_KeyDown(object sender, KeyEventArgs e)
        {

            if (IsIgnorableKey(e.Key))
            {
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                ADpass_ProcessDelete();
                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                ADpass_ProcessBackspace();
                e.Handled = true;
            }
            else
            {
                ADpass_ProcessNewCharacter(e.Key.ToString().ToCharArray());
            }
        }
        private bool IsIgnorableKey(Key key)
        {
            return key == Key.Escape
                || key == Key.Enter
                || key == Key.RightShift
                || key == Key.LeftShift
                || key == Key.RightAlt
                || key == Key.LeftAlt
                || key == Key.RightCtrl
                || key == Key.LeftCtrl;
        }
        private void ADpass_ProcessDelete()
        {
            if (ADpass_TextBox.SelectionLength > 0)
            {
                ADpass_RemoveSelectedCharacters();
            }
            else if (ADpass_TextBox.SelectionStart < ADpass_TextBox.Text.Length)
            {
                ADpass_secureString.RemoveAt(ADpass_TextBox.SelectionStart);
            }

            ADpass_ResetDisplayCharacters(ADpass_TextBox.SelectionStart);
        }
        private void ADpass_ProcessBackspace()
        {
            if (ADpass_TextBox.SelectionLength > 0)
            {
                ADpass_RemoveSelectedCharacters();
                ADpass_ResetDisplayCharacters(ADpass_TextBox.SelectionStart);
            }
            else if (ADpass_TextBox.SelectionStart > 0)
            {
                ADpass_secureString.RemoveAt(ADpass_TextBox.SelectionStart - 1);
                ADpass_ResetDisplayCharacters(ADpass_TextBox.SelectionStart - 1);
            }
        }
        private void ADpass_ProcessNewCharacter(char[] character)
        {
            if (ADpass_TextBox.SelectionLength > 0)
            {
                ADpass_RemoveSelectedCharacters();
            }

            if (character.Length == 2)
                ADpass_secureString.InsertAt(ADpass_TextBox.SelectionStart, character[1]);
            else
                ADpass_secureString.InsertAt(ADpass_TextBox.SelectionStart, character[0]);
            ADpass_ResetDisplayCharacters(ADpass_TextBox.SelectionStart + 1);
        }
        private void ADpass_RemoveSelectedCharacters()
        {
            for (int i = 0; i < ADpass_TextBox.SelectionLength; i++)
            {
                ADpass_secureString.RemoveAt(ADpass_TextBox.SelectionStart);
            }
        }
        private void ADpass_ResetDisplayCharacters(int caretPosition)
        {
            ADpass_TextBox.Text = new string(_passwordChar, ADpass_secureString.Length);
            ADpass_TextBox.SelectionStart = caretPosition;
        }

        /* Primary Action */
        /// <summary>
        /// Saves any changes made to the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool confirmAction = false;
        private void saveLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            saveLabelButton.Style = defaultMouseDownLabelButtonStyle;
            resultMessage.Visibility = Visibility.Hidden;

            if (confirmAction)
            {
                curtain.Visibility = Visibility.Visible;
                confirmMessage.Content = "Are you sure you want to save changes?";
                confirmYesLabelButton.Content = "Save Changes";
                confirmMessage.Visibility = Visibility.Visible;
                confirmYesLabelButton.Visibility = Visibility.Visible;
                confirmYesLabelButton.IsEnabled = true;
                confirmNoLabelButton.Visibility = Visibility.Visible;
                confirmNoLabelButton.IsEnabled = true;
            }
            else
            {
                if (saveSettings())
                {
                    resultMessage.Content = "Saved Successfully";
                    resultMessage.Visibility = Visibility.Visible;
                }
                saveLabelButton.IsEnabled = true;
            }
        }
        private void saveLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            saveLabelButton.Style = defaultLabelButtonStyle;
        }
        private void saveLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            saveLabelButton.Style = defaultLabelButtonStyle;
        }
        /// <summary>
        /// Warning and confirmation template if needed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void confirmYesLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            curtain.Visibility = Visibility.Hidden;
            confirmMessage.Visibility = Visibility.Hidden;
            confirmYesLabelButton.Visibility = Visibility.Hidden;
            confirmYesLabelButton.IsEnabled = false;
            confirmNoLabelButton.Visibility = Visibility.Hidden;
            confirmNoLabelButton.IsEnabled = false;

            if (saveSettings())
            {
                resultMessage.Content = "Saved Successfully";
                resultMessage.Visibility = Visibility.Visible;
            }
            saveLabelButton.IsEnabled = true;
        }
        private void confirmNoLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            curtain.Visibility = Visibility.Hidden;
            confirmMessage.Visibility = Visibility.Hidden;
            confirmYesLabelButton.Visibility = Visibility.Hidden;
            confirmYesLabelButton.IsEnabled = false;
            confirmNoLabelButton.Visibility = Visibility.Hidden;
            confirmNoLabelButton.IsEnabled = false;

        }
        private void confirmLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style warningConfirmLabelButtonStyle = FindResource("warningConfirmLabelButtonStyle") as Style;
            Style warningCancelLabelButtonStyle = FindResource("warningCancelLabelButtonStyle") as Style;
            confirmYesLabelButton.Style = warningConfirmLabelButtonStyle;
            confirmNoLabelButton.Style = warningCancelLabelButtonStyle;
        }
        private void confirmLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style warningConfirmLabelButtonStyle = FindResource("warningConfirmLabelButtonStyle") as Style;
            Style warningCancelLabelButtonStyle = FindResource("warningCancelLabelButtonStyle") as Style;
            confirmYesLabelButton.Style = warningConfirmLabelButtonStyle;
            confirmNoLabelButton.Style = warningCancelLabelButtonStyle;
        }
        private bool saveSettings()
        {
            try
            {
                ActiveDirectory.DomainName = ADdomain_TextBox.Text;
                ActiveDirectory.AuthenticatingUsername = ADuser_TextBox.Text;
                ActiveDirectory.AuthenticatingPassword = ADpass_SecureString;
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Hide result message with mouse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void resultMessage_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //resultMessage.Visibility = Visibility.Hidden;
        }
    }
}
