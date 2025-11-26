using MongoDB.Driver;
using UserFeed.Domain.Entities;
using UserFeed.Domain.Ports;
using UserFeed.Infrastructure.Configuration;

namespace UserFeed.Infrastructure.Adapters.Persistence;

public class MongoUserCommentRepository : IUserCommentRepository
{
    private readonly IMongoCollection<UserComment> _collection;

    public MongoUserCommentRepository(MongoDbSettings settings)
    {
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);
        _collection = database.GetCollection<UserComment>(settings.CollectionName);

        // Crear índices
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        var indexKeys = Builders<UserComment>.IndexKeys.Ascending(c => c.ArticleId);
        _collection.Indexes.CreateOne(new CreateIndexModel<UserComment>(indexKeys));

        var userIndexKeys = Builders<UserComment>.IndexKeys.Ascending(c => c.UserId);
        _collection.Indexes.CreateOne(new CreateIndexModel<UserComment>(userIndexKeys));
    }

    public async Task<UserComment?> GetByIdAsync(string id)
    {
        var filter = Builders<UserComment>.Filter.Eq(c => c.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<UserComment>> GetByArticleIdAsync(string articleId, int page = 1, int pageSize = 10)
    {
        var filter = Builders<UserComment>.Filter.And(
            Builders<UserComment>.Filter.Eq(c => c.ArticleId, articleId),
            Builders<UserComment>.Filter.Eq(c => c.IsDeleted, false)
        );
        
        return await _collection.Find(filter)
            .SortByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserComment>> GetByUserIdAsync(string userId, int page = 1, int pageSize = 10)
    {
        var filter = Builders<UserComment>.Filter.And(
            Builders<UserComment>.Filter.Eq(c => c.UserId, userId),
            Builders<UserComment>.Filter.Eq(c => c.IsDeleted, false)
        );
        
        return await _collection.Find(filter)
            .SortByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();
    }

    public async Task<UserComment?> GetByUserAndArticleAsync(string userId, string articleId)
    {
        var filter = Builders<UserComment>.Filter.And(
            Builders<UserComment>.Filter.Eq(c => c.UserId, userId),
            Builders<UserComment>.Filter.Eq(c => c.ArticleId, articleId),
            Builders<UserComment>.Filter.Eq(c => c.IsDeleted, false)
        );
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<UserComment> CreateAsync(UserComment comment)
    {
        await _collection.InsertOneAsync(comment);
        return comment;
    }

    public async Task<UserComment> UpdateAsync(UserComment comment)
    {
        await _collection.ReplaceOneAsync(c => c.Id == comment.Id, comment);
        return comment;
    }

    public async Task DeleteAsync(string id)
    {
        var comment = await GetByIdAsync(id);
        if (comment != null)
        {
            comment.Delete();
            await UpdateAsync(comment);
        }
    }

}
