using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MyAuditApplication.DAO;
using MyAuditApplication.Entities;
using MyAuditApplication.Models;
using MyAuditApplication.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyAuditApplication.BusinessLayer
{
    public static class ObjectBL
    {
        public static Result SaveObjects(List<Object> objects)
        {
            Result result = new Result() { Code = ResultCode.Success };
            try
            {
                foreach (Object obj in objects)
                {
                    //new object to be saved...
                    BsonDocument doc = GetBSONDocument(obj);

                    //get if any existing object from db... 
                    //FilterDefinition<BsonDocument> fc = Builders<BsonDocument>.Filter.Eq(Constants.AttribObjectId,obj.Id);
                    FilterDefinition<BsonDocument> fc = Builders<BsonDocument>.Filter.And(new FilterDefinition<BsonDocument>[] {
                    Builders<BsonDocument>.Filter.Eq(Constants.AttribObjectId,obj.Id),
                    Builders<BsonDocument>.Filter.Lt(Constants.AttribTimestamp,new BsonTimestamp((long)obj.Timestamp))
                    });

                    BsonDocument existing = MongoDBConnector.FindOne<BsonDocument>(obj.Type, fc);
                    //find - ObjectId, order by timestamp desc top 1
                    if (existing != null)
                    {
                        existing.Remove("_id");
                        foreach (BsonElement e in doc.Elements)
                        {
                            existing[e.Name] = e.Value;
                        }
                        doc = existing;
                    }
                    Task.Run(() => MongoDBConnector.insert<BsonDocument>(doc, obj.Type)).Wait();

                    #region "Update future records"
                    //to update future records if any..
                    BsonDocument filterCriteria = new BsonDocument();
                    filterCriteria.Add(Constants.AttribObjectId, obj.Id);
                    filterCriteria.Add(Constants.AttribTimestamp, new BsonDocument().Add("$gte", new BsonTimestamp((long)obj.Timestamp)));
                    List<BsonDocument> docs = null;
                    try
                    {
                        docs = MongoDBConnector.find<BsonDocument>(obj.Type, filterCriteria);
                    }
                    catch
                    {
                        //no docs found...
                    }
                    if (docs != null)
                    {
                        foreach (BsonDocument futureDoc in docs)
                        {
                            bool changed = false;
                            foreach (BsonElement e in doc.Elements)
                            {
                                if (futureDoc.Elements.Where(fe => fe.Name == e.Name).Count() == 0)
                                {
                                    futureDoc.Add(e);
                                    changed = true;
                                }
                            }
                            if (changed)
                            {
                                fc = Builders<BsonDocument>.Filter.Eq("_id", futureDoc.GetElement("_id").Value);
                                Task.Run(() => MongoDBConnector.ReplaceOne<BsonDocument>(obj.Type, fc, futureDoc)).Wait();
                            }

                        }
                    }
                    #endregion
                }
            }
            catch
            {
                result.Code = ResultCode.Error;
            }

            return result;
        }

        public static BsonDocument GetBSONDocument(Object obj)
        {

            JObject jObj = JObject.Parse(JToken.Parse(obj.Changes).ToString());
            jObj.Add(Constants.AttribObjectId, obj.Id);
            BsonDocument bDoc = BsonDocument.Parse(jObj.ToString());
            bDoc.Add(new BsonElement(Constants.AttribTimestamp, new BsonTimestamp((long)obj.Timestamp)));
            return bDoc;
        }

        public static ObjectState FindObjectState(string type, string id, double timestamp)
        {
            ObjectState objStateResult = new ObjectState() { Code = ResultCode.Success };
            FilterDefinition<BsonDocument> fc = Builders<BsonDocument>.Filter.And(new FilterDefinition<BsonDocument>[]
            {
                 Builders<BsonDocument>.Filter.Eq(Constants.AttribObjectId,id),
                 Builders<BsonDocument>.Filter.Lte(Constants.AttribTimestamp,new BsonTimestamp ((long)timestamp))
            });
            BsonDocument objState;
            try
            {
                objState = MongoDBConnector.FindOne<BsonDocument>(type, fc);
            }
            catch
            {
                objStateResult.Code = ResultCode.Error;
                return objStateResult;
            }
            if (objState != null)
            {
                if (type.Equals("Invoice"))
                {
                    //find the latest order record...
                    BsonElement beOrderId = objState.Elements.Where(e => e.Name == "order_id").FirstOrDefault();
                    if (beOrderId != null)
                    {
                        fc = Builders<BsonDocument>.Filter.Eq("ObjectId", beOrderId.Value.ToString());
                        BsonDocument bOrder = MongoDBConnector.FindOne<BsonDocument>("Order", fc);
                        if (bOrder != null)
                        {
                            bOrder.Remove("_id");
                            bOrder.Remove(Constants.AttribTimestamp);
                            bOrder.Remove(Constants.AttribObjectId);
                            objState.Add("OrderDetails", bOrder);
                            
                        }
                    }
                    //find the latest product records...
                    BsonElement beProductIds = objState.Elements.Where(e => e.Name == "product_ids").FirstOrDefault();
                    if (beProductIds != null)
                    {
                        IEnumerable<string> productIds = ((BsonArray)beProductIds.Value).Select(e => e.ToString()).Distinct();
                        
                        BsonDocument filterCriteria = new BsonDocument();
                        filterCriteria.Add("ObjectId",new BsonDocument("$in", new BsonArray(productIds)));


                        IEnumerable<BsonDocument> bProducts = MongoDBConnector.find<BsonDocument>("Product", filterCriteria);
                        if (bProducts != null && bProducts.Count() > 0)
                        {
                            bProducts = bProducts.GroupBy(d => d.GetElement(Constants.AttribObjectId).Value)
                                .Select(grp => grp.OrderByDescending(d1 => d1.GetElement(Constants.AttribTimestamp)).First());

                           
                            BsonArray baProducts = new BsonArray(bProducts);
                            foreach (BsonDocument bdoc in baProducts)
                            {
                                bdoc.Remove("_id");
                                bdoc.Remove(Constants.AttribTimestamp);
                            }
                            objState.Add(new BsonElement("Products", baProducts));
                        }
                    }

                }

                objState.Remove("_id");
                objState.Remove(Constants.AttribTimestamp);
                objState.Remove(Constants.AttribObjectId);
                objStateResult.state = objState.ToJson().Replace(":","=>");
            }
            else
            {
                objStateResult.Code = ResultCode.NoRecordFound;
                return objStateResult;
            }
            return objStateResult;
        }
    }
}