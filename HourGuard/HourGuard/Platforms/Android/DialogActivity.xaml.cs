namespace HourGuard
{
    public partial class DialogActivity : ContentPage
    {
        private readonly Question _question;
        private float _progress = 0;

        public DialogActivity(string appName)
        {
            InitializeComponent();

            if (string.IsNullOrEmpty(appName))
                appName = "THIS APP";

            // ----- Streak placeholder -----
            int streakDays = 10;
            StreakLabel.Text = $"You have a {streakDays} day streak.";

            PromptLabel.Text =
                $"Do you want to continue into {appName}?\nYou must complete a task first.";

            // ----- Random question -----
            var rand = new Random();
            _question = QuestionBank.Questions[rand.Next(QuestionBank.Questions.Count)];
            QuestionLabel.Text = _question.Text;

            // ----- Progress bar loop -----
            StartProgressLoop();

            // ----- Answer validation -----
            AnswerEntry.TextChanged += (_, __) =>
            {
                YesButton.IsEnabled =
                    AnswerEntry.Text?.Trim() == _question.CorrectAnswer;
            };

            // ----- Slider snapping -----
            TimeSlider.ValueChanged += (_, e) =>
            {
                int snapped = (int)(Math.Round(e.NewValue / 5.0) * 5);
                TimeSlider.Value = snapped;

                TimeLabel.Text = snapped == 0
                    ? "Duration: Unlimited"
                    : $"Duration: {snapped} minutes";
            };

            // ----- Buttons -----
            CreateButtons();
        }

        Button YesButton;

        private void CreateButtons()
        {
            var yesBtn = new Button
            {
                Text = "Yes open app",
                IsEnabled = false
            };

            var noBtn = new Button
            {
                Text = "No go away"
            };

            YesButton = yesBtn;

            yesBtn.Clicked += async (_, __) =>
            {
                await Navigation.PopModalAsync();
            };

            noBtn.Clicked += async (_, __) =>
            {
                await Navigation.PopModalAsync();
            };

            // Random order
            if (Random.Shared.Next(2) == 0)
            {
                ButtonRow.Add(yesBtn);
                ButtonRow.Add(noBtn);
            }
            else
            {
                ButtonRow.Add(noBtn);
                ButtonRow.Add(yesBtn);
            }
        }

        private void StartProgressLoop()
        {
            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                _progress += 0.1f;
                if (_progress > 1)
                    _progress = 0;

                UsageProgress.Progress = _progress;
                return true; // keep looping
            });
        }

        protected override bool OnBackButtonPressed()
        {
            // Treat back as "No"
            return true; // disable default back behavior
        }
    }
}
