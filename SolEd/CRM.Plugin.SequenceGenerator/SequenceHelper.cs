using System;
using System.Collections.ObjectModel;
using System.Collections;
using System.Reflection;

using Microsoft.Crm.Sdk.Messages;

using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Crm.Sdk;


namespace CRM.Plugin.SequenceGenerator {
    public class SequenceHelper {

        public const string SchemaName = "ce_counter";//Auto Number Entity Name

        public class Fields
        {
            public const string Id = "ce_counterid";
            public const string EntityName = "ce_name";
            public const string PropertyName = "new_propertyname";
            public const string CurrentPosition = "ce_nextnumber";
            public const string Prefix = "ce_prefix";
            public const string Postfix = "ce_postfix";
        }

        public Guid Id { get; set; }
        public string EntityName { get; set; }
        public string PropertyName { get; set; }
        public int CurrentPosition { get; set; }
        public string Prefix { get; set; }
        public string Postfix { get; set; }

        public SequenceHelper() { }

        public SequenceHelper(Entity entity)
        {
            this.Id = entity.Id;
            this.EntityName = entity[Fields.EntityName].ToString();
            this.PropertyName = entity[Fields.PropertyName].ToString();
            this.CurrentPosition = int.Parse(entity[Fields.CurrentPosition].ToString());
            
            if (entity.Contains(Fields.Prefix))
            {
                this.Prefix = entity[Fields.Prefix].ToString();
            }
            else {
                this.Prefix = "";
            }

            if (entity.Contains(Fields.Postfix))
            {
                this.Postfix = entity[Fields.Postfix].ToString();
            }
            else
            {
                this.Postfix = "";
            }
        }

        public void Increment(IOrganizationService service, int next)
        {
            this.CurrentPosition = next; // set before calling ToDynamic

            Entity entity = new Entity(SequenceHelper.SchemaName);
            
            entity[Fields.Id] = this.Id;
            entity[Fields.CurrentPosition] = this.CurrentPosition;

            service.Update(entity);
        }

        // see if there are any increment settings for this entity
        public static SequenceHelper GetSettings(IOrganizationService service, string entityName)
        {
            SequenceHelper setting = null;

            QueryByAttribute query = new QueryByAttribute();
            query.Attributes.AddRange(new string[] { SequenceHelper.Fields.EntityName });
            query.ColumnSet = new ColumnSet(new string[] { SequenceHelper.Fields.Id, SequenceHelper.Fields.EntityName, SequenceHelper.Fields.PropertyName, SequenceHelper.Fields.CurrentPosition, SequenceHelper.Fields.Prefix, SequenceHelper.Fields.Postfix });
            query.EntityName = SequenceHelper.SchemaName;
            query.Values.AddRange(new object[] { entityName });

            //Execute using a request to test the OOB (XRM) message contracts
            RetrieveMultipleRequest request = new RetrieveMultipleRequest();
            request.Query = query;
            Collection<Entity> entityList = ((RetrieveMultipleResponse)service.Execute(request)).EntityCollection.Entities;

            if (entityList.Count > 0) // no error checkig to see if there are 2 incrementors set for the same entity
            {
                setting = new SequenceHelper(entityList[0]);
            }

            return setting;
        }

    }
}
