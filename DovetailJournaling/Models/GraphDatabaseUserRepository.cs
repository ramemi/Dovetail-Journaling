/*
 * File Name: GraphDatabaseUserRepository.cs
 * Dovetail Journaling
 * 
 * Purpose: Performs data persistence using a Redisgraph database as storage.
 * This is the "real" user repository, that does all of the communicating with the
 * database instance, apart from logging, which is handled by the proxy.
 * 
 * For the Cypher commands used in this class:
 * parameters are defined before each command, as part of a strategy to 
 * mitigate injection attacks (along with escaping string input from the user).
 * Also, AppConfigData, the singleton, is used to get the database connection
 * and the strings associated with the types of sentiment.
 * 
 * This is included in the "Models" folder because data persistence is a
 * part of the model layer.
 * 
 * Author: Emily Ramanna
 * Contact: ramannae@mcmaster.ca
 */

using System;
using System.Collections.Generic;

namespace DovetailJournaling.Models
{
    // implements the IUserRepository interface, as part of the Repository pattern
    class GraphDatabaseUserRepository : IUserRepository
    {
        // Creates a user node in the graph database
        public bool CreateNewUser(UserModel userModel)
        {
            // parameters defined and then create statement
            var rs = AppConfigData.Config.GraphDB.Query(AppConfigData.Config.DBName,
                "CYPHER username_param = \"" + StringHelper.EscapeString(userModel.UserName) +
                "\" password_param = \"" + StringHelper.EscapeString(userModel.Password) +
                "\" salt_param = \"" + StringHelper.EscapeString(userModel.Salt) +
                "\" contactInfo_param = \"" + StringHelper.EscapeString(userModel.ContactInfo) +
                "\"; CREATE (:User {username: $username_param, password: $password_param," +
                " salt: $salt_param, contactInfo: $contactInfo_param})");

            // one node should be created
            if (rs.Result.Metrics.NodesCreated > 0)
            {
                return true;
            }

            return false;
        }

        // Retrieve a user node by a username
        public UserModel GetUserByUserName(string username)
        {
            UserModel retrieved = null;

            // parameters and MATCH command
            var rs = AppConfigData.Config.GraphDB.Query(AppConfigData.Config.DBName,
                "CYPHER username_param = \"" + StringHelper.EscapeString(username) +
                "\"; MATCH (u:User {username: $username_param}) RETURN u");

            // sets up UserModel object with retrieved values
            if (rs.Result.Results.Count == 1)
            {
                retrieved = new UserModel();
                Node userData = rs.Result.Results["u"][0] as Node;
                retrieved.UserName = userData.Properties["username"].ToString();
                retrieved.Password = userData.Properties["password"].ToString();
                retrieved.Salt = userData.Properties["salt"].ToString();
                retrieved.ContactInfo = userData.Properties["contactInfo"].ToString();
            }

            return retrieved;

        }

