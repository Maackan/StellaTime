using StellaBooking.Models;
using System.Net.Http.Headers;
namespace StellaBooking.Services
{
    public class WashingMachineClient
    {
        private HttpClient _httpClient;

        public WashingMachineClient(HttpClient httpClient) 
        {
            _httpClient = httpClient;
        }

        public async Task<WashingMachine> GetWashingMachineAsync(string washingMachineId)
        {
            return await _httpClient.GetFromJsonAsync<WashingMachine>($"/machine/{washingMachineId}");
        }

        public async Task UpdateWashingMachineAsync(string washingMachineId)
        {
            await _httpClient.PutAsJsonAsync<UpdateWashingMachineDto>($"/machine/{washingMachineId}", new UpdateWashingMachineDto() { WashingMachineId = washingMachineId});
        }
    }
}
