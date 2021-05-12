using BlobRepositoryDemo.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;

namespace BlobRepositoryDemo.Server.Data
{
    public class BlobDataManager<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private List<TEntity> Data = new List<TEntity>();
        private PropertyInfo IdProperty = null;
        private string IdPropertyName = "";
        private AzureStorageHelper AzureStorageHelper = null;
        private SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);
        private string AzureBlobStorageConnectionString = "";
        private string AzureParentContainerUrl = "";
        private string ContainerName = "";
        private string DataFolder = "";
        private string DataFileName = "";
        public DateTime LastAccessTime;
        private int MinutesToCache = 5;

        public BlobDataManager(
            string azureBlobStorageConnectionString,
            string azureParentContainerUrl,
            string containerName,
            string idPropertyName,
            int minutesToCache)
        {
            // We need this to pass to the AzureStorageHelper
            AzureBlobStorageConnectionString = azureBlobStorageConnectionString;
            // The public url to the blob storage parent directory
            AzureParentContainerUrl = azureParentContainerUrl;
            // The container name where files for this entity will go
            ContainerName = containerName;
            // We need to know the name of the primary key property
            IdPropertyName = idPropertyName;
            // How many minutes between reads
            MinutesToCache = minutesToCache;
            // This gets the PropertyInfo object for the PK property
            IdProperty = typeof(TEntity).GetProperty(idPropertyName);
            // Create the AzureStorageHalper from the connection string
            AzureStorageHelper = new AzureStorageHelper(AzureBlobStorageConnectionString);
            // Create a Json folder for storing local json files
            DataFolder = $"{Environment.CurrentDirectory}\\Json";
            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);
            }
            // The file name in the local Json directory
            DataFileName = $"{DataFolder}\\{typeof(TEntity).Name}.json";
            // Load data from the blobs
            var t = Task.Run(() => LoadData());
            t.Wait();
        }

        /// <summary>
        /// Loads data from the Azure blob file
        /// </summary>
        /// <returns></returns>
        private async Task LoadData()
        {
            using var http = new HttpClient();
            try
            {
                string url = $"{AzureParentContainerUrl}{ContainerName}/{typeof(TEntity).Name}.json";
                string json = await http.GetStringAsync(url);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    // Success! Make the data accessible
                    Data = JsonConvert.DeserializeObject<List<TEntity>>(json);
                    // Reset the access time
                    LastAccessTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }

        /// <summary>
        /// Thread-safe way to save data when it changes
        /// </summary>
        /// <returns></returns>
        private async Task SaveData()
        {
            // SemaphoreSlim only allows one caller at a time to save the file
            await SemaphoreSlim.WaitAsync();
            try
            {
                // serialize
                var json = JsonConvert.SerializeObject(Data);
                // write to local file
                File.WriteAllText(DataFileName, json);
                // upload to blob storage
                await AzureStorageHelper.UploadFileInChunks(ContainerName,
                    DataFileName, Path.GetFileName(DataFileName));
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }


        public async Task<bool> Delete(TEntity EntityToDelete)
        {
            if (EntityToDelete == null) return false;

            try
            {
                if (Data.Contains(EntityToDelete))
                {
                    Data.Remove(EntityToDelete);
                    await SaveData();// save after deleting entity
                    return true;
                }
            }
            catch { }

            return false;

        }

        public async Task<bool> Delete(object Id)
        {
            try
            {
                var EntityToDelete = await GetById(Id);
                // pass it on to the other Delete method
                return await Delete(EntityToDelete);

            }
            catch { }

            return false;

        }

        // This is really only in here for demo purposes.
        // I would not allow this in production. Too risky
        public async Task DeleteAll()
        {
            Data.Clear();
            await SaveData(); // Careful, now!
        }

        /// <summary>
        /// This Get allows you to do advanced queries. 
        /// It is only to be used on the server side.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="orderBy"></param>
        /// <param name="includeProperties"></param>
        /// <returns></returns>
        public async Task<IEnumerable<TEntity>> Get(Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null, string includeProperties = "")
        {
            // If [MinutesToCache] minutes have elapsed, re-load the data
            var diff = DateTime.Now.Subtract(LastAccessTime).TotalMinutes;
            if (diff >= MinutesToCache)
            {
                await LoadData();
            }

            return await Task.Run(() =>
            {
                try
                {
                    // Get the dbSet from the Entity passed in                
                    IQueryable<TEntity> query = Data.AsQueryable<TEntity>();

                    // Apply the filter
                    if (filter != null)
                    {
                        query = query.Where(filter);
                    }

                    // Sort
                    if (orderBy != null)
                    {
                        return orderBy(query).ToList();
                    }
                    else
                    {
                        return query.ToList();
                    }
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                    return null;
                }

            });
        }

        /// <summary>
        /// This code has been redone to use reflection to look up 
        /// the value of the PK so we can use entities that have Int32
        /// PKs or Guid PKs or even string PKs.
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<TEntity> GetById(object Id)
        {
            // If [MinutesToCache] minutes have elapsed, re-load the data
            var diff = DateTime.Now.Subtract(LastAccessTime).TotalMinutes;
            if (diff >= MinutesToCache)
            {
                await LoadData();
            }

            // IdProperty will be used, so check for nulls
            if (IdProperty == null) return default(TEntity);
            TEntity entity = null;
            // Probably not necessary, but I have it in here just to show it can be done.
            if (IdProperty.PropertyType.IsValueType)
            {
                // use PropertyInfo.GetValue in the query
                entity = (from x in Data 
                          where IdProperty.GetValue(x).ToString() == Id.ToString() 
                          select x).FirstOrDefault();
            }
            else
            {
                // the PK is a reference type. You probably will never need this.

                // use PropertyInfo.GetValue in the query
                entity = (from x in Data 
                          where IdProperty.GetValue(x) == Id 
                          select x).FirstOrDefault();
            }
            return entity;
        }

        public async Task<TEntity> Insert(TEntity Entity)
        {
            if (Entity == null) return default(TEntity);
            try
            {
                Data.Add(Entity);
                await SaveData();
                return Entity;
            }
            catch { }
            return default(TEntity);
        }

        public async Task<TEntity> Update(TEntity EntityToUpdate)
        {
            if (EntityToUpdate == null) return default(TEntity);
            if (IdProperty == null) return default(TEntity);

            try
            {
                // first we need the PK value
                var id = IdProperty.GetValue(EntityToUpdate);
                // next we need the entity from Data 
                // with the same PK, because the values will be different
                var entity = await GetById(id);

                if (entity != null)
                {
                    // Now we need the index of that entity
                    var index = Data.IndexOf(entity);
                    // so we can replace it
                    Data[index] = EntityToUpdate;
                    await SaveData();
                    return EntityToUpdate;
                }
                else
                    return default(TEntity);
            }
            catch { }
            return default(TEntity);
        }
        
        public async Task<IEnumerable<TEntity>> GetAll()
        {
            // If [MinutesToCache] minutes have elapsed, re-load the data
            var diff = DateTime.Now.Subtract(LastAccessTime).TotalMinutes;
            if (diff >= MinutesToCache)
            {
                await LoadData();
            }
            return Data;
        }
    }
}
