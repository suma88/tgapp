using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MyAuditApplication.DAO
{
    public static class MongoDBConnector
    {
        private static MongoClientSettings settings = new MongoClientSettings { Server = new MongoServerAddress(ConfigurationManager.AppSettings["MongoDBServer"], Convert.ToInt32(ConfigurationManager.AppSettings["MongoDBPort"])) };
        private static IMongoClient client = new MongoClient(settings);
        private static IMongoDatabase database = client.GetDatabase(ConfigurationManager.AppSettings["MongoDBDatabaseName"].ToString());

        public static async Task insert<BSONDocument>(BSONDocument doc, string collectionName)
        {
            var collection = database.GetCollection<BSONDocument>(collectionName);
            await collection.InsertOneAsync(doc);
        }

        private static async Task<Task<T>> findOne<T>(string collectionName, FilterDefinition<T> filterCriteria)
        {
            var collection = database.GetCollection<T>(collectionName);
            return await Task.FromResult(collection.Find(filterCriteria).Sort("{Timestamp:-1}").FirstOrDefaultAsync());

        }

        public static T FindOne<T>(string collectionName, FilterDefinition<T> filterCriteria)
        {
            return findOne<T>(collectionName, filterCriteria).GetAwaiter().GetResult().GetAwaiter().GetResult();

        }
        private static async Task<Task<List<T>>> Find<T>(string collectionName, FilterDefinition<T> filterCriteria)
        {

            var collection = database.GetCollection<T>(collectionName);
            return await Task.FromResult(collection.Find(filterCriteria).ToListAsync());

        }

        public static List<T> find<T>(string collectionName, BsonDocument filterCriteria)
        {

            return Find<T>(collectionName, filterCriteria).GetAwaiter().GetResult().GetAwaiter().GetResult();

        }

        //public static async Task<List<T>> Find<T>(string collectionName,FilterDefinition<T> filterCriteria )
        //{
        //    var collection = database.GetCollection<T>(collectionName);
        //    return await collection.Find(filterCriteria).ToListAsync();

        //}
        public static async Task ReplaceOne<T>(string collectionName, FilterDefinition<T> filterCriteria, T replacementDoc)
        {
            IMongoCollection<T> collection = database.GetCollection<T>(collectionName);
            await collection.ReplaceOneAsync(filterCriteria, replacementDoc);
        }
    }
}