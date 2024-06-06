using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CosmosDbService
{
    private Container _container;

    public CosmosDbService(CosmosClient cosmosClient, string databaseName, string containerName)
    {
        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task<IEnumerable<VideoGame>> GetVideoGamesAsync(string queryString)
    {
        var query = _container.GetItemQueryIterator<VideoGame>(new QueryDefinition(queryString));
        List<VideoGame> results = new List<VideoGame>();
        while (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            results.AddRange(response.ToList());
        }
        return results;
    }

    public async Task<VideoGame> GetVideoGameAsync(string id)
    {
        try
        {
            ItemResponse<VideoGame> response = await _container.ReadItemAsync<VideoGame>(id, new PartitionKey(id));
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<VideoGame> AddVideoGameAsync(VideoGame videoGame)
    {
        ItemResponse<VideoGame> response = await _container.CreateItemAsync<VideoGame>(videoGame, new PartitionKey(videoGame.Id));
        return response.Resource;
    }

    public async Task UpdateVideoGameAsync(string id, VideoGame videoGame)
    {
        await _container.UpsertItemAsync(videoGame, new PartitionKey(id));
    }

    public async Task DeleteVideoGameAsync(string id)
    {
        await _container.DeleteItemAsync<VideoGame>(id, new PartitionKey(id));
    }
}
