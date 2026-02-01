using Android.App;
using Android.OS;
using Android.Content;
using Android.Widget;
using Android.Views;

namespace HourGuard
{
    [Activity(Label = "Confirm", Theme = "@style/DialogTheme")]
    public class DialogActivityOLD : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // grabs app name
            string appName = Intent.GetStringExtra("appName");
            if (appName == null)
            {
                appName = "THIS APP";
            }

            // Basic vertical layout
            var layout = new LinearLayout(this) { Orientation = Orientation.Vertical };

            if (true)   // display only if app limit is set
            {
                // Original "continue" question
                var appLimitText = new TextView(this)
                {
                    TextSize = 18f,
                    Text = "Daily Limit usage"
                };
                layout.AddView(appLimitText);

                // Placeholder progress bar at the top
                var progressBar = new Android.Widget.ProgressBar(this, null, Android.Resource.Attribute.ProgressBarStyleHorizontal)
                {
                    LayoutParameters = new LinearLayout.LayoutParams(
                        ViewGroup.LayoutParams.MatchParent,
                        20) // height in pixels, adjust as needed
                };
                progressBar.Max = 100;      // maximum value
                progressBar.Progress = 0;   // current progress
                layout.AddView(progressBar);

                // Handler for updating progress
                var handler = new Android.OS.Handler();
                int progress = 0;

                // Runnable to update the progress every 1 second
                void UpdateProgress()
                {
                    progress += 10; // increase by 10%
                    if (progress > 100) progress = 0; // reset when exceeding 100%
                    progressBar.Progress = progress;

                    // Schedule next update in 1 second (1000 ms)
                    handler.PostDelayed(UpdateProgress, 1000);
                }

                // Start the loop
                UpdateProgress();

                // placeholder for streaks
                var streakDays = 10;
                var streakText = new TextView(this)
                {
                    TextSize = 18f,
                    Text = $"You have a {streakDays} day streak."
                };
                layout.AddView(streakText);
            }

            // Original "continue" question
            var tv = new TextView(this)
            {
                TextSize = 18f,
                Text = $"Do you want to continue into {appName}?\n You must complete a task first."
            };
            layout.AddView(tv);

            // 🧠 Load a random question from the list of questions
            var questions = QuestionBank.Questions;
            var randomQuestion = new System.Random();
            var question = questions[randomQuestion.Next(questions.Count)];

            // Display the popup task question
            var questionText = new TextView(this)
            {
                TextSize = 18f,
                Text = question.Text
            };
            layout.AddView(questionText);

            // Add an EditText below the question for user input
            var answerBox = new EditText(this)
            {
                Hint = "Enter your answer"
            };
            answerBox.SetFilters(new Android.Text.IInputFilter[]
            {
                new Android.Text.InputFilterLengthFilter(16),
                new AllowedCharacterFilter()
            });
            layout.AddView(answerBox);

            // Optionally specify a session timer (placeholder for now)
            var sessionTimerText = new TextView(this)
            {
                TextSize = 18f,
                Text = "Optionally, specify a session timer."
            };
            layout.AddView(sessionTimerText);

            // Slider for session time in minutes
            var timeSlider = new Android.Widget.SeekBar(this)
            {
                Max = 60 // max 60 minutes
            };
            timeSlider.Progress = 0; // default 0 minutes = "unlimited"
            layout.AddView(timeSlider);

            // Label to show selected value
            var timeLabel = new TextView(this)
            {
                Text = "Duration: Unlimited",
                TextSize = 16f
            };
            layout.AddView(timeLabel);

            // Snap to 5-minute increments
            timeSlider.ProgressChanged += (s, e) =>
            {
                // Round to nearest multiple of 5
                int snappedValue = (int)(Math.Round(e.Progress / 5.0) * 5);

                // Update slider to snapped value
                timeSlider.Progress = snappedValue;

                // Update label
                timeLabel.Text = snappedValue == 0 ? "Duration: Unlimited" : $"Duration: {snappedValue} minutes";
            };

            // Buttons layout
            var buttonLayout = new LinearLayout(this) { Orientation = Orientation.Horizontal };

            var yesBtn = new Android.Widget.Button(this) { Text = "Yes open app" };
            var noBtn = new Android.Widget.Button(this) { Text = "No go away" };

            yesBtn.Enabled = false;

            var randomYesNo = new System.Random();
            if (randomYesNo.Next(2) == 0)
            {
                // Add Yes first, No second
                buttonLayout.AddView(yesBtn);
                buttonLayout.AddView(noBtn);
            }
            else
            {
                // Add No first, Yes second
                buttonLayout.AddView(noBtn);
                buttonLayout.AddView(yesBtn);
            }

            layout.AddView(buttonLayout);

            SetContentView(layout);

            // Text change listener to check answer
            answerBox.TextChanged += (s, e) =>
            {
                string answer = answerBox.Text.Trim();
                // Accept "2" or "two" (case insensitive)
                yesBtn.Enabled = (answer == question.CorrectAnswer);
            };

            yesBtn.Click += (s, e) =>
            {
                // Just finish and let clock app remain in foreground
                FinishAndRemoveTask();
            };

            noBtn.Click += (s, e) =>
            {
                // Send user to Home, backgrounding clock app
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
