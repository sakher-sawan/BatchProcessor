using BatchProcessing.Logging;
using BatchProcessorConfig;
using System;
using System.IO;

namespace BatchProcessor.FileSystemStore
{
    internal class FileSystemStore
    {
        private string storePath;
        public FileSystemStore(Enum storeNameKey)
        {
            storePath = ConfigurationManager.AppSettings[storeNameKey];
        }
        public void Delete(string key)
        {
            try
            {
                File.Delete(Path.Combine(storePath, key));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public bool ResultExists(string key)
        {
            try
            {
                return File.Exists(Path.Combine(storePath, key));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public byte[] Get(string key)
        {
            try
            {
                return File.ReadAllBytes(Path.Combine(storePath, key));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        public void Store(string key, byte[] data)
        {
            try
            {
                File.WriteAllBytes(Path.Combine(storePath, key), data);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }
    }
}
