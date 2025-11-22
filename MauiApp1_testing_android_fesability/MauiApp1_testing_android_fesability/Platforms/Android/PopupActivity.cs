using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;

namespace MauiApp1_testing_android_fesability
{
    [Activity(Label = "PopupActivity")]
    public class PopupActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set the layout from the XML file
            SetContentView(Resource.Layout.popup_layout);

            // Get references to the buttons
            var title = FindViewById<TextView>(Resource.Id.popup_title);
            var yesButton = FindViewById<Android.Widget.Button>(Resource.Id.popup_yes_button);
            var noButton = FindViewById<Android.Widget.Button>(Resource.Id.popup_no_button);

            title.Text = "Do you really want to open YouTube?";

            // --- Button Click Handlers ---

            // "Yes" button: Just close this popup and let the user continue
            yesButton.Click += (sender, e) =>
            {
                Finish();
            };

            // "No" button: Send the user to the home screen (effectively "closing" YouTube)
            // and then close this popup.
            noButton.Click += (sender, e) =>
            {
                Intent startMain = new Intent(Intent.ActionMain);
                startMain.AddCategory(Intent.CategoryHome);
                startMain.SetFlags(ActivityFlags.NewTask);
                StartActivity(startMain);
                Finish();
            };
        }
    }
}