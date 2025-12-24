using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Maui.ApplicationModel; // for FileSystem.OpenAppPackageFileAsync

namespace HourGuard
{
    public class Question
    {
        public string Text { get; set; }
        public string CorrectAnswer { get; set; }
    }

    public static class QuestionBank
    {
        private static List<Question> _questions;

        public static List<Question> Questions
        {
            get
            {
                if (_questions == null)
                {
                    _questions = LoadQuestions();
                }
                return _questions;
            }
        }

        private static List<Question> LoadQuestions()
        {
            using var stream = FileSystem.OpenAppPackageFileAsync("PopupTasks.json").Result;
            using var reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<List<Question>>(json);
        }
    }
}
