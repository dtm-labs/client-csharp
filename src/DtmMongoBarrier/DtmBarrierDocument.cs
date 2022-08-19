namespace DtmMongoBarrier
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    internal class DtmBarrierDocument
    {
        [MongoDB.Bson.Serialization.Attributes.BsonElement("trans_type")]
        public string TransType { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement("gid")]
        public string GId { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement("branch_id")]
        public string BranchId { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement("op")]
        public string Op { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement("barrier_id")]
        public string BarrierId { get; set; }

        [MongoDB.Bson.Serialization.Attributes.BsonElement("reason")]
        public string Reason { get; set; }
    }
}