        // Persists a journal entry, including any topics identified
        public bool AddJournalEntry(IJournalEntryComponent entry, string username)
        {
            // parameters and 3 MERGE commands
            // The MERGE commands ensure that there are no duplicates in the database
            // because it creates a new node only if it needs to in order to complete
            // the relationship.
            // The final MERGE command creates the AUTHOR relationship between the user node
            // and the journal entry node.
            string queryString = "CYPHER content_param = \"" +
                StringHelper.EscapeString(entry.Content) +
                "\" username_param = \"" + StringHelper.EscapeString(username) + "\";" +
                "MERGE (a:User {username: $username_param}) " +
                "MERGE (b:JournalEntry {date: " + new DateTimeOffset(entry.Date).ToUnixTimeSeconds() + ", " +
                "content: $content_param}) " +
                "MERGE (a)-[r:AUTHOR]-(b) ";

            // starting this pointer at 'c' because 'a' and 'b' were already defined in the query ( see: previous line)
            char alphabetPointer = 'c';

            // adds to the query string for each topic identified in the entry
            foreach (var topic in entry.Topics)
            {
                // adds 2 MERGE commands per topic
                // 1. To create the topic node if need be
                // 2. To create the relationship between the topic node and journal entry
                // The topic sentiment is what defines the relationship
                queryString += "MERGE (" + alphabetPointer.ToString() +
                    ":Topic {topic: '" + topic.Keyword + "'}) ";
                queryString += "MERGE (b)-[:" + topic.Sentiment + "]-(" + alphabetPointer + ") ";
                alphabetPointer++;
            }

            // actually running the prepared query
            var rs = AppConfigData.Config.GraphDB.Query(AppConfigData.Config.DBName, queryString);

            // return true if it was successful
            if (rs.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
            {
                return true;
            }

            return false;
        }

        // Retrieves a user's journal entries that were made on a specified date
        public IJournalEntryComponent GetJournalEntries(DateTime date, string username)
        {
            var dateTimestamp = new DateTimeOffset(date).ToUnixTimeSeconds();

            // Object to store the journal entries retrieved
            JournalEntryListModel journalentries = new JournalEntryListModel();
            journalentries.Date = date;

            // parameters, MATCH command for the user node, OPTIONAL MATCH command
            // for the journal entries because there may or may not be any on
            // the specified date, OPTIONAL MATCH commands for the topics because there may or 
            // may not be any for a given journal entry.
            // The WHERE clause narrows it to within the day that the user specified.
            var rs = AppConfigData.Config.GraphDB.Query(AppConfigData.Config.DBName,
                "MATCH (u:User {username: '" + username + "'}) " +
                 "OPTIONAL MATCH (u)-[:AUTHOR]-(j:JournalEntry) " +
                 " WHERE j.date > " + dateTimestamp + " AND j.date < " + (dateTimestamp + 86400) +
                 " OPTIONAL MATCH (j)-[:" + AppConfigData.Config.PositiveSentiment + "]-(t:Topic) " +
                 "OPTIONAL MATCH (j)-[:" + AppConfigData.Config.NegativeSentiment + "]-(s:Topic) " +
                 "OPTIONAL MATCH (j)-[:" + AppConfigData.Config.NeutralSentiment + "]-(v:Topic) " +
                 " RETURN  j, t, s, v");

            // if there is a result set, the first journal entry will not be
            // of the type "ScalarResult<string>"
            if (!(rs.Result.Results["j"][0] is ScalarResult<string>))
            {
                // for all of the journal entries in the result set...
                for (int i = 0; i < rs.Result.Results["j"].Count; i++)
                {
                    // object will be used to store journal entry info
                    IJournalEntryComponent journalEntryProcessed = null;

                    // if this is the first entry being considered, or if this journal entry in the 
                    // result set does not have the same date as the previous journal entry in the 
                    // result set...
                    if (i == 0 || (rs.Result.Results["j"][i - 1] as Node).Properties["date"]
                        != (rs.Result.Results["j"][i] as Node).Properties["date"])
                    {
                        // get the values to create a journal entry object
                        Node journalEntry = rs.Result.Results["j"][i] as Node;
                        string content = journalEntry.Properties["content"].ToString();
                        long seconds = long.Parse(journalEntry.Properties["date"]);
                        DateTime localTime = DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime;

                        // add the journal entry object to the list
                        journalentries.JournalEntryList.Add(new JournalEntryModel(content, localTime));

                        // This needs to be done because of the way the result set is formatted.
                        // If a journal entry has 'n' number of topics, it will show up 'n' times in 
                        // a row in the result set, so the if statement ensures that we really need
                        // to process the next journal entry instead of continuing to work on the
                        // current one.
                    }

                    // The journal entry being processed is the one with the highest index in the list
                    journalEntryProcessed = journalentries.JournalEntryList[journalentries.Count - 1];

                    // Figure out which sentiment a topic has and which element in the dictionary
                    // should be inspected to extract the contents.
                    // This is done by seeing if the index 'i' in the dictionary is a Node, 
                    // if it is, then the topic has the corresponding type of sentiment that was
                    // defined by the query string.
                    string sentiment;
                    string returnedset;
                    if (rs.Result.Results["t"][i] is Node)
                    {
                        sentiment = AppConfigData.Config.PositiveSentiment;
                        returnedset = "t";
                    }
                    else if (rs.Result.Results["s"][i] is Node)
                    {
                        sentiment = AppConfigData.Config.NegativeSentiment;
                        returnedset = "s";
                    }
                    else
                    {
                        sentiment = AppConfigData.Config.NeutralSentiment;
                        returnedset = "v";
                    }

                    // cast the appropriate dictionary element as a Node
                    Node topicNode = rs.Result.Results[returnedset][i] as Node;

                    // If the node is not null (because we can have journal entries with
                    // no topics identified, so those would be null)...
                    if (!(topicNode is null))
                    {
                        // create a TopicModel and set up its values
                        TopicModel topic = new TopicModel
                        {
                            Sentiment = sentiment,
                            Keyword = topicNode.Properties["topic"].ToString()
                        };

                        // add the topic to the journal entry
                        journalEntryProcessed.Topics.Add(topic);
                    }
                }
            }

            return journalentries;
        }



        // Retrieves a list of users who had a journal entry with the same sentiment in the past week
        public List<UserContactModel> GetSameSentimentList(string username, string sentiment)
        {
            // calculates a week ago in Unix Epoch time
            long weekago = (new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()) - 604800;

            List<UserContactModel> sameSentimentList = new List<UserContactModel>();

            // parameters, MATCH statement to find other users who had the same sentiment
            // WHERE clause narrows it to the past week
            var rs = AppConfigData.Config.GraphDB.Query(AppConfigData.Config.DBName,
                "CYPHER username_param = \"" + StringHelper.EscapeString(username) +
                "\"; Match (a1:User)-[:AUTHOR]-(j1:JournalEntry)-[:" + sentiment +
                "]-(t:Topic)-[:" + sentiment + "]-(j2:JournalEntry)-[:AUTHOR]-(a2:User)" +
                " WHERE a1.username = $username_param AND a1.username <> a2.username AND j1.date > " + weekago +
                " AND j2.date > " + weekago + " Return a2.username, a2.contactInfo, t");

            // if there were other users in the past week with the same sentiment...
            if (rs.Result.Results.Count > 0)
            {
                for (int i = 0; i < rs.Result.Results["a2.username"].Count; i++)
                {
                    // Models to be set up with retrieved info
                    UserContactModel userContact = new UserContactModel();
                    TopicModel topic = new TopicModel();

                    userContact.UserName = (rs.Result.Results["a2.username"][i] as ScalarResult<string>).Value;
                    userContact.ContactInfo = (rs.Result.Results["a2.contactInfo"][i] as ScalarResult<string>).Value;

                    topic.Keyword = (rs.Result.Results["t"][i] as Node).Properties["topic"].ToString();
                    topic.Sentiment = sentiment;

                    // Adding the topic to the UserContact model
                    userContact.Topic = topic;

                    // Adding the UserContact to the list
                    sameSentimentList.Add(userContact);
                }
            }
            return sameSentimentList;
        }

        // Creates a userconnection node that contains the date, GUID, topic, and sentiment
        // that 2 users connected over
        public bool CreateNewUserContact(string username, UserContactModel userContact)
        {
            // parameters, sequence of MERGE statements to create the userconnection node
            // and form the relationships between it and the 2 user nodes
            string queryString = "CYPHER  first_username_param = \"" +
                StringHelper.EscapeString(username) +
                "\" second_username_param = \"" +
                StringHelper.EscapeString(userContact.UserName) + "\";" +
               " MERGE (a:User {username: $first_username_param}) " +
               "MERGE (b:UserConnection {date: " + new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds() + ", " +
               "guid: \"" + userContact.GUID + "\", topic: \"" +
               userContact.Topic.Keyword + "\", sentiment: \"" + userContact.Topic.Sentiment + "\"}) " +
               "MERGE (c:User {username: $second_username_param}) " +
               "MERGE (a)-[r:SIMILAR]-(b)-[s:SIMILAR]-(c)";

            // actually running the query string
            var rs = AppConfigData.Config.GraphDB.Query(AppConfigData.Config.DBName, queryString);

            // returns true if the creation was successful
            if (rs.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
            {
                return true;
            }
            return false;
        }

        // Gets a list of userconnection nodes
        public List<UserContactModel> GetUserContactRelationships(string username)
        {
            List<UserContactModel> guidList = new List<UserContactModel>();

            // parameter, MATCH command to get the appropriate userconnection nodes
            var rs = AppConfigData.Config.GraphDB.Query(AppConfigData.Config.DBName,
                "CYPHER username_param = \"" + StringHelper.EscapeString(username) +
                "\"; MATCH (a:User)-[:SIMILAR]-(b:UserConnection)-[:SIMILAR]-(c:User) " +
                "WHERE a.username = $username_param AND a.username <> c.username RETURN b,c.username, c.contactInfo");

            // if there were nodes retrieved...
            if (rs.Result.Results.Count > 0)
            {
                for (int i = 0; i < rs.Result.Results["b"].Count; i++)
                {
                    // set up the model objects
                    UserContactModel userContact = new UserContactModel();
                    TopicModel topic = new TopicModel();

                    userContact.UserName = (rs.Result.Results["c.username"][i] as ScalarResult<string>).Value;
                    userContact.ContactInfo = (rs.Result.Results["c.contactInfo"][i] as ScalarResult<string>).Value;

                    Node userConnection = rs.Result.Results["b"][i] as Node;
                    userContact.GUID = userConnection.Properties["guid"].ToString();
                    topic.Sentiment = userConnection.Properties["sentiment"].ToString();
                    topic.Keyword = userConnection.Properties["topic"].ToString();

                    // add the topic to the user contact
                    userContact.Topic = topic;

                    // add the user contact to the list
                    guidList.Add(userContact);

                }
            }
            return guidList;
        }

        // Deletes a journal entry node from the database
        public bool DeleteJournalEntry(IJournalEntryComponent entry, string username)
        {
            // parameter, MATCH command to find the journal entry, DELETE to remove
            // it from storage
            var rs = AppConfigData.Config.GraphDB.Query(AppConfigData.Config.DBName,
                "CYPHER username_param = \"" + StringHelper.EscapeString(username) +
                "\"; MATCH (a:User {username: $username_param})-[:AUTHOR]-(j:JournalEntry) " +
                "WHERE j.date = " + new DateTimeOffset(entry.Date).ToUnixTimeSeconds() +
                " DELETE j");

            if (rs.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
            {
                return true;
            }
            return false; ;
        }

        public bool UpdateUserPassword(string username, string newHashedPassword, string newSalt)
        {
            // parameters, MATCH command to find the user node, SET to update
            // the password and salt values
            var rs = AppConfigData.Config.GraphDB.Query(AppConfigData.Config.DBName,
                "CYPHER username_param = \"" + StringHelper.EscapeString(username) +
                 "\" password_param = \"" + StringHelper.EscapeString(newHashedPassword) +
                "\" salt_param = \"" + StringHelper.EscapeString(newSalt) +
                "\"; MATCH (a:User {username: $username_param}) " +
                "SET a.password = $password_param, a.salt = $salt_param");

            if (rs.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
            {
                return true;
            }
            return false;
        }

        public bool UpdateUserContactInfo(string username, string newContactInfo)
        {
            // parameters, MATCH command to find the user node, SET to update
            // the contact information value
            var rs = AppConfigData.Config.GraphDB.Query(AppConfigData.Config.DBName,
               "CYPHER username_param = \"" + StringHelper.EscapeString(username) +
                "\" contact_param = \"" + StringHelper.EscapeString(newContactInfo) +
               "\"; MATCH (a:User {username: $username_param}) " +
               "SET a.contactInfo = $contact_param");

            if (rs.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
            {
                return true;
            }
            return false;
        }
    }
}
