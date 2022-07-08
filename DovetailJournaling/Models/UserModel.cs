/*
 * File Name: UserModel.cs
 * Dovetail Journaling
 * 
 * Purpose: This is the "main" model. There are others that act as complements,
 * but this is the model responsible for most business logic and data persistence.
 * This model sets up the other models when they do perform their functionality 
 * (e.g. passing along the repository object to an instance of a subclass of 
 * IJournalEntryComponent for it to perform data persistence).
 * 
 * This model is oblivious to the type of data storage thanks to the repository pattern.
 * It is only aware that it can use its repository object, which is a subclass of IUserRepository,
 * to perform certain operations related to data persistence and retrieval.
 * See: https://docs.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design
 * 
 * This model is meant to have the Controller set it up with an instance of the GraphDatabaseRepositoryProxy 
 * class, so that the logging functionality of the proxy class can be used.
 * 
 * The composite pattern is also evident in this class. Whenever this model receives a
 * subclass of the IJournalEntryComponent interface, it does not need to know whether it's
 * a singular journal entry or a list of journal entries.
 * 
 * Author: Emily Ramanna
 * Contact: ramannae@mcmaster.ca
 */

using System;
using System.Collections.Generic;
using System.Security.Authentication;

namespace DovetailJournaling.Models
{
    class UserModel
    {
        // Other classes should not be able to set, has "getter" method: CheckAuthentication
        private bool _isAuthenticated;

        // These are set by the Controller,
        // but other classes should not "reach through" the model to access,
        // so that's why there are no getters
        private IAnalysisService analysisService;
        public IAnalysisService AnalysisService
        {
            set => analysisService = value;
        }

        private IUserRepository userRepository;
        public IUserRepository UserRepository
        {
            set => userRepository = value;
        }

        // Properties
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public string ContactInfo { get; set; }


        // Returns true if the current user has successfully logged in
        public bool CheckAuthentication()
        {
            return _isAuthenticated;
        }

        public bool Login()
        {
            // tries to get a user with the username specified
            UserModel userRetrieved = userRepository.GetUserByUserName(UserName);

            // if there is no such user, or the password entered is not correct,
            // then login attempt failed
            if (userRetrieved is null ||
                !PasswordHelper.ArePasswordHashesTheSame(userRetrieved.Password,
                Password, userRetrieved.Salt))
            {
                return false;
            }

            // if login successful, set the password proprty to the hashed value,
            // and set the authenticated flag to true
            Password = PasswordHelper.ComputeHashedValue(Password, Salt);
            _isAuthenticated = true;
            return true;
        }

        // returns this UserModel object to an unauthenticated state
        public void Logout()
        {
            UserName = null;
            Password = null;
            Salt = null;
            ContactInfo = null;
            _isAuthenticated = false;
        }

        public void SaveNewUser()
        {
            // sees if there is already a user that exists with the username specified
            UserModel retrieved = userRepository.GetUserByUserName(UserName);

            // if it's an original username...
            if (retrieved is null)
            {
                // hash and salt the password
                Salt = PasswordHelper.CreateSaltValue();
                Password = PasswordHelper.ComputeHashedValue(Password, Salt);

                // use the repo object to persist the new account
                userRepository.CreateNewUser(this);
            }
            else
            {
                throw new ArgumentException("That username is already taken!");
            }

        }

        public bool UpdatePassword(string newPassword)
        {
            if (_isAuthenticated)
            {
                // salt and hash new password
                string newSalt = PasswordHelper.CreateSaltValue();
                string newHashedPassword = PasswordHelper.ComputeHashedValue(newPassword, newSalt);

                // use the repo object to update the password in the database
                bool updateSuccess = userRepository.UpdateUserPassword(UserName, newHashedPassword, newSalt);

                // sets property values if update was successful
                if (updateSuccess)
                {
                    Salt = newSalt;
                    Password = newHashedPassword;
                    return true;
                }
                return false;
            }
            else
            {
                throw new AuthenticationException("User must be logged in to change their password");
            }

        }

        public bool UpdateContactInfo(string newContactInfo)
        {
            if (_isAuthenticated)
            {
                // use the repo object to update the contact info in the database
                bool updateSuccess = userRepository.UpdateUserContactInfo(UserName, newContactInfo);

                // sets property value if update was successful
                if (updateSuccess)
                {
                    ContactInfo = newContactInfo;
                    return true;
                }
                return false;
            }
            else
            {
                throw new AuthenticationException("User must be logged in to change their contact information");
            }

        }

        // Creates and persists a new singular journal entry
        public IJournalEntryComponent CreateJournalEntry(string entryContent)
        {
            if (_isAuthenticated)
            {
                // Creates a new journal entry with content entered and current timestamp
                JournalEntryModel createdEntry = new JournalEntryModel(entryContent, DateTime.Now);

                // passes along the AnalysisService so the journal entry can use the API to learn 
                // its topics and sentiments
                createdEntry.LearnTopicsAndSentiments(analysisService);

                // passes along the Repository object so that the journal entry can be saved
                createdEntry.SaveJournalEntry(userRepository, UserName);

                return createdEntry;
            }
            else
            {
                throw new AuthenticationException("User must be logged in to create a journal entry");
            }

        }

        // Gets journal entries based on a date
        // Doesn't need to know if it's getting a singular entry or a list,
        // thanks to the composite pattern.
        public IJournalEntryComponent RetrieveJournalEntries(DateTime date)
        {
            if (_isAuthenticated)
            {
                return userRepository.GetJournalEntries(date, UserName);
            }
            else
            {
                throw new AuthenticationException("User must be logged in to retrieve journal entries");
            }

        }

        // Deletes chosen journal entry
        public void DeleteJournalentry(IJournalEntryComponent entries, int index)
        {
            if (_isAuthenticated)
            {
                // passes along the Repository object to the Journal Entry object
                // so that the deletion can be persisted
                entries.DeleteJournalEntry(userRepository, UserName, index);
            }
            else
            {
                throw new AuthenticationException("User must be logged in to delete journal entries");
            }
        }

        // Gets a list of other users who shared a similar sentiment within past week
        public List<UserContactModel> GetSameSentimentList(string sentiment)
        {
            if (_isAuthenticated)
            {
                return userRepository.GetSameSentimentList(UserName, sentiment);
            }
            else
            {
                throw new AuthenticationException("User must be logged in to view other users with same sentiment");
            }
        }

        // Gets a list of GUIDs associated with the user
        public List<UserContactModel> GetAssociatedGUIDS()
        {
            if (_isAuthenticated)
            {
                return userRepository.GetUserContactRelationships(UserName);
            }
            else
            {
                throw new AuthenticationException("User must be logged in to view associated GUIDs");
            }

        }

        // Creates and persists a new GUID for 2 users that share the same sentiment
        public string CreateGUID(UserContactModel userContact)
        {
            if (_isAuthenticated)
            {
                userContact.GUID = Guid.NewGuid().ToString();
                userRepository.CreateNewUserContact(UserName, userContact);

                return userContact.GUID;
            }
            else
            {
                throw new AuthenticationException("User must be logged in to create a GUID");
            }
        }
    }
}
