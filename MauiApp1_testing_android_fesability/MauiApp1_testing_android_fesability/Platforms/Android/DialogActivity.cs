using Android.App;
using Android.OS;
using Android.Content;
using Android.Widget;
using Android.Views;

namespace MauiApp1_testing_android_fesability
{
    [Activity(Label = "Confirm", Theme = "@style/DialogTheme")]
    public class DialogActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Basic vertical layout
            var layout = new LinearLayout(this) { Orientation = Orientation.Vertical};
            var tv = new TextView(this)
            {
                TextSize = 18f,
                Text = "Do you want to continue into YouTube?"
            };
            layout.AddView(tv);

            var buttonLayout = new LinearLayout(this) { Orientation = Orientation.Horizontal};

            var yesBtn = new Android.Widget.Button(this) { Text = "Yes open youtube" };
            var noBtn = new Android.Widget.Button(this) { Text = "No go away" };

            buttonLayout.AddView(yesBtn);
            buttonLayout.AddView(noBtn);
            layout.AddView(buttonLayout);

            SetContentView(layout);

            yesBtn.Click += (s, e) =>
            {
                // Just finish and let YouTube remain in foreground
                FinishAndRemoveTask();
            };

            noBtn.Click += (s, e) =>
            {
                // Send user to Home, backgrounding YouTube
                Intent intent = new Intent(Intent.ActionMain);
                intent.AddCategory(Intent.CategoryHome);
                intent.SetFlags(ActivityFlags.NewTask);
                StartActivity(intent);

                FinishAndRemoveTask();
            };
        }

        // Prevent back from doing anything surprising
        public override void OnBackPressed()
        {
            // treat as No (send home)
            Intent intent = new Intent(Intent.ActionMain);
            intent.AddCategory(Intent.CategoryHome);
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
            Finish();
        }
    }
}
