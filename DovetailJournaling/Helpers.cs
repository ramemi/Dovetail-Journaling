/*
 * File Name: Helpers.cs
 * Dovetail Journaling
 * 
 * Purpose: Static utility classes that provide functionality that
 * don't need to be instantiated themselves.
 * 
 * Author: Emily Ramanna
 * Contact: ramannae@mcmaster.ca
 */
using System;
using System.Security.Cryptography;
using System.Text;

namespace DovetailJournaling
{
    // Add any string processing to this class
    static class StringHelper
    {
        // This is used to escape user input before adding it to the database,
        // which is part of the mitigation of injection attacks
        public static string EscapeString(string candidateString)
        {
            return candidateString.Replace("\"", "\\\"");

        }
    }

    //Class adapted from Bamidele Alegbe's answer from:
    //https://stackoverflow.com/questions/2138429/hash-and-salt-passwords-in-c-sharp
    static class PasswordHelper
    {
        public static string CreateSaltValue()
        {
            // creates a buffer for 128 bit salt value
            byte[] saltValue = new byte[16];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                // fills the buffer
                rng.GetBytes(saltValue);
            }

            return Convert.ToBase64String(saltValue);
        }

        public static string ComputeHashedValue(string password, string saltValue)
        {
            // puts together and encodes password and salt
            byte[] combinedPasswordAndSalt = Encoding.UTF8.GetBytes(password + saltValue);

            SHA256Managed sHA256Managed = new SHA256Managed();
            // Computes the SHA256 hash of password+salt
            byte[] hashedValue = sHA256Managed.ComputeHash(combinedPasswordAndSalt);

            return Convert.ToBase64String(hashedValue);
        }

        // Processes and compares plaintext password with salted and hashed password
        public static bool ArePasswordHashesTheSame(string hashedPassword, string enteredPassword, string salt)
        {
            string hashedEnteredPassword = ComputeHashedValue(enteredPassword, salt);
            return hashedEnteredPassword.Equals(hashedPassword);
        }
    }
}
