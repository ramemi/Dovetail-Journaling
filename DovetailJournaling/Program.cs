/*
 * File Name: Program.cs
 * Dovetail Journaling
 * 
 * Purpose: Allow users to manage an account and their own journal entries.
 * Also allows users to find other users that expressed a similar sentiment on a topic
 * in one of their journal entries.
 *
 * This starts the application by creating the core MVC objects,
 * the proxy object of the user repository, and the service responsible
 * for accessing the sentiment analysis API.
 * 
 * Author: Emily Ramanna
 * Contact: ramannae@mcmaster.ca
 */

using DovetailJournaling.Models;

namespace DovetailJournaling
{
    class Program
    {
        static void Main(string[] args)
        {
            Controller controller = new Controller(new MeaningCloudAnalysisService(),
                new GraphDatabaseUserRepositoryProxy(), new UserModel(), new View());

            controller.Run();
        }
    }
}
