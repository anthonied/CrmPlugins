using System;
using System.Globalization;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace CRM.Plugin.SequenceGenerator
{
    public class SequenceGenerator : IPlugin
    {

        private static readonly object SyncLock = new object();
        /// <summary>
        /// A plugin that auto generates the number for entities configured
        /// </summary>
        /// <remarks>
        /// Register this plug-in on the Create message and pre-event stage.
        /// </remarks>
        /// <param name="serviceProvider"></param>
        public void Execute(IServiceProvider serviceProvider)
        {
            // Obtain the execution context from the service provider.
            var context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            var serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var service = serviceFactory.CreateOrganizationService(context.UserId);

            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity))
            {
                tracingService.Trace(
                       "AutoNumber Pluing: The configuration is invalid. Target does not exist. Please contact adminsitrator with the issue.");
                throw new InvalidPluginExecutionException(
                    "AutoNumber Pluing: The configuration is invalid. Target does not existd. Please contact adminsitrator with the issue.");
            }

            var entity = context.InputParameters["Target"] as Entity;
            
            try
            {
                if (context.IsInTransaction)
                {
                    lock (SyncLock)
                    {
                        if (entity != null)
                        {

                            var setting = SequenceHelper.GetSettings(service, entity.LogicalName);

                            if (setting != null && !entity.Attributes.Contains(setting.PropertyName))
                            {
                                var next = setting.CurrentPosition + 1;
                                var nextAlternateClientNumber = "";
                                if (setting.Prefix != "" && setting.Postfix != "")
                                {
                                    tracingService.Trace("The configuration is invalid. Prefix and Postfix are both provided. Please contact adminsitrator with the issue.");
                                    throw new InvalidPluginExecutionException("The configuration is invalid. Prefix and Postfix are both provided. Please contact adminsitrator with the issue.");
                                }
                                if (setting.Prefix != "")
                                {
                                    var numberOfSpacingCharacters = setting.Prefix.Contains("#") ? setting.Prefix.Split('#').Length - 1 : 1;
                                    var cleanPrefix = setting.Prefix.Replace("#", "");
                                    nextAlternateClientNumber = cleanPrefix + next.ToString(CultureInfo.InvariantCulture).PadLeft(numberOfSpacingCharacters, '0');
                                }
                                entity[setting.PropertyName] = nextAlternateClientNumber;                                
                                setting.Increment(service, next);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                tracingService.Trace("AutoNumber Plugin Exception: " + ex);
                throw new InvalidPluginExecutionException(ex.ToString());
            }
        }
    }
}
