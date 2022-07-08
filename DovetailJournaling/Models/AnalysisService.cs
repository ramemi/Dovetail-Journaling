/*
 * File Name: AnalysisService.cs
 * Dovetail Journaling
 * 
 * Purpose: Handles the interaction with the sentiment analysis
 * API.
 * 
 * For more info about MeaningCloud see:
 * https://learn.meaningcloud.com/developer/sentiment-analysis/2.1/doc/what-is-sentiment-analysis
 * 
 * This is included in the "Models" because interaction with the API is a domain concept,
 * which means that its logic is a component of the application's "Model" portion.
 * 
 * Author: Emily Ramanna
 * Contact: ramannae@mcmaster.ca
 */
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;

namespace DovetailJournaling.Models
{
    // Defines the operations that can be performed with the sentiment
    // analysis service. Other classes only have to know about this 
    // interface and don't need to be concerned about the specific API
    // used.
    interface IAnalysisService
    {
        List<TopicModel> GetTopicsAndSentiments(string content);
    }

    // implements IAnalysisService with the MeaningCloud API
    class MeaningCloudAnalysisService : IAnalysisService
    {
        public List<TopicModel> GetTopicsAndSentiments(string content)
        {
            List<TopicModel> learnedTopics = new List<TopicModel>();

            // initialize a new RestClient
            // for more details about RestSharp, see: https://restsharp.dev/
            var client = new RestClient(AppConfigData.Config.ApiUrl +
                "key=" + AppConfigData.Config.ApiKey +
                "&of=json&txt=" + Uri.EscapeDataString(content) + "&lang=en");
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);

            // execute HTTP POST request to API
            IRestResponse response = client.Execute(request);

            // the returned data
            dynamic analysisData = JsonConvert.DeserializeObject<dynamic>(response.Content);

            // portion of returned data containing topics and sentiments
            dynamic extractedTopics = analysisData.sentimented_concept_list;

            // sets up Topic models with information from API
            for (int i = 0; i < extractedTopics.Count; i++)
            {
                TopicModel topic = new TopicModel();
                topic.Keyword = (Convert.ToString(extractedTopics[i]["form"])).ToLower();
                topic.Sentiment = ((Convert.ToString(extractedTopics[i]["score_tag"])).ToLower()).Replace("+", "");

                // adds topic to list
                learnedTopics.Add(topic);
            }
            return learnedTopics;
        }
    }
}