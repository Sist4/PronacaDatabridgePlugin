using Newtonsoft.Json;
using RestSharp;
using System;

namespace PronacaApi
{
    public  class DbaApi
    {
        public dynamic Post(string url, string json, string autorizacion = null)
        {
            try
            {
                var client = new RestClient(url);
                var request = new RestRequest(Method.POST);
                request.AddHeader("content-type", "application/json");
                request.AddParameter("application/json", json, ParameterType.RequestBody);

                if (autorizacion != null)
                {
                    request.AddHeader("Authorization", autorizacion);
                }

                IRestResponse response = client.Execute(request);

                dynamic datos = JsonConvert.DeserializeObject(response.Content);

                return datos;
            }
            catch (Exception ex)
            {
               // MessageBox.Show(ex.Message);
                return null;
            }
        }

        
    }
}
