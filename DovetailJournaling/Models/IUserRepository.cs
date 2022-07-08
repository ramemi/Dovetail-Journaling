/*
 * File Name: IUserRepository.cs
 * Dovetail Journaling
 * 
 * Purpose: Define the methods that persistent data storage
 * classes need to provide.
 * 
 * The interface of the repository pattern. Both "real" and proxy
 * data persistence objects will implement this interface.
 * 
 * This is the repository interface that the UserModel is aware of.
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
    // defines the methods that repository classes must provide
    interface IUserRepository
    {
        bool CreateNewUser(UserModel userModel);
        bool UpdateUserPassword(string username, string newHashedPassword, string newSalt);
        bool UpdateUserContactInfo(string username, string newContactInfo);
        UserModel GetUserByUserName(string username);
        bool AddJournalEntry(IJournalEntryComponent entry, string username);
        bool DeleteJournalEntry(IJournalEntryComponent entry, string username);
        IJournalEntryComponent GetJournalEntries(DateTime date, string username);
        List<UserContactModel> GetSameSentimentList(string username, string sentiment);
        bool CreateNewUserContact(string username, UserContactModel userContact);
        List<UserContactModel> GetUserContactRelationships(string username);
    }
}
