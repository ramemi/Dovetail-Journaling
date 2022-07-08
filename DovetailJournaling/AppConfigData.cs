/*
 * File Name: AppConfigData.cs
 * Dovetail Journaling
 * 
 * Purpose: Implement the singleton pattern to manage global read-only state.
 * 
 * When the single instance is instantiated, it reads in a json file using the Json.Net parser from Newtonsoft.
 * See: https://www.newtonsoft.com/json/help/html/ReadJson.htm
 * 
 * This singleton makes the following read-only configuration data available throughout the application:
 * - Redis database name
 * - The string values corresponding to positive, negative, and neutral sentiment used in the database
 * - The URL and key for the sentiment analysis API
 * 
 * The singleton also makes available one redisgraph database connection that can be used throughout the 
 * application. It does this by reading in the database host, port, and password information from the json
 * file.
 * 
 * Currently, the sentiment analysis API used is provided by MeaningCloud:
 * https://learn.meaningcloud.com/developer/sentiment-analysis/2.1/doc/what-is-sentiment-analysis
 * 
 * 
 * This code was adapted from the second version listed here: https://csharpindepth.com/articles/singleton
 * 
 * Author: Emily Ramanna
 * Contact: ramannae@mcmaster.ca
 */

using Newtonsoft.Json.Linq;
using RedisGraphDotNet.Client;
using StackExchange.Redis;
using System.IO;

namespace DovetailJournaling
{
    class AppConfigData
    {
        // The singular instance
        private static AppConfigData _config = null;

        // Lock used for thread-safety
        private static readonly object _singletonLock = new object();

        // RedisGraphClient connection
        public RedisGraphClient GraphDB { get; }

        // Configuration information, only getters for read-only info
        public string DBName { get; }
        public string ApiUrl { get; }
        public string ApiKey { get; }
        public string PositiveSentiment { get; }
        public string NegativeSentiment { get; }
        public string NeutralSentiment { get; }

        // private constructor that is only called once
        private AppConfigData()
        {
            // Read the json file
            JObject obj = JObject.Parse(File.ReadAllText("config.json"));

            // Set up the redisgraph database connection with host, port and password values read in from file
            ConnectionMultiplexer conn = ConnectionMultiplexer.Connect(obj.GetValue("databaseHost").ToString() +
                ":" + obj.GetValue("databasePort").ToString() + ", password=" + obj.GetValue("databasePassword").ToString());
            GraphDB = new RedisGraphClient(conn);

            // Set up the rest of the configuration data
            DBName = obj.GetValue("databaseName").ToString();
            PositiveSentiment = obj.GetValue("positiveSentiment").ToString();
            NegativeSentiment = obj.GetValue("negativeSentiment").ToString();
            NeutralSentiment = obj.GetValue("neutralSentiment").ToString();
            ApiUrl = obj.GetValue("APIURL").ToString();
            ApiKey = obj.GetValue("APIKey").ToString();
        }

        // Static method that calls the private constructor if there is no existing instance
        public static AppConfigData Config
        {
            get
            {
                // lock to prevent multiple threads from instantiating
                lock (_singletonLock)
                {
                    // if no instance, create one => call private constructor
                    if (_config is null)
                    {
                        _config = new AppConfigData();
                    }

                    // returns instance
                    return _config;
                }
            }
        }
    }
}
