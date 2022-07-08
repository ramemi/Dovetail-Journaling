/*
 * File Name: View.cs
 * Dovetail Journaling
 * 
 * Purpose: Handles user interaction. Displays options and
 * output to the user. Prompts for and returns input from
 * the user to the Controller.
 * 
 * Author: Emily Ramanna
 * Contact: ramannae@mcmaster.ca
 */

using DovetailJournaling.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DovetailJournaling
{
    class View
    {
        /* DISPLAY MENUS WITH NUMERICAL OPTIONS */

        public int StartPage()
        {
            Console.WriteLine("\n-------------------------------------------------------------");
            Console.WriteLine("(1) Create Account");
            Console.WriteLine("(2) Login");
            Console.WriteLine("(3) Quit");
            return Convert.ToInt32(Console.ReadLine());
        }

        public void AccountCreationPage()
        {
            Console.WriteLine("\n-------------------------------------------------------------");
            Console.WriteLine("Welcome to the account creation page!");
            Console.WriteLine("You will be prompted for a username, a password, and a piece of contact information.");
        }

        public int MainPage()
        {
            Console.WriteLine("\n-------------------------------------------------------------");
            Console.WriteLine("(1) Journal Entry Menu");
            Console.WriteLine("(2) Account Management Menu");
            Console.WriteLine("(3) Logout");
            return Convert.ToInt32(Console.ReadLine());
        }

        public int AccountManagementPage()
        {
            Console.WriteLine("\n-------------------------------------------------------------");
            Console.WriteLine("(1) Change Password");
            Console.WriteLine("(2) Change Contact Information");
            Console.WriteLine("(3) Back");
            return Convert.ToInt32(Console.ReadLine());
        }

        public int JournalEntryManagementPage()
        {
            Console.WriteLine("\n-------------------------------------------------------------");
            Console.WriteLine("(1) Create Journal Entry");
            Console.WriteLine("(2) View Journal Entry");
            Console.WriteLine("(3) Delete Journal Entry");
            Console.WriteLine("(4) View GUIDS Associated With You");
            Console.WriteLine("(5) Find Others With Similar Sentiment");
            Console.WriteLine("(6) Back");
            return Convert.ToInt32(Console.ReadLine());
        }

        public int TypeOfSentimentPage()
        {
            Console.WriteLine("\n-------------------------------------------------------------");
            Console.WriteLine("Which type of sentiment do you want to look for?");
            Console.WriteLine("(1) Positive");
            Console.WriteLine("(2) Negative");
            Console.WriteLine("(3) Neutral");
            return Convert.ToInt32(Console.ReadLine());
        }

        /* PROMPTS FOR DIFFERENT TYPES OF INPUT */
        // These handle some of the formatting issues, like
        // whether or not to include a newline or prompting for
        // how the date should be entered.
        // If this were to be converted to a website, then the view would
        // take care of this through elements like a DatePicker.

        public string PromptForShortStringInput(string message)
        {
            Console.Write(message + ": ");
            return Console.ReadLine();
        }

        public string PromptForDateInput(string message)
        {
            Console.Write(message + " (MM/dd/yyyy): ");
            return Console.ReadLine();
        }

        public string PromptForLongStringInput(string message)
        {
            Console.Write(message + ":\n");
            return Console.ReadLine();
        }

        public int PromptForIntegerInput(string message)
        {
            Console.Write(message + ": ");
            return Convert.ToInt32(Console.ReadLine());
        }

        // Hides input from console to avoid shoulder surfing, used for password entry
        public string PromptForHiddenInput(string message)
        {
            Console.Write(message + ": ");
            StringBuilder input = new StringBuilder();

            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    break;
                }
                else if (keyInfo.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input.Remove(input.Length - 1, 1);
                }
                else if (keyInfo.Key != ConsoleKey.Backspace)
                {
                    input.Append(keyInfo.KeyChar);
                }
            }
            Console.WriteLine();
            return input.ToString();
        }

        /* DISPLAY MESSAGES AND LISTS */

        public void DisplayMessage(string message)
        {
            Console.WriteLine("\n" + message);
        }

        public void DisplayUserContactList(List<UserContactModel> userContacts)
        {
            // if the list is empty...
            if (userContacts.Count == 0)
            {
                Console.WriteLine("There have been no GUIDs associated to you this past week\n");
            }
            else
            {
                int i = 1;
                // formats the printing of a UserContact
                foreach (var userContact in userContacts)
                {
                    Console.WriteLine(i + ") username: " + userContact.UserName);
                    Console.WriteLine("\tcontact: " + userContact.ContactInfo);
                    Console.WriteLine("\ttopic: " + userContact.Topic.Keyword);
                    Console.WriteLine("\tsentiment: " + userContact.Topic.Sentiment);
                    Console.WriteLine("\tGUID: " + userContact.GUID + "\n");
                    i++;
                }
            }
        }

        public int SameSentimentList(List<UserContactModel> sameSentimentList)
        {
            // if the list is empty...
            if (sameSentimentList.Count == 0)
            {
                Console.WriteLine("There are no other users with this type of sentiment about the same topics as you\n");
                return 0;
            }
            else
            {
                // prompt the user to select a user from the list
                Console.WriteLine("Select a user to get their contact information and generate a unique" +
                    " GUID that you can use as proof that you both had the same sentiment about a topic if you reach out to them.\n\n");

                // displays an enumerated list for the user to choose from
                int i = 1;
                foreach (var userContact in sameSentimentList)
                {
                    Console.WriteLine(i + ") Username: " + userContact.UserName + ", Topic: " + userContact.Topic.Keyword + ", Sentiment: " + userContact.Topic.Sentiment);
                    i++;
                }

                // Adds the "back" option
                Console.WriteLine(i + ") Go Back");

                return Convert.ToInt32(Console.ReadLine());
            }
        }
    }
}
