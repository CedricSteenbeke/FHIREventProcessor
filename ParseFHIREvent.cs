using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Microsoft.fhirdemo
{
    public static class ParseFHIREvent
    {
        
        [FunctionName("ParseFHIREvent")]
        public static async System.Threading.Tasks.Task Run([EventHubTrigger("fhirevents31hub", Connection = "fhirevents31_RootManageSharedAccessKey_EVENTHUB")] EventData[] events, ILogger log)
        {
            
            var exceptions = new List<Exception>();
            string authority = Environment.GetEnvironmentVariable("Authority");
            string audience = Environment.GetEnvironmentVariable("Audience");
            string clientId = Environment.GetEnvironmentVariable("ClientId");
            string clientSecret = Environment.GetEnvironmentVariable("ClientSecret");
            Uri fhirServerUrl = new Uri(Environment.GetEnvironmentVariable("FhirServerUrl"));

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: "+ messageBody);

                    var ed = JsonConvert.DeserializeObject<FHIREventData>(messageBody as string);
                    
                    var fhirEndpoint = fhirServerUrl + "/" + ed.resourcetype + "/" + ed.id;
                    log.LogInformation("Endpoint: " + fhirEndpoint);
                    if(ed.resourcetype == "DiagnosticReport" && ed.action == "Created"){
                        var authContext = new AuthenticationContext(authority);
                        var clientCredential = new ClientCredential(clientId, clientSecret);
                        var authResult = authContext.AcquireTokenAsync(audience, clientCredential).Result;

                        HttpClient newClient = new HttpClient();
                        HttpRequestMessage newRequest = new HttpRequestMessage(HttpMethod.Get, fhirEndpoint);
                        newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                        newRequest.Headers.Add("Accept", "application/fhir+json");
                        newRequest.Headers.Add("Prefer", "respond-async");
                        //Read Server Response
                        HttpResponseMessage response = await newClient.SendAsync(newRequest);
                        log.LogInformation(response.ToString());
                        string content = "";
                        if(response.IsSuccessStatusCode){
                            var c = await response.Content.ReadAsStringAsync();

                            //using(var reader = new System.IO.StreamReader(await response.Content.ReadAsStringAsync()))
                            
                            
                            var fhirParser = new FhirJsonParser();
                            try
                            {
                                DiagnosticReport parsedObservation = fhirParser.Parse<DiagnosticReport>(c);
                                log.LogInformation("Parsed DiagnosticReport: " + parsedObservation.Status);
                            }
                            catch (Exception ex)
                            {
                                // We need to keep processing the rest of the batch - capture this exception and continue.
                                // Also, consider capturing details of the message that failed processing so it can be processed again later.
                                exceptions.Add(ex);
                            }
                                
                            
                            log.LogInformation(content);
                        }
                        
                    }
                    
                    await System.Threading.Tasks.Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }

    }
}
