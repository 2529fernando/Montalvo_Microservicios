using System.Net;

namespace Inventario.Api.Services;

public class CatalogoClient(HttpClient httpClient)
{
    public async Task<bool> ExisteProductoAsync(int id, CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync($"api/Productos/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        response.EnsureSuccessStatusCode();
        return true;
    }
}
