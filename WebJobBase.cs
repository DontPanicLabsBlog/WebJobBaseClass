using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SendGrid;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPL
{
    public abstract class WebJobBase
    {
        protected static JobHost GetJobHost(string webJobName)
        {
            JobHost host = new JobHost(GetConfiguration(webJobName));

            return host;
        }

        protected static JobHostConfiguration GetConfiguration(string webJobName)
        {
            JobHostConfiguration config = new JobHostConfiguration();
            
            // Timers
            bool isTimersEnabled = bool.Parse(ConfigurationManager.AppSettings["WebJobsTimersEnabled"]);

            if (isTimersEnabled)
            {
                config.UseTimers();
            }
            
            // ServiceBus
            CreateStartSubscription(webJobName);
            config.UseServiceBus();

            // Set Configs
            config.NameResolver = new ServiceBusTopicResolver(webJobName);
            config.UseServiceBus();

            config.UseCore();

            return config;
        }

        private static void CreateStartSubscription(string webJobName)
        {
            NamespaceManager nsMgr = NamespaceManager.CreateFromConnectionString(ConfigurationManager.ConnectionStrings["AzureWebJobsServiceBus"].ConnectionString);
            string environment = ConfigurationManager.AppSettings["WebJobsEnvName"];
            string topic = ConfigurationManager.AppSettings["WebJobsTopicName"];
            string subscription = $"{webJobName}{environment}StartMessages";

            if (nsMgr.SubscriptionExists(topic, subscription))
            {
                nsMgr.DeleteSubscription(topic, subscription);
            }

            SqlFilter startMessagesFilter = new SqlFilter($"Environment = '{environment}' AND JobName='{webJobName}'");
           
            nsMgr.CreateSubscription(topic, subscription, startMessagesFilter);
        }
    }

    public class ServiceBusTopicResolver : INameResolver
    {
        private string _webJobName;

        public ServiceBusTopicResolver(string webJobName)
        {
            _webJobName = webJobName;
        }

        public string Resolve(string key)
        {
            string result = "";

            if (key == "SubscriptionName")
            {
                string environment = ConfigurationManager.AppSettings["WebJobsEnvName"];
                string subscription = $"{_webJobName}{environment}StartMessages";

                result = subscription;
            }
            else
            {
                result = ConfigurationManager.AppSettings[key];
            }

            return result;
        }
    }
}
