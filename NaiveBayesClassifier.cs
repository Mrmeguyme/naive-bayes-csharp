using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace Document_Sort
{
    class NaiveBayesClassifier
    {
        public bayesInfo i;

        public NaiveBayesClassifier ()
        {   // Creating the bayesInfo object.
            i = new bayesInfo();

            init();
        }

        public NaiveBayesClassifier (string jsonPath)
        {   // Creates the bayesInfo object from a json file.
            bayesInfo jsonObj = JsonConvert.DeserializeObject<bayesInfo>(File.ReadAllText(jsonPath));
            i = jsonObj;
        }

        public void init () // Initializes the bayesInfo object.
        {
            i.vocab = new Dictionary<string, bool>();
            i.vocabSize = 0;

            i.totalDocs = 0;
            i.docCount = new Dictionary<string, int>();

            i.wordCount = new Dictionary<string, int>();
            i.wordFreqCount = new Dictionary<string, Dictionary<string, int>>();

            i.categories = new Dictionary<string, bool>();
        }

        public void initCategory (string name)
        {
            if (!i.categories.ContainsKey(name)) // Initializes category if the category does not already exist.
            {
                i.docCount.Add(name, 0);
                i.wordCount.Add(name, 0);
                i.wordFreqCount.Add(name, new Dictionary<string, int>());
                i.categories.Add(name, true);
            }
        }

        public void learn (string text, string category)
        {
            initCategory(category); // initializes the category if it doesn't already exist.
            i.docCount[category]++; // Increase the amount of documents for the individual category.
            i.totalDocs++; // Increase the amount of documents for the total classifier.

            string[] words = filter(text); // Splits the words up into a list of strings, as well as removing all special characters.
            Dictionary<string, int> freqTable = getFreqTable(words); // Gets the frequency table for all words.

            string[] keys = freqTable.Keys.ToArray();
            for (int k = 0; k < keys.Length; k++)
            {
                if (!i.vocab.ContainsKey(keys[k]))
                {
                    i.vocab[keys[k]] = true;
                    i.vocabSize++;
                }
                
                int freqInText = freqTable[keys[k]];

                if (!i.wordFreqCount[category].ContainsKey(keys[k]))
                {
                    i.wordFreqCount[category].Add(keys[k], freqInText);
                }
                else
                {
                    i.wordFreqCount[category][keys[k]] += freqInText;
                }

                i.wordCount[category] += freqInText;
            }

        }

        public string categorize (string text)
        {
            double maxProb = double.NegativeInfinity;
            string chosenCategory = "";

            string[] words = filter(text);
            Dictionary<string, int> freqTable = getFreqTable(words);

            string[] keys = i.categories.Keys.ToArray();

            for (int k = 0; k < keys.Length; k++)
            {
                double categoryProb = (double)i.docCount[keys[k]] / i.totalDocs; // Calculating overall probability of category
                double logProb = Math.Log10(categoryProb); // calculate log to avoid underflow

                string[] fkeys = freqTable.Keys.ToArray();

                for (int l = 0; l < fkeys.Length; l++) // Calculate P(W|C) for each word.
                {
                    double freqInText = freqTable[fkeys[l]];
                    double wordProb = tokenProb(fkeys[l], keys[k]);

                    //Console.Out.WriteLine($"Word: {fkeys[l]}, category: {keys[k]}, token probability: {wordProb}, frequency: {freqInText}, {freqInText * Math.Log10(wordProb)}");

                    logProb += freqInText * Math.Log10(wordProb);
                }

                if (logProb > maxProb)
                {
                    maxProb = logProb;
                    chosenCategory = keys[k];
                }

                Console.Out.WriteLine($"Log Prob: {logProb}");
            }

            return chosenCategory;
        }

        public double tokenProb (string word, string category)
        {
            double wordFreqCount;

            if (!i.wordFreqCount[category].ContainsKey(word))
            {
                wordFreqCount = 0;
            }
            else
            {
                wordFreqCount = i.wordFreqCount[category][word];
            }
            
            //Console.Out.WriteLine($"{wordFreqCount}, {i.wordCount[category]}, {i.vocabSize}");

            double wordCount = i.wordCount[category];
            double k = 0.0001;

            return (wordFreqCount + k) / (wordCount + i.vocabSize);
        }

        public Dictionary<string, int> getFreqTable (string[] words)
        {
            Dictionary<string, int> freqTable = new Dictionary<string, int>();

            for (int k = 0; k < words.Length; k++)
            {
                if (!freqTable.ContainsKey(words[k]))
                {
                    freqTable.Add(words[k], 1);
                }
                else
                {
                    freqTable[words[k]]++;
                }
            }

            return freqTable;
        }

        public string[] filter (string text)
        {   // Filters out all non-text characters from a string
            // Regex rgx = new Regex(@"/[^(a-zA-ZA-Яa-я0-9_)+\s]/g"); - DIDN'T WORK
            var sb = new StringBuilder();

            foreach (char c in text)
            {
                if (!char.IsPunctuation(c))
                    if (c == '\t' || c == '\n')
                    {
                        sb.Append(' ');
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    
            }

            string output = sb.ToString();

            return output
                .ToLower() // stops uppercase and lowercase affecting the classifier
                .Split(" ") // Splits the string into an array of words
                .Where(x => !string.IsNullOrEmpty(x) && !(x==" ")) // Makes sure that there are no blank words.
                .ToArray(); // Transforms the enumerable back into an array
        }

        public bayesInfo getClassifier ()
        {   //Returns the classifier object.
            return i;
        }
    }

    class bayesInfo
    {
        public Dictionary<string, bool> categories;
        public Dictionary<string, int> docCount;
        public int totalDocs;
        public Dictionary<string, bool> vocab;
        public int vocabSize;
        public Dictionary<string, int> wordCount;
        public Dictionary<string, Dictionary<string, int>>wordFreqCount;
    }
}
