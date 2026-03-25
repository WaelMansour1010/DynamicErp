using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using Amazon.Runtime;
using Amazon;
using System.Threading.Tasks;

namespace MyERP.Utils
{
    public class AmazonHelper
    {
        AmazonS3Client client = new AmazonS3Client(new BasicAWSCredentials("AKIA6NGZYTRRXPJJC2GM", "2FhxdVeZxH4ao50MShsLIOqoc6cnih5TO/j2jPvx"), RegionEndpoint.USEast2);

        string BucketName = "mysoftecom";

        public GetObjectResponse getObject(string fileName)
        {
            try
            {
                // Create a client

                // Create a GetObject request
                GetObjectRequest request = new GetObjectRequest
                {
                    BucketName = BucketName,
                    Key = fileName
                };

                // Issue request and remember to dispose of the response
                using (GetObjectResponse response = client.GetObject(request))
                {
                    //using (StreamReader reader = new StreamReader(response.ResponseStream))
                    //{
                    //    string contents = reader.ReadToEnd();
                    //    Console.WriteLine("Object - " + response.Key);
                    //    Console.WriteLine(" Version Id - " + response.VersionId);
                    //    Console.WriteLine(" Contents - " + contents);
                    //}
                    return response;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool WritingAnObject(string fileName, string img)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(img.Split(',')[1]);

                using (var ms = new MemoryStream(bytes))
                {
                    // 1. Put object-specify only key name for the new object.
                    var putRequest1 = new PutObjectRequest
                    {
                        BucketName = BucketName,
                        Key = fileName,
                        CannedACL = S3CannedACL.PublicRead,
                    };

                    putRequest1.InputStream = ms;
                    PutObjectResponse response1 = client.PutObject(putRequest1);
                }

            }
            catch (AmazonS3Exception e)
            {
                return false;
                //  Console.WriteLine("Error encountered ***. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                return false;
                //  Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            return true;
        }

        public bool DeleteAnObject(string fileName)
        {
            try
            {
                var deleteRequest1 = new DeleteObjectRequest
                {
                    BucketName = BucketName,
                    Key = fileName,
                };

                client.DeleteObject(deleteRequest1);
            }
            catch (AmazonS3Exception e)
            {
                return false;
                //  Console.WriteLine("Error encountered ***. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                return false;
                //  Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            return true;
        }
    }
}