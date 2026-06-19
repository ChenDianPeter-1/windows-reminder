using System.Windows;
using Microsoft.Toolkit.Uwp.Notifications;

namespace WindowsReminder.Poc;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ToastNotificationManagerCompat.OnActivated += OnToastActivated;
        SendTestToast();

        Console.WriteLine("[POC] Toast sent — look for it and click 'Done'");
        StatusText.Text = "Toast sent! Click 'Done' button on the notification.";
    }

    private void OnToastActivated(ToastNotificationActivatedEventArgsCompat args)
    {
        Dispatcher.Invoke(() =>
        {
            var argStr = args.Argument ?? "";
            Console.WriteLine($"[POC] Activated — args: '{argStr}'");

            if (argStr.Contains("action=done"))
            {
                Console.WriteLine("[POC] PASS: Done callback received!");
                ResultText.Text = "✅ PASS: Done callback received!";
                ResultText.Foreground = System.Windows.Media.Brushes.Green;
                StatusText.Text = "Callback works. Close this window.";
            }
        });
    }

    private static void SendTestToast()
    {
        new ToastContentBuilder()
            .AddText("Windows Reminder POC")
            .AddText("Click Done to verify callback")
            .AddButton("Done", ToastActivationType.Foreground, "action=done")
            .Show(toast => toast.ExpirationTime = DateTime.Now.AddMinutes(2));
    }
}
