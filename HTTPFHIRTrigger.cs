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
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

namespace Microsoft.fhirdemo
{
    public static class HTTPFHIRTrigger
    {
        [FunctionName("HTTPFHIRTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var exceptions = new List<Exception>();
            string authority = Environment.GetEnvironmentVariable("Authority");
            string audience = Environment.GetEnvironmentVariable("Audience");
            string clientId = Environment.GetEnvironmentVariable("ClientId");
            string clientSecret = Environment.GetEnvironmentVariable("ClientSecret");
            Uri fhirServerUrl = new Uri(Environment.GetEnvironmentVariable("FhirServerUrl"));

            log.LogInformation("C# HTTP trigger function processed a request.");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var ed = JsonConvert.DeserializeObject<FHIREventData>(requestBody);
            AlgorithmFormat af = new AlgorithmFormat();
            try
                {                    
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
                        if(response.IsSuccessStatusCode){
                            var c = await response.Content.ReadAsStringAsync();
                            var fhirParser = new FhirJsonParser();

                            DiagnosticReport parsedDR = fhirParser.Parse<DiagnosticReport>(c);
                            log.LogInformation("Parsed DiagnosticReport: " + parsedDR.Status);
                            //foreach (var refObservation in parsedDR.)
                            
                            foreach ( var obsResult in parsedDR.Result)
                            {
                                //fetch observation:
                                fhirEndpoint = fhirServerUrl + "/" + obsResult.Reference;
                                HttpRequestMessage obsRequest = new HttpRequestMessage(HttpMethod.Get, fhirEndpoint);
                                obsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                                obsRequest.Headers.Add("Accept", "application/fhir+json");
                                obsRequest.Headers.Add("Prefer", "respond-async");
                                HttpResponseMessage obsResponse = await newClient.SendAsync(obsRequest);
                                if(obsResponse.IsSuccessStatusCode)
                                {
                                    var o = await obsResponse.Content.ReadAsStringAsync();
                                    Observation parsedObs = fhirParser.Parse<Observation>(o);
                                    //process the observation
                                    //There are smarter ways than a switch to do this but for the sake of easy code I went with this.
                                    switch(parsedObs.Code.Coding[0].Code)
                                    {
                                        case "000-0":
                                            af.WCC = ((Quantity) parsedObs.Value).Value.ToString();
                                            break;
                                        case "777-3":
                                            af.PLT =((Quantity) parsedObs.Value).Value.ToString();
                                            break;
                                    }
                                }
                            }
                        }   
                    }
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }



            string jsonString = JsonConvert.SerializeObject(af);

            string responseMessage = string.IsNullOrEmpty(requestBody)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"The following JSON string can be send to an algorithm for further processing {jsonString}";

            return new OkObjectResult(responseMessage);
        }
    }
}
