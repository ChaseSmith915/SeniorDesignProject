using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Widget;
using Javax.Security.Auth;
using Kotlin.IO.Encoding;
using Microsoft.Maui.Platform;
using static Android.Provider.ContactsContract.CommonDataKinds;
using static Microsoft.Maui.ApplicationModel.Platform;
using Intent = Android.Content.Intent;

namespace HourGuard
{
    [Activity(Label = "Confirm", Theme = "@style/DialogTheme")]
    public class DialogActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetFinishOnTouchOutside(false);

            // grabs arguments
            string appName = Intent.GetStringExtra("appName");
            long dailyTimeUsedMillis = Intent.GetLongExtra("dailyTimeUsedMillis", 0);
            TimeSpan dailyTimeUsed = TimeSpan.FromMilliseconds(dailyTimeUsedMillis);
            long dailyTimeLimitMillis = Intent.GetLongExtra("dailyTimeLimitMillis", 0);
            TimeSpan dailyTimeLimit = TimeSpan.FromMilliseconds(dailyTimeLimitMillis);
            int streak = Intent.GetIntExtra("streak", 0);

            // TEMP VARIABLES
            streak = -1;
            dailyTimeUsed = new TimeSpan(1, 1, 0);
            dailyTimeLimit = new TimeSpan(1, 0, 0);

            // timespan formatting
            string timespanFormat(TimeSpan time)
            {
                string output = "";
                if (time.Hours > 1)
                {
                    output += time.ToString("%h") + " Hours";
                }
                else if (time.Hours == 1)
                {
                    output += "1 Hour";
                }
                if (time.Hours > 0 && time.Minutes > 0)
                {
                    output += " ";
                }
                if (time.Minutes > 1)
                {
                    output += time.ToString("%m") + " Minutes";
                }
                else if (time.Minutes == 1)
                {
                    output += "1 Minute";
                }
                return output;
            }
            string dailyTimeUsedString = timespanFormat(dailyTimeUsed);
            string dailyTimeLimitString = timespanFormat(dailyTimeLimit);

            // failsafe for app name
            if (appName == null)
            {
                appName = "this app";
            }

            // determines percent of time used
            int dailyLimitUsedPercent = (int)Math.Truncate(dailyTimeUsed.TotalSeconds / dailyTimeLimit.TotalSeconds * 100);

            // load layout from xml file
            SetContentView(Resource.Layout.dialog_activity);

            // set variables
            var dailyLimitLayout = FindViewById<LinearLayout>(Resource.Id.dailyLimitLayout);
            var dailyLimitInfoText = FindViewById<TextView>(Resource.Id.dailyLimitInfoText);
            var dailyLimitText = FindViewById<TextView>(Resource.Id.dailyLimitText);
            var dailyLimitProgressBar = FindViewById<Android.Widget.ProgressBar>(Resource.Id.dailyLimitProgressBar);
            var dividerDailyLimit = FindViewById(Resource.Id.dividerDailyLimit);
            var streakText = FindViewById<TextView>(Resource.Id.streakText);
            var continueIntoAppText = FindViewById<TextView>(Resource.Id.continueIntoAppText);
            var taskQuestionText = FindViewById<TextView>(Resource.Id.taskQuestionText);
            var taskAnswerBox = FindViewById<EditText>(Resource.Id.taskAnswerBox);
            var sessionTimerText = FindViewById<TextView>(Resource.Id.sessionTimerText);
            var sessionTimerSlider = FindViewById<SeekBar>(Resource.Id.sessionTimerSlider);
            var sessionTimerValueText = FindViewById<TextView>(Resource.Id.sessionTimerLabelText);
            var buttonLayout = FindViewById<LinearLayout>(Resource.Id.buttonLayout);
            var yesButton = FindViewById<Android.Widget.Button>(Resource.Id.yesButton);
            var noButton = FindViewById<Android.Widget.Button>(Resource.Id.noButton);

            // colors
            Android.Graphics.Color colorPrimary = new Android.Graphics.Color(AndroidX.Core.Content.ContextCompat.GetColor(this, Resource.Color.Primary));
            Android.Content.Res.ColorStateList colorGray100 = Android.Content.Res.ColorStateList.ValueOf(new Android.Graphics.Color(AndroidX.Core.Content.ContextCompat.GetColor(this, Resource.Color.Gray100)));
            Android.Graphics.Color colorSecondary = new Android.Graphics.Color(AndroidX.Core.Content.ContextCompat.GetColor(this, Resource.Color.Secondary));
            Android.Content.Res.ColorStateList colorSecondaryDarkText = Android.Content.Res.ColorStateList.ValueOf(new Android.Graphics.Color(AndroidX.Core.Content.ContextCompat.GetColor(this, Resource.Color.SecondaryDarkText)));

