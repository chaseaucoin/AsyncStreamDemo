using System.IO.Compression;
using System.IO;
using System.Text.Json;
using System.IO.Pipes;

namespace SuperChargedStreams.Utils
{
    public class FileCache
    {
        private readonly string _cacheDirectory = "asyncCache";

        public FileCache()
        {
            if(!Directory.Exists(_cacheDirectory))
                Directory.CreateDirectory(_cacheDirectory);
        }

        //public async Task SaveAsync<T>(string key, IAsyncEnumerable<T> data)
        //{
        //    var filePath = GetFilePath(key);
        //    using var stream = File.Create(filePath);
        //    await JsonSerializer.SerializeAsync(stream, data);
        //}

        //public async IAsyncEnumerable<T> LoadAsync<T>(string key)
        //{
        //    var filePath = GetFilePath(key);
        //    if (!File.Exists(filePath))
        //        yield break;

        //    using var stream = File.OpenRead(filePath);
        //    var data = JsonSerializer.DeserializeAsyncEnumerable<T>(stream);
        //    if (data != null)
        //    {
        //        await foreach (var item in data)
        //        {
        //            yield return item;
        //        }
        //    }
        //}

        public async Task SaveAsync<T>(string key, IAsyncEnumerable<T> data)
        {
            var filePath = GetFilePath(key);
            using var fileStream = File.Create(filePath);
            
            // Create a GZipStream to compress data into the FileStream.
            await using var gzipStream = new GZipStream(fileStream, CompressionLevel.Fastest);

            await JsonSerializer.SerializeAsync(gzipStream, data);
        }

        public async IAsyncEnumerable<T> LoadAsync<T>(string key)
        {
            var filePath = GetFilePath(key);
            if (!File.Exists(filePath))
                yield break;

            using var fileStream = File.OpenRead(filePath);
            // Create a GZipStream to compress data into the FileStream.
            await using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);

            var data = JsonSerializer.DeserializeAsyncEnumerable<T>(gzipStream);
            if (data != null)
            {
                await foreach (var item in data)
                {
                    yield return item;
                }
            }
        }

        public bool Exists(string key)
        {
            var filePath = GetFilePath(key);
            return File.Exists(filePath);
        }

        private string GetFilePath(string key)
        {
            var fileName = $"{key}.json";
            return Path.Combine(_cacheDirectory, fileName);
        }
    }

}
