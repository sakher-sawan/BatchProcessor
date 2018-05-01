/**********************************************************************************
 *   Copyright 2017-2018 Mohamed Sakher Sawan                                     *
 *                                                                                *
 *   Licensed under the Apache License, Version 2.0 (the "License");              *
 *   you may not use this file except in compliance with the License.             *
 *   You may obtain a copy of the License at                                      *
 *                                                                                *
 *       http://www.apache.org/licenses/LICENSE-2.0                               *
 *                                                                                *
 *   Unless required by applicable law or agreed to in writing, software          *
 *   distributed under the License is distributed on an "AS IS" BASIS,            *
 *   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.     *
 *   See the License for the specific language governing permissions and          *
 *   limitations under the License.                                               *
***********************************************************************************/

using Amazon.S3;
using BatchProcessor.Config;
using BatchProcessor.Logging;
using System;
using System.IO;

namespace DataStore.AWS.S3
{
    public class S3DataStore
    {
        private string bucketName;
        public S3DataStore(Enum bucketNameConfigKey)
        {
            bucketName = ConfigurationManager.AppSettings[bucketNameConfigKey];
        }
        public void Delete(string key)
        {
            Amazon.S3.AmazonS3Client client = getS3Client();
            var resTask = client.DeleteObjectAsync(new Amazon.S3.Model.DeleteObjectRequest()
            {
                BucketName = bucketName,
                Key = key
            });
            resTask.Wait();
            if (resTask.Result.HttpStatusCode != System.Net.HttpStatusCode.OK
                && resTask.Result.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new Exception("Delete failed, error code: " + resTask.Result.HttpStatusCode, resTask.Exception);
            }
        }

        public bool ResultExists(string key)
        {
            try
            {
                var resTask = getS3Client().GetObjectMetadataAsync(bucketName, key);
                resTask.Wait();
                return true;
            }
            catch (System.AggregateException e)
            {
                var ex = e.InnerExceptions[0] as AmazonS3Exception;
                if (ex == null)
                {
                    throw ex;
                }
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false;
                }
                throw e;
            }
        }

        public byte[] Get(string key)
        {
            Amazon.S3.AmazonS3Client client = getS3Client();
            var resTask = client.GetObjectAsync(bucketName, key);
            try
            {
                resTask.Wait();
            }
            catch (AggregateException ex)
            {
                BatchProcessor.Logging.Logger.Error(ex, "Error retreiving an item from S3 bucket, bucket name=" + bucketName + ", item key=" + key);
                //TODO: Check the first exception, and make sure that it is 404 not found.
                return null;
            }
            byte[] res;
            using (resTask.Result.ResponseStream)
            {
                using (MemoryStream mem = new MemoryStream())
                {
                    resTask.Result.ResponseStream.CopyTo(mem);
                    res = mem.ToArray();
                }
            }
            return res;
        }

        public void Store(string key, byte[] data)
        {
            Amazon.S3.AmazonS3Client client = getS3Client();
            var resTask = client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest()
            {
                BucketName = bucketName,
                Key = key,
                InputStream = new MemoryStream(data),
            });
            bool error = false;
            Exception innerException = null;
            try
            {
                resTask.Wait();
            }
            catch (Exception ex)
            {
                innerException = ex;
            }
            if (error || resTask.Result.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                if (innerException == null)
                {
                    innerException = resTask.Exception;
                }
                var newEx = new Exception("Store failed, error code: " + resTask.Result.HttpStatusCode, innerException);

                Logger.Error(newEx, "Error storing in S3 bucket, bucket name=" + bucketName + ", key=" + key);
                throw newEx;
            }
        }

        private static Amazon.S3.AmazonS3Client getS3Client()
        {
            return new Amazon.S3.AmazonS3Client(Amazon.RegionEndpoint.EUWest1);
        }
    }
}
