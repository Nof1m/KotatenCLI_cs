using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();

    static async Task Main()
    {
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("KotatenCLI/1.0");

        Console.Write("Bem vindo(a)! Digite o título do mangá que você está procurando: ");
        string pesquisa = Console.ReadLine();

        string baseUrl = "https://api.mangadex.org/";
        HttpResponseMessage response = await httpClient.GetAsync($"{baseUrl}/manga?title={pesquisa}");

        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            var mangaResponse = JsonSerializer.Deserialize<MangaResponse>(responseBody);

            if (mangaResponse != null && mangaResponse.data.Length > 0)
            {
                for (int i = 0; i < mangaResponse.data.Length; i++)
                {
                    Console.WriteLine($"{i} - {mangaResponse.data[i].attributes.title.en}");
                }

                while (true)
                {
                    try
                    {
                        Console.Write("Escolha o número do mangá que você deseja: ");
                        int escolha = int.Parse(Console.ReadLine());
                        Manga chosenManga = mangaResponse.data[escolha];
                        string mangaId = chosenManga.id;

                        Console.Write("Digite o idioma dos capítulos que você quer baixar (ex: 'en' para inglês): ");
                        string idioma = Console.ReadLine();

                        Capitulo[] capitulos = await ObterCapitulosPorIdioma(mangaId, idioma);

                        if (capitulos != null)
                        {
                            Console.Write("Digite o caminho completo do diretório onde deseja salvar os capítulos: ");
                            string diretorio = Console.ReadLine();

                            foreach (var capitulo in capitulos)
                            {
                                await SalvarCapitulo(capitulo, diretorio);
                            }
                        }
                        break;
                    }
                    catch (FormatException)
                    {
                        Console.Write("Escolha inválida. Por favor, escolha um número válido.");
                    }
                }
            }
            else
            {
                Console.Write("Nenhum mangá encontrado com o título fornecido.");
            }
        }
        else
        {
            Console.WriteLine($"Erro ao fazer requisição: {response.StatusCode}");
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Detalhes do erro: {responseBody}");
        }
        Console.Write("Obrigado por usar Kotaten! :D\nPressione qualquer tecla para sair... ");
        Console.ReadKey();
    }

    static async Task<Capitulo[]> ObterCapitulosPorIdioma(string mangaId, string idioma)
    {
        string url = "https://api.mangadex.org/";
        string endpoint = $"manga/{mangaId}/feed";

        HttpResponseMessage response = await httpClient.GetAsync($"{url}{endpoint}?translatedLanguage[]={idioma}");

        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            var capituloResponse = JsonSerializer.Deserialize<CapituloResponse>(responseBody);
            return capituloResponse.data;
        }
        else
        {
            Console.WriteLine($"Erro ao obter capítulos: {response.StatusCode}");
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Detalhes do erro: {responseBody}");
            return null;
        }
    }

    static async Task SalvarCapitulo(Capitulo capitulo, string diretorio)
    {
        string imagensId = capitulo.id;

        HttpResponseMessage response = await httpClient.GetAsync($"https://api.mangadex.org/at-home/server/{imagensId}");

        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            var imagensUrl = JsonSerializer.Deserialize<ImagensUrlResponse>(responseBody);

            string capituloNumber = capitulo.attributes.chapter;

            string capituloDir = Path.Combine(diretorio, $"Capítulo {capituloNumber}");
            Directory.CreateDirectory(capituloDir);

            Console.WriteLine($"Baixando capitulo {capituloNumber}");
            foreach (var page in imagensUrl.chapter.data)
            {
                HttpResponseMessage imageResponse = await httpClient.GetAsync($"{imagensUrl.baseUrl}/data/{imagensUrl.chapter.hash}/{page}");

                if (imageResponse.IsSuccessStatusCode)
                {
                    byte[] imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
                    string imagePath = Path.Combine(capituloDir, page);
                    File.WriteAllBytes(imagePath, imageBytes);
                }
                else
                {
                    Console.WriteLine($"Erro ao baixar imagem {page}: {imageResponse.StatusCode}");
                    string responseBodyImage = await imageResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Detalhes do erro: {responseBodyImage}");
                }
            }
        }
        else
        {
            Console.WriteLine($"Erro ao obter URL das imagens: {response.StatusCode}");
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Detalhes do erro: {responseBody}");
        }
    }
}

public class MangaResponse
{
    public Manga[] data { get; set; }
}

public class Manga
{
    public string id { get; set; }
    public MangaAttributes attributes { get; set; }
}

public class MangaAttributes
{
    public MangaTitle title { get; set; }
}

public class MangaTitle
{
    public string en { get; set; }
}

public class CapituloResponse
{
    public Capitulo[] data { get; set; }
}

public class Capitulo
{
    public string id { get; set; }
    public CapituloAttributes attributes { get; set; }
}

public class CapituloAttributes
{
    public string chapter { get; set; }
}

public class ImagensUrlResponse
{
    public string baseUrl { get; set; }
    public ImagensChapter chapter { get; set; }
}

public class ImagensChapter
{
    public string hash { get; set; }
    public List<string> data { get; set; }
}
