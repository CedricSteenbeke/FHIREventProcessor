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
            string authority = Environment.GetEnvironmentVariable("Authority");
            string audience = Environment.GetEnvironmentVariable("Audience");
            string clientId = Environment.GetEnvironmentVariable("ClientId");
            string clientSecret = Environment.GetEnvironmentVariable("ClientSecret");
            Uri fhirServerUrl = new Uri(Environment.GetEnvironmentVariable("FhirServerUrl"));

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var ed = JsonConvert.DeserializeObject<FHIREventData>(requestBody);
            AlgorithmFormat af = new AlgorithmFormat();
            try
            {
                var fhirEndpoint = fhirServerUrl + "/" + ed.resourcetype + "/" + ed.id;
                log.LogInformation("Endpoint: " + fhirEndpoint);
                if (ed.resourcetype == "DiagnosticReport" && ed.action == "Created")
                {
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
                    // For demoing log the body of the HTTP request.
                    log.LogInformation(response.ToString());

                    if (response.IsSuccessStatusCode)
                    {
                        var c = await response.Content.ReadAsStringAsync();
                        var fhirParser = new FhirJsonParser();
                        //fetch full Diagnostic Report and loop the associated Observations
                        DiagnosticReport parsedDR = fhirParser.Parse<DiagnosticReport>(c);
                        foreach (var obsResult in parsedDR.Result)
                        {
                            //fetch observation
                            fhirEndpoint = fhirServerUrl + "/" + obsResult.Reference;
                            HttpRequestMessage obsRequest = new HttpRequestMessage(HttpMethod.Get, fhirEndpoint);
                            obsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                            obsRequest.Headers.Add("Accept", "application/fhir+json");
                            obsRequest.Headers.Add("Prefer", "respond-async");
                            HttpResponseMessage obsResponse = await newClient.SendAsync(obsRequest);
                            if (obsResponse.IsSuccessStatusCode)
                            {
                                // Store observation value in the AF object.
                                var o = await obsResponse.Content.ReadAsStringAsync();
                                Observation parsedObs = fhirParser.Parse<Observation>(o);
                                string obsCode = parsedObs.Code.Coding[0].Code;
                                string obsValue = ((Quantity)parsedObs.Value).Value.ToString();
                                af.setObservation(obsCode, obsValue);
                            }
                        }
                        //return flat format for now as a browser response. 
                        // In the future here we'd implement the call to the back-end of choice.

                        return new OkObjectResult(JsonConvert.SerializeObject(af));
                    }else{
                        return new NotFoundObjectResult($"Unable to find {ed.resourcetype} with id: {ed.id}");
                    }
                }
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
                return new BadRequestObjectResult("something went wrong processing the request.");
            }

            return new BadRequestObjectResult("Unfortunatley something went wrong. Please check the logs.");
        }
    }
}