            // daily limit usage
            if (dailyTimeLimit > new TimeSpan(0))
            {
                dailyLimitInfoText.Text = "Daily time limit usage:";
                dailyLimitText.Text = $"{dailyTimeUsedString} of {dailyTimeLimitString}";
                if (dailyLimitUsedPercent >= 100)
                {
                    dailyLimitProgressBar.Progress = 100;
                    dailyLimitProgressBar.ProgressTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Red);
                }
                else
                {
                    dailyLimitProgressBar.Progress = dailyLimitUsedPercent;
                }

                // streaks
                if (streak > 0 && dailyLimitUsedPercent == 100)
                {
                    streakText.Text = $"If you continue, you will lose your {streak} day streak! Exit now to keep your streak.";
                }
                else if (streak > 0 && dailyLimitUsedPercent > 100)
                {
                    streakText.Text = "This text should never show, because it means you went over your limit but your streak was not removed :(";
                }
                else if (streak > 0)
                {
                    streakText.Text = $"You currently have a {streak} day streak!";
                }
                else if (streak == 0 && dailyLimitUsedPercent == 100)
                {
                    streakText.Text = "You do not have an active streak. If you continue, you will not start a new streak today.";
                }
                else if (streak == 0 && dailyLimitUsedPercent > 100)
                {
                    streakText.Text = "You do not have an active streak and you went over your time limit. You will not start a new streak today.";
                }
                else if (streak == 0)
                {
                    streakText.Text = "You do not have an active streak. Stay under your time limit to start a new one today!";
                }
                else if (streak < 0)
                {
                    // streak should be set to -1 on the day it is broken, then incremented back to 0 the next day
                    streakText.Text = "You lost your streak today. Start a new one tomorrow!";
                }
                else
                {
                    // this should never be reached
                    streakText.Text = "How did we get here?";
                }
            }
            else // remove section if no daily limit is set
            {
                dailyLimitLayout.RemoveAllViews();
                dividerDailyLimit.RemoveFromParent();
            }

            // continue into app
            continueIntoAppText.Text = $"You must complete a task before you can open {appName}.";

            // task
            var questions = QuestionBank.Questions;
            var randomQuestion = new System.Random();
            var question = questions[randomQuestion.Next(questions.Count)];
            taskQuestionText.Text = question.question;

            // answer box
            taskAnswerBox.SetFilters(new Android.Text.IInputFilter[]
            {
                new Android.Text.InputFilterLengthFilter(16),
                new AllowedCharacterFilter()
            });
            taskAnswerBox.TextChanged += (s, e) =>
            {
                string answer = taskAnswerBox.Text.Trim();
                if (answer == question.correctAnswer)
                {
                    yesButton.Enabled = true;
                    yesButton.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(colorPrimary);
                    yesButton.SetTextColor(colorGray100);
                }
                else
                {
                    yesButton.Enabled = false;
                    yesButton.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(colorSecondary);
                    yesButton.SetTextColor(colorSecondaryDarkText);
                }
            };

            // session timer
            sessionTimerText.Text = "Optionally, set a session timer:";
            sessionTimerValueText.Text = "Duration: session timer not set";
            sessionTimerSlider.ProgressChanged += (s, e) =>
            {
                int sessionTimer = (int)(Math.Round(e.Progress / 5.0) * 5);
                sessionTimerSlider.Progress = sessionTimer;
                sessionTimerValueText.Text = sessionTimer == 0 ? "Duration: session timer not set" : $"Duration: {sessionTimer} minutes";
            };

            // buttons
            yesButton.Text = "Continue";
            yesButton.Click += (s, e) =>
            {
                //var launchIntent = PackageManager.GetLaunchIntentForPackage("com.android.chrome");

                //launchIntent.AddFlags(ActivityFlags.NewTask);
                //StartActivity(launchIntent);

                FinishAndRemoveTask();
            };
            noButton.Text = "Exit";
            noButton.Click += (s, e) =>
            {
                Intent intent = new Intent(Intent.ActionMain);
                intent.AddCategory(Intent.CategoryHome);
                intent.SetFlags(ActivityFlags.NewTask);
                StartActivity(intent);

                FinishAndRemoveTask();
            };

            void RandomButtonOrder()
            {
                buttonLayout.RemoveAllViews();

                var randomYesNo = new System.Random();
                if (randomYesNo.Next(2) == 0)
                {
                    // Add Yes first, No second
                    buttonLayout.AddView(yesButton);
                    buttonLayout.AddView(noButton);
                }
                else
                {
                    // Add No first, Yes second
                    buttonLayout.AddView(noButton);
                    buttonLayout.AddView(yesButton);
                }
            }
            RandomButtonOrder();

        }

        // Prevent back from doing anything surprising
        public override void OnBackPressed()
        {
            // treat as No (send home)
            Intent intent = new Intent(Intent.ActionMain);
            intent.AddCategory(Intent.CategoryHome);
            intent.SetFlags(ActivityFlags.NewTask);
            StartActivity(intent);
            FinishAndRemoveTask();
        }
    }
}
