using System;
using System.Collections.Generic;
using UnityEngine;

namespace Networking.Models
{
    [Serializable]
    public class TriviaQuestion
    {
        public string QuestionText;
        public string CorrectAnswer;
        public string WrongAnswer1;
        public string WrongAnswer2;
        public string WrongAnswer3;
    }

    [CreateAssetMenu(fileName = "TriviaDatabase", menuName = "Networking/Trivia Database")]
    public class TriviaDatabase : ScriptableObject
    {
        [SerializeField] private List<TriviaQuestion> _questions = new List<TriviaQuestion>();

        public TriviaQuestion GetRandom()
        {
            if (_questions == null || _questions.Count == 0) return null;
            return _questions[UnityEngine.Random.Range(0, _questions.Count)];
        }
    }
}
