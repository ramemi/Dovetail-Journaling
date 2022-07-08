/*
 * File Name: JournalEntry.cs
 * Dovetail Journaling
 * 
 * Purpose: Holding data of journal entries.
 * This file contains the implementation for the composite pattern, which
 * allows the rest of the application to treat a single journal entry the 
 * same as a list of journal entries.
 * 
 * Author: Emily Ramanna
 * Contact: ramannae@mcmaster.ca
 */

using System;
using System.Collections.Generic;

namespace DovetailJournaling.Models
{
    // interface that defines commonality between a single journal entry
    // and a list of journal entries
    interface IJournalEntryComponent
    {
        // properties
        string Content { get; set; }
        DateTime Date { get; set; }
        // each journal entry can have multiple topics
        List<TopicModel> Topics { get; set; }
        int Count { get; }

        // methods that a journal entry component needs to provide
        void LearnTopicsAndSentiments(IAnalysisService service);
        bool SaveJournalEntry(IUserRepository repo, string username);
        bool DeleteJournalEntry(IUserRepository repo, string username, int index);
    }

    // implements interface IJournalEntryComponent
    // represents a singular journal entry
    class JournalEntryModel : IJournalEntryComponent
    {
        public string Content { get; set; }
        public DateTime Date { get; set; }
        public List<TopicModel> Topics { get; set; }

        // count will only ever be 1
        public int Count
        {
            get { return 1; }
        }

        // Constructor to initialize with values
        public JournalEntryModel(string content, DateTime date)
        {
            Topics = new List<TopicModel>();
            Content = content;
            Date = date;
        }

        // learns topics with analysis service passed in
        public void LearnTopicsAndSentiments(IAnalysisService service)
        {
            Topics = service.GetTopicsAndSentiments(Content);
        }

        // string representation of journal entry
        public override string ToString()
        {
            string representation = "Date of entry: " + Date.ToString("MM/dd/yyyy") +
                "\n\nEntry:\n\t" + Content + "\n\n" + "Topics and sentiments identified:\n";

            if (Topics.Count == 0)
            {
                representation += "\tNo topics and sentiments identified for this entry.\n\n";
            }

            // formats topics identified
            foreach (var topic in Topics)
            {
                representation += "\t" + topic.Keyword + ": " + topic.Sentiment + "\n";
            }

            representation += "\n";
            return representation;
        }

        // persists journal entry with repo object passed in
        public bool SaveJournalEntry(IUserRepository repo, string username)
        {
            return repo.AddJournalEntry(this, username);
        }

        // deletes journal entry with repo object passed in
        public bool DeleteJournalEntry(IUserRepository repo, string username, int index)
        {
            return repo.DeleteJournalEntry(this, username);
        }
    }

    // implements interface IJournalEntryComponent
    // represents a list of journal entries, the "composite"
    class JournalEntryListModel : IJournalEntryComponent
    {
        public string Content { get; set; }
        public DateTime Date { get; set; }
        public List<TopicModel> Topics { get; set; }

        // list of child components
        public List<IJournalEntryComponent> JournalEntryList { get; set; }

        // constructor that initializes the list of journal entry components
        public JournalEntryListModel()
        {
            JournalEntryList = new List<IJournalEntryComponent>();
        }

        // how many child components
        public int Count
        {
            get { return JournalEntryList.Count; }
        }

        // learn topics and sentiments for each component with
        // analysis service passed in
        public void LearnTopicsAndSentiments(IAnalysisService service)
        {
            foreach (var entry in JournalEntryList)
            {
                entry.LearnTopicsAndSentiments(service);
            }
        }

        // string representation of a list of journal entries
        public override string ToString()
        {
            string representation = "Journal entries made on this date : " + Date.ToString("MM/dd/yyyy") + "\n\n";
            if (JournalEntryList.Count == 0)
            {
                representation += "There were no journal entries made by you on this date.\n";
            }
            else
            {
                int i = 1;
                foreach (var entry in JournalEntryList)
                {
                    representation += "Journal Entry #" + i + ":\n";
                    representation += entry.ToString();
                    i++;
                }
            }
            return representation;
        }

        // save each child component using repo object passed in
        public bool SaveJournalEntry(IUserRepository repo, string username)
        {
            bool wereAllSavedSuccessfully = true;
            foreach (var entry in JournalEntryList)
            {
                bool successfulSave = entry.SaveJournalEntry(repo, username);
                if (!successfulSave)
                {
                    wereAllSavedSuccessfully = false;
                }
            }
            return wereAllSavedSuccessfully;
        }

        // delete a single child component based on the index passed in, using repo object
        public bool DeleteJournalEntry(IUserRepository repo, string username, int index)
        {
            return repo.DeleteJournalEntry(JournalEntryList[index], username);
        }
    }
}
