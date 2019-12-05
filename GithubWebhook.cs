using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Company.Function
{
    public static class GithubWebhook
    {
        [FunctionName("GithubWebhook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("GitHub webhook received");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var fileList = new List<string>();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            foreach( dynamic commit in data.commits ) {
                foreach( dynamic fileAdded in commit.added ) {
                    string fileName = (string)fileAdded;
                    log.LogInformation(fileName.Split('.')[fileName.Split('.').Length-1]);
                    
                    if ( fileName.Split('.')[fileName.Split('.').Length-1] == "png" ) {
                        fileList.Add(fileName);
                    }
                }
            }

            string repoUrl = data.repository.html_url;
            var db_images  = Environment.GetEnvironmentVariable("db_images");
            if (fileList.Count > 0) {
                using (var connection = new SqlConnection(db_images)) {
                    string sql = @"INSERT INTO CHRISTMAS_PNG (PNG_PATH) SELECT @PNGPATH";
                    foreach( string pngpath in fileList) {
                        using (SqlCommand command = new SqlCommand(sql, connection))
                        {
                            connection.Open();
                            var parampngpath = new SqlParameter("PNGPATH", SqlDbType.VarChar);
                            parampngpath.Value = repoUrl+ "/raw/master/" + pngpath;
                            command.Parameters.Add(parampngpath);
                            
                            var results = command.ExecuteNonQuery();
                        }
                    }
                    connection.Close();
                }
            }

            return new OkObjectResult("success");

        }
    }
}
