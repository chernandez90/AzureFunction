using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System;
using Dapper;
using Microsoft.Azure.Functions.Worker;

public static class RunStoredProcedure
{
    private static readonly string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

    [FunctionName("RunStoredProcedure")]
    public static async Task<IActionResult> Run(
        [Microsoft.Azure.WebJobs.HttpTrigger(Microsoft.Azure.WebJobs.Extensions.Http.AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        dynamic data = JsonConvert.DeserializeObject(requestBody);

        string firstname = data?.firstname;
        string lastname = data?.lastname;
        DateTime dob = data?.dob != null ? DateTime.Parse((string)data.dob) : DateTime.MinValue;
        string phone = data?.phone;

        if (string.IsNullOrEmpty(firstname) || string.IsNullOrEmpty(lastname) || dob == DateTime.MinValue || string.IsNullOrEmpty(phone))
        {
            return new BadRequestObjectResult("Please provide firstname, lastname, dob, and phone in the request body.");
        }

        try
        {
            await CallStoredProcedure(firstname, lastname, dob, phone);
            return new OkObjectResult("Stored procedure executed successfully.");
        }
        catch (Exception ex)
        {
            log.LogError($"Error executing stored procedure: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task CallStoredProcedure(string firstname, string lastname, DateTime dob, string phone)
    {
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            await conn.OpenAsync();
            var parameters = new { FirstName = firstname, LastName = lastname, DOB = dob, Phone = phone };
            await conn.ExecuteAsync("InsertCustomer", parameters, commandType: System.Data.CommandType.StoredProcedure);
        }
    }
}
