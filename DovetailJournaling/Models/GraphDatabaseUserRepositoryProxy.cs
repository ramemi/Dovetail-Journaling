/*
 * File Name: GraphDatabaseUserRepositoryProxy.cs
 * Dovetail Journaling
 * 
 * Purpose: The proxy repo, handles logging for each of the "real" repository's
 * methods that interact with the database.
 * 
 * Methods sandwich the "real" repository method calls with code to start a timer
 * before and code to stop the timer and log a message after.
 * In the future, this proxy could have more uses, like caching recent requests.
 * 
 * Uses AppConfigData, the singleton, to get the connection to the database
 * in order to persist log messages.
 * 
 * This is included in the "Models" folder because data persistence is a
 * part of the model layer.
 * 
 * Author: Emily Ramanna
 * Contact: ramannae@mcmaster.ca
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DovetailJournaling.Models
{
    // implements the IUserRepository interface, as part of the Repository pattern
    class GraphDatabaseUserRepositoryProxy : IUserRepository
    {
        // instance of the "real" graph database repo
        private GraphDatabaseUserRepository realDBRepo;

        // constructor that instantiates the "real" object
        public GraphDatabaseUserRepositoryProxy()
        {
            realDBRepo = new GraphDatabaseUserRepository();
        }

        // Creates a log node that has a relationship to the user that initiated the action
        private void LogAssociatedToUser(string username, long ms, string message)
        {
            string queryString = "CYPHER username_param = \"" + StringHelper.EscapeString(username) +
                "\" message_param = \"" + StringHelper.EscapeString(message) +
                 "\"; MERGE (a:User {username: $username_param}) " +
                 "MERGE (b:LogMessage {time: "+ ms + ", message: $message_param})" +
                 "MERGE (a)-[:ACTIVITY]-(b)";
            AppConfigData.Config.GraphDB.Query(AppConfigData.Config.DBName, queryString);
        }

        // Creates a log node that has no relationships
        private void LogMessage(long ms, string message)
        {
            string queryString = "CYPHER message_param = \"" + StringHelper.EscapeString(message) + "\";" +
                 " CREATE (:LogMessage {time: " + ms + ", message: $message_param})";
            AppConfigData.Config.GraphDB.Query(AppConfigData.Config.DBName, queryString);
        }

        public bool AddJournalEntry(IJournalEntryComponent entry, string username)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            bool result = realDBRepo.AddJournalEntry(entry, username);

            stopwatch.Stop();

            // logs different message if it failed, associates to user
            if (result)
            {
                LogAssociatedToUser(username, stopwatch.ElapsedMilliseconds, "AddJournalEntry");
            }
            else
            {
                LogAssociatedToUser(username, stopwatch.ElapsedMilliseconds, "Failure:AddJournalEntry");
            }
            
            return result;
        }

        public bool CreateNewUser(UserModel userModel)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            bool result = realDBRepo.CreateNewUser(userModel);

            stopwatch.Stop();

            // logs different message if it failed, does not associate to user
            if (result)
            {
                LogMessage(stopwatch.ElapsedMilliseconds, "CreateNewUser");
            }
            else
            {
                LogMessage(stopwatch.ElapsedMilliseconds, "Failure:CreateNewUser");
            }

            return result;
        }

        public bool CreateNewUserContact(string username, UserContactModel userContact)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            bool result = realDBRepo.CreateNewUserContact(username, userContact);

            stopwatch.Stop();

            // logs different message if it failed, associates to user
            if (result)
            {
                LogAssociatedToUser(username, stopwatch.ElapsedMilliseconds, "CreateNewUserContact");
            }
            else
            {
                LogAssociatedToUser(username, stopwatch.ElapsedMilliseconds, "Failure:CreateNewUserContact");
            }

            return result;
        }

        public IJournalEntryComponent GetJournalEntries(DateTime date, string username)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            IJournalEntryComponent journalEntries = realDBRepo.GetJournalEntries(date, username);

            stopwatch.Stop();

            // logs how long it took, associates to user
            LogAssociatedToUser(username, stopwatch.ElapsedMilliseconds, "GetJournalEntries");

            return journalEntries;
        }

        public List<UserContactModel> GetSameSentimentList(string username, string sentiment)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            List<UserContactModel> list = realDBRepo.GetSameSentimentList(username, sentiment);

            stopwatch.Stop();

            // logs how long it took, associates to user
            LogAssociatedToUser(username, stopwatch.ElapsedMilliseconds, "SameSentimentList");

            return list;

        }

        public UserModel GetUserByUserName(string username)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            UserModel retrieved = realDBRepo.GetUserByUserName(username);

            stopwatch.Stop();

            // logs how long it took, does not associate to user
            LogMessage(stopwatch.ElapsedMilliseconds, "GetUserByUserName");

            return retrieved;
        }

        public List<UserContactModel> GetUserContactRelationships(string username)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            List<UserContactModel> list = realDBRepo.GetUserContactRelationships(username);

            stopwatch.Stop();

            // logs how long it took, associates to user
            LogAssociatedToUser(username, stopwatch.ElapsedMilliseconds, "GetUserContactRelationships");

            return list;
        }

        public bool DeleteJournalEntry(IJournalEntryComponent entry, string username)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            bool result = realDBRepo.DeleteJournalEntry(entry, username);

            stopwatch.Stop();

            // logs different message if it failed, associates to user
            if (result)
            {
                LogAssociatedToUser(username, stopwatch.ElapsedMilliseconds, "DeleteJournalEntry");
            }
            else
            {
                LogAssociatedToUser(username, stopwatch.ElapsedMilliseconds, "Failure:DeleteJournalEntry");
            }

            return result;
        }

        public bool UpdateUserPassword(string username, string newHashedPassword, string newSalt)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            bool result = realDBRepo.UpdateUserPassword(username, newHashedPassword, newSalt);

            stopwatch.Stop();

            // logs different message if it failed, associates to user
            if (result)
            {
                LogAssociatedToUser(username, stopwatch.ElapsedMilliseconds, "UpdateUserPassword");
            }
            else
            {
                LogAssociatedToUser(username, stopwatch.ElapsedMilliseconds, "Failure:UpdateUserPassword");
            }

            return result;
        }

        public bool UpdateUserContactInfo(string username, string newContactInfo)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            bool result = realDBRepo.UpdateUserContactInfo(username, newContactInfo);

            // logs different message if it failed, associates to user
            if (result)
            {
                LogAssociatedToUser(username, stopwatch.ElapsedMilliseconds, "UpdateUserContactInfo");
            }
            else
            {
                LogAssociatedToUser(username, stopwatch.ElapsedMilliseconds, "Failure:UpdateUserContactInfo");
            }

            return result;
        }
    }
}
