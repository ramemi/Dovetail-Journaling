/*
 * File Name: AdditonalDataModels.cs
 * Dovetail Journaling
 * 
 * Purpose: These models don't have any business logic and are used
 * to mimic an ORM, i.e. they just hold data retrieved from the database.
 * Logic related to these models are handled by the UserModel and 
 * JournalEntryModel/JournalEntryListModel models.
 * 
 * Author: Emily Ramanna
 * Contact: ramannae@mcmaster.ca
 */

namespace DovetailJournaling.Models
{
    // holds data from Topic nodes in database
    class TopicModel
    {
        public string Keyword { get; set; }
        public string Sentiment { get; set; }
    }

    // holds data from UserConnection nodes in database
    class UserContactModel
    {
        public string UserName { get; set; }
        public string ContactInfo { get; set; }
        public string GUID { get; set; }
        public TopicModel Topic { get; set; }
    }
}
