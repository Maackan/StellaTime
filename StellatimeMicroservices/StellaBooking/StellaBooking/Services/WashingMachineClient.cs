using StellaBooking.Models;

namespace StellaBooking.Services
{
    public class WashingMachineClient
    {
        private HttpClient _httpClient;

        public WashingMachineClient(HttpClient httpClient) 
        {
            _httpClient = httpClient;
        }

        public async Task<WashingMachine> GetWashingMachine(string washingMachineId)
        {
            return await _httpClient.GetFromJsonAsync<WashingMachine>($"/machine/{washingMachineId}");
        }
    }
}
