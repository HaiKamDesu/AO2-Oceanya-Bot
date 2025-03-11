using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OceanyaClient
{
    /// <summary>
    /// Interaction logic for WaitForm.xaml
    /// </summary>
    public partial class WaitForm : Window
    {
        private static WaitForm _instance;

        private WaitForm(string message, Window owner)
        {
            InitializeComponent();

            Opacity = 0; // Start fully transparent
            Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            lblMessage.Text = message;

            AdjustSizeToMessage(message);
        }

        private void AdjustSizeToMessage(string message)
        {
            var formattedText = new FormattedText(
                message,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(lblMessage.FontFamily, lblMessage.FontStyle, lblMessage.FontWeight, lblMessage.FontStretch),
                lblMessage.FontSize,
                Brushes.White,
                new NumberSubstitution(),
                1);

            Width = formattedText.Width + 20; // Add some padding
            Height = formattedText.Height + 20; // Add some padding
        }

        public static void ShowForm(string message, Window owner)
        {
            if (_instance == null)
            {
                _instance = new WaitForm(message, owner);
                _instance.Show();
                _instance.Activate();

                DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
                _instance.BeginAnimation(Window.OpacityProperty, fadeInAnimation);
            }
            else
            {
                _instance.Owner = owner;
                _instance.lblMessage.Text = message;
                _instance.AdjustSizeToMessage(message);
            }
        }

        public static void CloseForm()
        {
            if (_instance != null)
            {
                DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.5));
                fadeOutAnimation.Completed += (s, e) => { _instance.Close(); _instance = null; };
                _instance.BeginAnimation(Window.OpacityProperty, fadeOutAnimation);
            }
        }
    }
}
