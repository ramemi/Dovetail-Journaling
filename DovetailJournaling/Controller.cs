/*
 * File Name: Controller.cs
 * Dovetail Journaling
 * 
 * Purpose: Use the View object to get input from and give output
 * to the user. Also requests updates from the UserModel object where appropriate.
 * Handles flow of the application.
 * 
 * Author: Emily Ramanna
 * Contact: ramannae@mcmaster.ca
 */

using DovetailJournaling.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DovetailJournaling
{
    class Controller
    {
        public Controller(IAnalysisService service, IUserRepository repo, UserModel model, View view)
        {
            AnalysisService = service;
            UserRepository = repo;
            UserModel = model;
            View = view;

            // Injects the UserModel's Analysis service and Repository objects
            // via property setters
            UserModel.AnalysisService = service;
            UserModel.UserRepository = repo;
        }

        // Public properties
        public IAnalysisService AnalysisService { get; set; }
        public IUserRepository UserRepository { get; set; }
        public UserModel UserModel { get; set; }
        public View View { get; set; }

        /*CONTROLS MENUS*/

        // Uses View to show the start page with options to create an account, login, exit
        public void Run()
        {
            View.DisplayMessage("Dovetail Journaling Start:\n");
            while (true)
            {
                int option = View.StartPage();

                if (option == 1)
                {
                    CreateAccount();
                }
                else if (option == 2)
                {
                    Login();
                }
                else
                {
                    Environment.Exit(0);
                }
            }
        }

        // Uses view to display the main menu with options:
        // go to journal entry menu, go to account management menu, logout
        private void MainMenu()
        {
            while (true)
            {
                int option = View.MainPage();

                if (option == 1)
                {
                    JournalEntryMenu();
                }
                else if (option == 2)
                {
                    AccountManagementMenu();
                }
                else
                {
                    UserModel.Logout();
                    break;
                }
            }
        }

        // Uses View to show the account management menu with options to change password or
        // contact info or to go back
        private void AccountManagementMenu()
        {
            while (true)
            {
                int option = View.AccountManagementPage();

                if (option == 1)
                {
                    string newPassword = View.PromptForHiddenInput("Please enter your new password");
                    if (UserModel.UpdatePassword(newPassword))
                    {
                        View.DisplayMessage("Password successfully changed");
                    }
                    else
                    {
                        View.DisplayMessage("Password could not be updated");
                    }

                }
                else if (option == 2)
                {
                    string newContactInfo = View.PromptForShortStringInput("Please enter a new piece of contact info");
                    if (UserModel.UpdateContactInfo(newContactInfo))
                    {
                        View.DisplayMessage("Contact info successfully changed");
                    }
                    else
                    {
                        View.DisplayMessage("Contact info could not be updated");
                    }
                }
                else
                {
                    break;
                }
            }
        }

        // Uses View to show the journal entry management menu with options:
        // create, view, delete journal entries, view GUIDs associated with you,
        // view a list of users with similar sentiment
        private void JournalEntryMenu()
        {
            while (true)
            {
                int option = View.JournalEntryManagementPage();

                if (option == 1)
                {
                    CreateJournalEntry();
                }
                else if (option == 2)
                {
                    // sets deleteflag to flase, as the user only wants to view
                    ViewOrDeleteJournalEntry(false);
                }
                else if (option == 3)
                {
                    // sets deleteflag to true, as the user has opted to delete
                    ViewOrDeleteJournalEntry(true);
                }
                else if (option == 4)
                {
                    AssociatedGUIDS();
                }
                else if (option == 5)
                {
                    SameSentiment();
                }
                else
                {
                    break;
                }
            }
        }

        // Uses view to ask the user to select the type of sentiment they
        // wish to find that they have in common with other users
        private void SameSentiment()
        {
            int option = View.TypeOfSentimentPage();
            string sentimentChosen;

            // uses singleton to keep values of the types of sentiment consistent
            if (option == 1)
            {
                sentimentChosen = AppConfigData.Config.PositiveSentiment;
            }
            else if (option == 2)
            {
                sentimentChosen = AppConfigData.Config.NegativeSentiment;
            }
            else
            {
                sentimentChosen = AppConfigData.Config.NeutralSentiment;
            }

            // Uses the model to get the list of user contacts with a similar sentiment
            List<UserContactModel> sameSentimentList = UserModel.GetSameSentimentList(sentimentChosen);

            // Uses the view to request the user to select a user to generate a GUID for
            // "-1" done to get the index in the list
            option = View.SameSentimentList(sameSentimentList) - 1;

            if (option < sameSentimentList.Count && option > -1)
            {
                UserModel.CreateGUID(sameSentimentList[option]);

                // Shows GUID generated by the model in the previous line
                View.DisplayMessage(sameSentimentList[option].UserName + ", " + sameSentimentList[option].GUID);
            }
        }

        /*OTHER USER INTERACTION*/

        private void Login()
        {
            UserModel.UserName = View.PromptForShortStringInput("Please enter your username").ToLower();
            UserModel.Password = View.PromptForHiddenInput("Please enter your password");

            bool isLoginSuccessful = UserModel.Login();

            if (isLoginSuccessful)
            {
                View.DisplayMessage("Welcome, " + UserModel.UserName + "!");
                MainMenu();
            }
            else
            {
                View.DisplayMessage("Login unsuccessful");
            }
        }

        // Gets and displays a list of GUIDs that are associated to the user
        private void AssociatedGUIDS()
        {
            List<UserContactModel> associatedGUIDS = UserModel.GetAssociatedGUIDS();
            View.DisplayUserContactList(associatedGUIDS);
        }

        // The process of deleting a journal entry includes viewing a journal entry
        // The deleteflag is set to true when the user has opted to delete
        private void ViewOrDeleteJournalEntry(bool deleteflag)
        {
            // set prompt text depending on if the user has selected to delete
            string promptText = deleteflag ? "Please enter the date you made the journal entry you wish to delete" 
                : "Please enter the date for the journal entries you wish to view";

            // Parses a date entered by the user
            DateTime date = DateTime.Parse(View.PromptForDateInput(promptText), new CultureInfo("en-US", true));

            // Gets entries by date and displays
            IJournalEntryComponent entries = UserModel.RetrieveJournalEntries(date);
            View.DisplayMessage(entries.ToString());

            // If user has selected to delete...
            if (deleteflag)
            {
                // If they had not made any entries on the date they entered...
                if (entries.Count == 0)
                {
                    View.DisplayMessage("Sorry, there are no entries available to delete on that date");
                }
                else
                {
                    // prompt user to select which entry to delete
                    int option = View.PromptForIntegerInput("Please enter the journal entry number you wish to delete") - 1;
                    UserModel.DeleteJournalentry(entries, option);
                }
               
            }
        }

        // Prompt, create and display new journal entry
        private void CreateJournalEntry()
        {
            string content = View.PromptForLongStringInput("Please type your journal entry");
            IJournalEntryComponent createdEntry = UserModel.CreateJournalEntry(content);
            View.DisplayMessage(createdEntry.ToString());
        }

        // Prompt for info and create new user account
        private void CreateAccount()
        {
            UserModel.UserName = View.PromptForShortStringInput("Please enter a username for your new account").ToLower();
            UserModel.Password = View.PromptForHiddenInput("Please enter a password for your new account");
            UserModel.ContactInfo = View.PromptForShortStringInput("Please enter a piece of contact information");

            try
            {
                UserModel.SaveNewUser();
                View.DisplayMessage("Account created successfully");
            }
            // Exception occurs when the user name is already taken
            catch (ArgumentException ex)
            {
                View.DisplayMessage(ex.Message);
            }
        }
    }
}
